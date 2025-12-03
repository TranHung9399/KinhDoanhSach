using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using banSach.Helper;
using banSach.Models;
using Newtonsoft.Json;
using PagedList;
using System.Threading.Tasks;

namespace banSach.Areas.Admin.Controllers
{
	[AdminAuthorize(Roles = "Quản Lý, Nhân viên")]
	[CheckPermission]
	public class DonDatHangsController : Controller
    {
        private QLBanSachEntities db = new QLBanSachEntities();

		[CheckPermission(Permission = "DH_DETAIL")]
		public async Task<ActionResult> Invoice(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            // Tối ưu: AsNoTracking() cho read-only query, eager loading để tránh N+1
            DonDatHang donDatHang = await db.DonDatHangs
                .AsNoTracking()
                .Include(d => d.ChiTietDonHangs.Select(c => c.Sach))
                .FirstOrDefaultAsync(d => d.MaDonHang == id);
            if (donDatHang == null)
            {
                return HttpNotFound();
            }
            return View(donDatHang);
        }

		[HttpPost]
		[CheckPermission(Permission = "DH_UPDATE_STATUS")]
		public async Task<ActionResult> CapNhatTrangThai(string maDonHang, string trangThai)
        {
            var donHang = await db.DonDatHangs.FindAsync(maDonHang);
            if (donHang == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("Index");
            }

            donHang.TrangThai = trangThai;

            // ✅ THÊM: Tự động cập nhật trạng thái thanh toán cho COD
            var thanhToan = await db.ThanhToans.FirstOrDefaultAsync(t => t.MaDonHang == maDonHang);
            if (thanhToan != null && thanhToan.PhuongThucThanhToan == 1) // COD
            {
                // Khi đơn hàng "Hoàn tất", tự động chuyển trạng thái thanh toán sang "Đã thanh toán"
                if (trangThai == "Hoàn tất" || trangThai == "Đã giao")
                {
                    thanhToan.TrangThaiThanhToan = "Đã thanh toán";
                    thanhToan.NgayThanhToan = DateTime.Now;
                    thanhToan.GhiChu = "Thanh toán khi nhận hàng (COD) - Đã nhận tiền từ khách hàng";
                }
            }

            await db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật trạng thái thành công!";

            // Gửi email nếu trạng thái là "Đang giao hàng"
            if (trangThai == "Đang giao hàng")
            {
                decimal tongTien = 0;
                string emailBody = $"<div style='color: black;'>" +
                                    $"<p>Chào {donHang.HoTen},</p>" +
                                    $"<p>Đơn hàng của bạn (Mã đơn: <strong>{donHang.MaDonHang}</strong>) hiện đang được vận chuyển.</p>" +
                                    $"<p>Chúng tôi sẽ giao hàng đến bạn trong thời gian sớm nhất. Hãy chú ý điện thoại của bạn.</p>" +
                                    "<p>Chi tiết đơn hàng:</p>" +
                                    "<table border='1' cellspacing='0' cellpadding='5' style='border-collapse: collapse; width: 100%;'>" +
                                    "<thead><tr>" +
                                    "<th style='text-align:left;'>Tên sách</th>" +
                                    "<th>Số lượng</th>" +
                                    "<th>Đơn giá</th>" +
                                    "<th>Thành tiền</th>" +
                                    "</tr></thead><tbody>";

                foreach (var item in donHang.ChiTietDonHangs)
                {
                    var sach = await db.Saches.FindAsync(item.MaSach);
                    if (sach != null)
                    {
                        var thanhTien = (item.SoLuong ?? 0) * (item.DonGia ?? 0);
                        tongTien += thanhTien;

                        emailBody += $"<tr>" +
                                     $"<td>{sach.TenSach}</td>" +
                                     $"<td style='text-align:center;'>{item.SoLuong}</td>" +
                                     $"<td style='text-align:right;'>{item.DonGia:N0}₫</td>" +
                                     $"<td style='text-align:right;'>{thanhTien:N0}₫</td>" +
                                     "</tr>";

                        // Trừ tồn kho
                        sach.SoLuongTon -= item.SoLuong ?? 0;
                    }
                }

                emailBody += $"<tr>" +
                             $"<td colspan='3' style='text-align:right; font-weight:bold;'>Tổng cộng:</td>" +
                             $"<td style='text-align:right; font-weight:bold;'>{tongTien:N0}₫</td>" +
                             $"</tr></tbody></table>" +
                             "<p><strong>Cảm ơn quý khách đã mua hàng tại cửa hàng Sách Trực Tuyến!</strong></p>" +
                             "<p>Hotline hỗ trợ: <strong>+060 (800) 801-582</strong></p>" +
                             "<p><em>Trân trọng,</em><br/>HT STORE</p>" +
                             "</div>";

                // Gửi email
                try
                {
                    SendMail sendMail = new SendMail();
                    // Async wrapper for email sending
                    await Task.Run(() => sendMail.SendMailFunction(donHang.Email, "Thông báo về trạng thái đơn hàng: Đang giao hàng", emailBody));
                    TempData["SuccessMessage"] = "Đã gửi email xác nhận về trạng thái đơn hàng!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Đã có lỗi khi gửi email: " + ex.Message;
                }

                await db.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

		// GET: Admin/DonDatHangs
		[CheckPermission(Permission = "DH_VIEW")]
		public ActionResult Index(int? page, string searchString, string searchType, string statusFilter)
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
            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentSearchType = searchType ?? "name";
            ViewBag.StatusFilter = statusFilter;

            // Tối ưu: AsNoTracking() cho read-only query và eager loading
            var donDatHangs = db.DonDatHangs
                .AsNoTracking()
                .Include(d => d.ChiTietDonHangs)
                .AsQueryable();
            
            if (!string.IsNullOrEmpty(searchString))
            {
                if (searchType == "id")
                {
                    donDatHangs = donDatHangs.Where(d => d.MaDonHang.Contains(searchString));
                }
                else
                {
                    donDatHangs = donDatHangs.Where(d => d.HoTen.Contains(searchString));
                }
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(statusFilter))
            {
                donDatHangs = donDatHangs.Where(d => d.TrangThai == statusFilter);
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            var danhSach = donDatHangs.OrderByDescending(hd => hd.NgayDat).ToPagedList(pageNumber, pageSize);
            return View(danhSach);
        }


		// GET: Admin/DonDatHangs/Details/5
		[CheckPermission(Permission = "DH_DETAIL")]
		public async Task<ActionResult> Details(string id)
		{
			if (id == null)
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}

			// Tối ưu: AsNoTracking() cho read-only query và eager loading
			var donDatHang = await db.DonDatHangs
				.AsNoTracking()
				.Include(d => d.ChiTietDonHangs.Select(ct => ct.Sach))
				.Include(d => d.KhachHang)  // ✅ THÊM: Include KhachHang
				.Include(d => d.ThanhToans)  // ✅ THÊM: Include ThanhToan
				.FirstOrDefaultAsync(d => d.MaDonHang == id);

			if (donDatHang == null)
			{
				return HttpNotFound();
			}

			// ✅ THÊM: Lấy thông tin mã giảm giá đã dùng
			if (!string.IsNullOrEmpty(donDatHang.MaGiamGia))
			{
				var suDungMGG = await db.SuDungMaGiamGias
					.Include(s => s.MaGiamGia)
					.FirstOrDefaultAsync(s => s.MaDonHang == id);
				ViewBag.SuDungMaGiamGia = suDungMGG;
			}

			return View(donDatHang);
		}

		// GET: Admin/DonDatHangs/Create
		public async Task<ActionResult> Create()
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
            // Fetch books with Status = 1
            var sachList = await db.Saches
                .Where(s => s.Status == 1)
                .Select(s => new { s.MaSach, s.TenSach, s.GiaBan })
                .ToListAsync();

            if (!sachList.Any())
            {
                System.Diagnostics.Debug.WriteLine("No books found with Status = 1");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Found {sachList.Count} books with Status = 1");
                foreach (var sach in sachList)
                {
                    System.Diagnostics.Debug.WriteLine($"Book: {sach.TenSach}, Price: {sach.GiaBan}");
                }
            }

            ViewBag.SachListJson = JsonConvert.SerializeObject(sachList, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            return View();
        }

        // POST: Admin/DonDatHangs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(DonDatHang model)
        {
            if (model.ChiTietDonHangs == null || !model.ChiTietDonHangs.Any())
            {
                ModelState.AddModelError("", "Vui lòng thêm ít nhất một cuốn sách vào đơn hàng.");
            }

            if (ModelState.IsValid)
            {
                var donHang = new DonDatHang
                {
                    MaDonHang = "DH" + DateTime.Now.Ticks,
                    HoTen = model.HoTen,
                    SoDienThoai = model.SoDienThoai,
                    Email = model.Email,
                    DiaChi = model.DiaChi,
                    NgayDat = DateTime.Now,
                    PhuongThucThanhToan = model.PhuongThucThanhToan,
                    TrangThai = string.IsNullOrEmpty(model.TrangThai) ? "Chờ xác nhận" : model.TrangThai
                };

                db.DonDatHangs.Add(donHang);

                if (model.ChiTietDonHangs != null)
                {
                    foreach (var item in model.ChiTietDonHangs)
                    {
                        var chiTiet = new ChiTietDonHang
                        {
                            MaDonHang = donHang.MaDonHang,
                            MaSach = item.MaSach,
                            SoLuong = item.SoLuong,
                            DonGia = item.DonGia
                        };
                        db.ChiTietDonHangs.Add(chiTiet);
                    }
                }

                try
                {
                    await db.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Tạo đơn hàng thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Database Error: {ex.Message}");
                    ModelState.AddModelError("", "Lỗi khi lưu đơn hàng. Vui lòng thử lại.");
                }
            }

            var sachList = await db.Saches.Where(s => s.Status == 1).Select(s => new { s.MaSach, s.TenSach, s.GiaBan }).ToListAsync();
            ViewBag.SachListJson = JsonConvert.SerializeObject(
                sachList,
                new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            );
            return View(model);
        }

       
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
