using System;
using System.Linq;
using System.Web.Mvc;
using banSach.Models;
using System.Collections.Generic;
using System.Globalization;
using ClosedXML.Excel;
using System.IO;
using Rotativa;
using System.Data.Entity;
using System.Threading.Tasks; // Added for Async

namespace banSach.Areas.Admin.Controllers
{
    public class ThongkeController : Controller
    {
        private QLBanSachEntities db = new QLBanSachEntities();

        // GET: Admin/Thongke
        public async Task<ActionResult> Index(string loaiThongKe = "thang", DateTime? ngayBatDau = null, DateTime? ngayKetThuc = null)
        {
            if (Session["AdminUser"] == null)
            {
                return RedirectToAction("Index", "Login", new { area = "Admin" });
            }

            var user = Session["AdminUser"] as NhanVien;
            if (user == null)
            {
                return RedirectToAction("Index", "Login", new { area = "Admin" });
            }

            ViewBag.HoTen = user.HoTen;
            // Mặc định: 30 ngày trước
            var ketThuc = ngayKetThuc ?? DateTime.Now;
            var batDau = ngayBatDau ?? ketThuc.AddDays(-30);

            // Kiểm tra loại thống kê
            if (loaiThongKe != "ngay" && loaiThongKe != "thang" && loaiThongKe != "nam")
            {
                loaiThongKe = "thang";
            }

            // Base Query - AsNoTracking for performance
            var query = db.DonDatHangs
                .AsNoTracking()
                .Where(d =>
                    (d.TrangThai == "Hoàn tất" || d.TrangThai == "Đã thanh toán")
                    && d.NgayDat >= batDau && d.NgayDat <= ketThuc);

            // Calculate totals in DB
            var tongSoDonHang = await query.CountAsync();
            var tongDoanhThu = await query.SelectMany(d => d.ChiTietDonHangs).SumAsync(c => (decimal?)((c.SoLuong ?? 0) * (c.DonGia ?? 0))) ?? 0;
            var tongSoSanPhamBan = await query.SelectMany(d => d.ChiTietDonHangs).SumAsync(c => (int?)(c.SoLuong ?? 0)) ?? 0;
            var tongKhachHang = await query.Select(d => d.MaKH).Distinct().CountAsync();
            var doanhThuTrungBinh = tongSoDonHang > 0 ? tongDoanhThu / tongSoDonHang : 0;

            // Top sản phẩm bán chạy
            var topSanPham = await db.ChiTietDonHangs
                .AsNoTracking()
                .Where(ct => ct.DonDatHang.NgayDat >= batDau 
                    && ct.DonDatHang.NgayDat <= ketThuc
                    && (ct.DonDatHang.TrangThai == "Hoàn tất" || ct.DonDatHang.TrangThai == "Đã thanh toán"))
                .GroupBy(ct => new { ct.MaSach, ct.Sach.TenSach, ct.Sach.Hinh })
                .Select(g => new SanPhamBanChay
                {
                    TenSach = g.Key.TenSach,
                    SoLuongBan = g.Sum(ct => ct.SoLuong ?? 0),
                    DoanhThu = g.Sum(ct => (ct.SoLuong ?? 0) * (ct.DonGia ?? 0)),
                    AnhBia = g.Key.Hinh
                })
                .OrderByDescending(s => s.SoLuongBan)
                .Take(10)
                .ToListAsync();

            // Thống kê theo loại
            var thongKeLoai = await db.ChiTietDonHangs
                .AsNoTracking()
                .Where(ct => ct.DonDatHang.NgayDat >= batDau 
                    && ct.DonDatHang.NgayDat <= ketThuc
                    && (ct.DonDatHang.TrangThai == "Hoàn tất" || ct.DonDatHang.TrangThai == "Đã thanh toán"))
                .GroupBy(ct => ct.Sach.Loai.TenLoai)
                .Select(g => new ThongKeTheoLoai
                {
                    TenLoai = g.Key,
                    SoLuongBan = g.Sum(ct => ct.SoLuong ?? 0),
                    DoanhThu = g.Sum(ct => (ct.SoLuong ?? 0) * (ct.DonGia ?? 0)),
                    TyLe = 0
                })
                .OrderByDescending(l => l.DoanhThu)
                .ToListAsync();

            // Tính tỷ lệ phần trăm
            var tongDoanhThuLoai = thongKeLoai.Sum(l => l.DoanhThu);
            if (tongDoanhThuLoai > 0)
            {
                foreach (var loai in thongKeLoai)
                {
                    loai.TyLe = Math.Round((loai.DoanhThu / tongDoanhThuLoai) * 100, 2);
                }
            }

            // Tạo model thống kê
            var model = new ThongKe
            {
                NgayBatDau = batDau,
                NgayKetThuc = ketThuc,
                LoaiThongKe = loaiThongKe,
                TongSoDonHang = tongSoDonHang,
                TongDoanhThu = tongDoanhThu,
                DoanhThuTrungBinh = doanhThuTrungBinh,
                TongSoSanPhamBan = tongSoSanPhamBan,
                TongKhachHang = tongKhachHang,
                TopSanPhamBanChay = topSanPham,
                ThongKeTheoLoai = thongKeLoai,
                ChiTiet = new List<ChiTietThongKe>()
            };

            // Nhóm theo ngày, tháng hoặc năm - Execute in DB
            if (loaiThongKe == "ngay")
            {
                var data = await query
                    .GroupBy(o => DbFunctions.TruncateTime(o.NgayDat))
                    .Select(g => new 
                    {
                        Date = g.Key,
                        Count = g.Count(),
                        Revenue = g.Sum(o => o.ChiTietDonHangs.Sum(c => (decimal?)((c.SoLuong ?? 0) * (c.DonGia ?? 0))) ?? 0)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                model.ChiTiet = data.Select(x => new ChiTietThongKe
                {
                    KhoangThoiGian = x.Date.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    SoDonHang = x.Count,
                    DoanhThu = x.Revenue
                }).ToList();
            }
            else if (loaiThongKe == "nam")
            {
                var data = await query
                    .GroupBy(o => o.NgayDat.Value.Year)
                    .Select(g => new
                    {
                        Year = g.Key,
                        Count = g.Count(),
                        Revenue = g.Sum(o => o.ChiTietDonHangs.Sum(c => (decimal?)((c.SoLuong ?? 0) * (c.DonGia ?? 0))) ?? 0)
                    })
                    .OrderBy(x => x.Year)
                    .ToListAsync();

                model.ChiTiet = data.Select(x => new ChiTietThongKe
                {
                    KhoangThoiGian = x.Year.ToString(),
                    SoDonHang = x.Count,
                    DoanhThu = x.Revenue
                }).ToList();
            }
            else // Thang
            {
                var data = await query
                    .GroupBy(o => new { o.NgayDat.Value.Year, o.NgayDat.Value.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Count(),
                        Revenue = g.Sum(o => o.ChiTietDonHangs.Sum(c => (decimal?)((c.SoLuong ?? 0) * (c.DonGia ?? 0))) ?? 0)
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToListAsync();

                model.ChiTiet = data.Select(x => new ChiTietThongKe
                {
                    KhoangThoiGian = $"{x.Month}/{x.Year}",
                    SoDonHang = x.Count,
                    DoanhThu = x.Revenue
                }).ToList();
            }

            return View(model);
        }

        public async Task<ActionResult> ExportExcel(string loaiThongKe, DateTime? ngayBatDau, DateTime? ngayKetThuc)
        {
            // Lấy lại dữ liệu như action Index
            var ketThuc = ngayKetThuc ?? DateTime.Now;
            var batDau = ngayBatDau ?? ketThuc.AddDays(-30);
            if (loaiThongKe != "ngay" && loaiThongKe != "thang" && loaiThongKe != "nam")
            {
                loaiThongKe = "thang";
            }
            
            // Base Query - AsNoTracking
            var query = db.DonDatHangs
                .AsNoTracking()
                .Where(d => (d.TrangThai == "Hoàn tất" || d.TrangThai == "Đã thanh toán")
                    && d.NgayDat >= batDau && d.NgayDat <= ketThuc);

            // Calculate totals in DB
            var tongSoDonHang = await query.CountAsync();
            var tongDoanhThu = await query.SelectMany(d => d.ChiTietDonHangs).SumAsync(c => (decimal?)((c.SoLuong ?? 0) * (c.DonGia ?? 0))) ?? 0;

            var model = new ThongKe
            {
                NgayBatDau = batDau,
                NgayKetThuc = ketThuc,
                LoaiThongKe = loaiThongKe,
                TongSoDonHang = tongSoDonHang,
                TongDoanhThu = tongDoanhThu,
                ChiTiet = new List<ChiTietThongKe>()
            };

            if (loaiThongKe == "ngay")
            {
                var data = await query
                    .GroupBy(o => DbFunctions.TruncateTime(o.NgayDat))
                    .Select(g => new 
                    {
                        Date = g.Key,
                        Count = g.Count(),
                        Revenue = g.Sum(o => o.ChiTietDonHangs.Sum(c => (decimal?)((c.SoLuong ?? 0) * (c.DonGia ?? 0))) ?? 0)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                model.ChiTiet = data.Select(x => new ChiTietThongKe
                {
                    KhoangThoiGian = x.Date.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    SoDonHang = x.Count,
                    DoanhThu = x.Revenue
                }).ToList();
            }
            else if (loaiThongKe == "nam")
            {
                var data = await query
                    .GroupBy(o => o.NgayDat.Value.Year)
                    .Select(g => new
                    {
                        Year = g.Key,
                        Count = g.Count(),
                        Revenue = g.Sum(o => o.ChiTietDonHangs.Sum(c => (decimal?)((c.SoLuong ?? 0) * (c.DonGia ?? 0))) ?? 0)
                    })
                    .OrderBy(x => x.Year)
                    .ToListAsync();

                model.ChiTiet = data.Select(x => new ChiTietThongKe
                {
                    KhoangThoiGian = x.Year.ToString(),
                    SoDonHang = x.Count,
                    DoanhThu = x.Revenue
                }).ToList();
            }
            else
            {
                var data = await query
                    .GroupBy(o => new { o.NgayDat.Value.Year, o.NgayDat.Value.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Count(),
                        Revenue = g.Sum(o => o.ChiTietDonHangs.Sum(c => (decimal?)((c.SoLuong ?? 0) * (c.DonGia ?? 0))) ?? 0)
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToListAsync();

                model.ChiTiet = data.Select(x => new ChiTietThongKe
                {
                    KhoangThoiGian = $"{x.Month}/{x.Year}",
                    SoDonHang = x.Count,
                    DoanhThu = x.Revenue
                }).ToList();
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("ThongKe");
                worksheet.Cell(1, 1).Value = "Khoảng Thời Gian";
                worksheet.Cell(1, 2).Value = "Số Đơn Hàng";
                worksheet.Cell(1, 3).Value = "Doanh Thu";
                int row = 2;
                foreach (var item in model.ChiTiet)
                {
                    worksheet.Cell(row, 1).Value = item.KhoangThoiGian;
                    worksheet.Cell(row, 2).Value = item.SoDonHang;
                    worksheet.Cell(row, 3).Value = item.DoanhThu;
                    row++;
                }
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ThongKeDoanhThu.xlsx");
                }
            }
        }

        public async Task<ActionResult> ExportPdf(string loaiThongKe, DateTime? ngayBatDau, DateTime? ngayKetThuc)
        {
            var ketThuc = ngayKetThuc ?? DateTime.Now;
            var batDau = ngayBatDau ?? ketThuc.AddDays(-30);
            if (loaiThongKe != "ngay" && loaiThongKe != "thang" && loaiThongKe != "nam")
            {
                loaiThongKe = "thang";
            }

            // Base Query - AsNoTracking
            var query = db.DonDatHangs
                .AsNoTracking()
                .Where(d => (d.TrangThai == "Hoàn tất" || d.TrangThai == "Đã thanh toán")
                    && d.NgayDat >= batDau && d.NgayDat <= ketThuc);

            // Calculate totals in DB
            var tongSoDonHang = await query.CountAsync();
            var tongDoanhThu = await query.SelectMany(d => d.ChiTietDonHangs).SumAsync(c => (decimal?)((c.SoLuong ?? 0) * (c.DonGia ?? 0))) ?? 0;

            var model = new ThongKe
            {
                NgayBatDau = batDau,
                NgayKetThuc = ketThuc,
                LoaiThongKe = loaiThongKe,
                TongSoDonHang = tongSoDonHang,
                TongDoanhThu = tongDoanhThu,
                ChiTiet = new List<ChiTietThongKe>()
            };

            if (loaiThongKe == "ngay")
            {
                var data = await query
                    .GroupBy(o => DbFunctions.TruncateTime(o.NgayDat))
                    .Select(g => new 
                    {
                        Date = g.Key,
                        Count = g.Count(),
                        Revenue = g.Sum(o => o.ChiTietDonHangs.Sum(c => (decimal?)((c.SoLuong ?? 0) * (c.DonGia ?? 0))) ?? 0)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                model.ChiTiet = data.Select(x => new ChiTietThongKe
                {
                    KhoangThoiGian = x.Date.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    SoDonHang = x.Count,
                    DoanhThu = x.Revenue
                }).ToList();
            }
            else if (loaiThongKe == "nam")
            {
                var data = await query
                    .GroupBy(o => o.NgayDat.Value.Year)
                    .Select(g => new
                    {
                        Year = g.Key,
                        Count = g.Count(),
                        Revenue = g.Sum(o => o.ChiTietDonHangs.Sum(c => (decimal?)((c.SoLuong ?? 0) * (c.DonGia ?? 0))) ?? 0)
                    })
                    .OrderBy(x => x.Year)
                    .ToListAsync();

                model.ChiTiet = data.Select(x => new ChiTietThongKe
                {
                    KhoangThoiGian = x.Year.ToString(),
                    SoDonHang = x.Count,
                    DoanhThu = x.Revenue
                }).ToList();
            }
            else
            {
                var data = await query
                    .GroupBy(o => new { o.NgayDat.Value.Year, o.NgayDat.Value.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Count(),
                        Revenue = g.Sum(o => o.ChiTietDonHangs.Sum(c => (decimal?)((c.SoLuong ?? 0) * (c.DonGia ?? 0))) ?? 0)
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToListAsync();

                model.ChiTiet = data.Select(x => new ChiTietThongKe
                {
                    KhoangThoiGian = $"{x.Month}/{x.Year}",
                    SoDonHang = x.Count,
                    DoanhThu = x.Revenue
                }).ToList();
            }
            return new Rotativa.ViewAsPdf("ExportPdf", model)
            {
                FileName = "ThongKeDoanhThu.pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageOrientation = Rotativa.Options.Orientation.Landscape
            };
        }
    }
}

