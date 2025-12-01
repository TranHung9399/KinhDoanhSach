using banSach.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Data.Entity;

namespace banSach.Areas.Admin.Controllers
{
	public class HomeAdminController : Controller
	{
		private QLBanSachEntities db = new QLBanSachEntities();
		// GET: Admin/HomeAdmin
		[CheckPermission(Permission = "TK_DASHBOARD")]
		public async Task<ActionResult> Index()
		{
			try
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
				ViewBag.ChucVu = Session["ChucVu"] != null ? Session["ChucVu"].ToString() : "Nhân viên";

				ViewBag.TongSach = await db.Saches.CountAsync();
				ViewBag.TongKhachHang = await db.KhachHangs.CountAsync();
				
                // Lấy 10 sách bán chạy nhất
				var bestSellerData = await db.ChiTietDonHangs
					.Where(ct => ct.Sach.Status == 1)
					.GroupBy(ct => ct.MaSach)
					.Select(g => new {
						MaSach = g.Key,
						TotalSold = g.Sum(x => x.SoLuong)
					})
					.OrderByDescending(x => x.TotalSold)
					.Take(10)
					.ToListAsync();

				var bestSellerIds = bestSellerData.Select(x => x.MaSach).ToList();
				var bestSellerBooks = await db.Saches
					.Where(s => bestSellerIds.Contains(s.MaSach))
					.ToListAsync();

				ViewBag.BestSellerBooks = bestSellerData
					.Select(bs => {
						var book = bestSellerBooks.FirstOrDefault(s => s.MaSach == bs.MaSach);
						return book != null ? new BestSellerViewModel
						{
							TenSach = book.TenSach,
							Hinh = book.Hinh,
							GiaBan = book.GiaBan,
							SoLuongBan = bs.TotalSold
						} : null;
					})
					.Where(s => s != null)
					.ToList();

				// Calculate total revenue from completed orders
				ViewBag.TongDoanhThu = (await db.DonDatHangs
					.Where(d => d.TrangThai == "Đã thanh toán" || d.TrangThai == "Hoàn tất")
					.SumAsync(d => (decimal?)d.TongTien)) ?? 0;

				// Count completed orders
				ViewBag.TongDonHang = await db.DonDatHangs
					.CountAsync(d => d.TrangThai == "Đã thanh toán" || d.TrangThai == "Hoàn tất");

				// Revenue data for chart (monthly revenue) - OPTIMIZED: Single Query using GroupBy
                int currentYear = DateTime.Now.Year;
                var monthlyRevenue = await db.DonDatHangs
                    .Where(d => (d.TrangThai == "Đã thanh toán" || d.TrangThai == "Hoàn tất")
                             && d.NgayDat.HasValue
                             && d.NgayDat.Value.Year == currentYear)
                    .GroupBy(d => d.NgayDat.Value.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        Revenue = g.Sum(d => (decimal?)d.TongTien) ?? 0
                    })
                    .ToListAsync();

                // Map database results to a 12-element array (filling missing months with 0)
                var revenueArray = new decimal[12];
                foreach (var item in monthlyRevenue)
                {
                    if (item.Month >= 1 && item.Month <= 12)
                    {
                        revenueArray[item.Month - 1] = item.Revenue;
                    }
                }

				ViewBag.RevenueData = Newtonsoft.Json.JsonConvert.SerializeObject(revenueArray);

				// Tạo đối tượng ViewModel để lưu trữ thống kê
				var thongKe = new ThongKeDonHang
				{
					ChoXacNhan = await db.DonDatHangs.CountAsync(d => d.TrangThai == "Chờ xác nhận"),
					DaHuy = await db.DonDatHangs.CountAsync(d => d.TrangThai == "Đã hủy"),
					DaThanhToanVaHoanTat = await db.DonDatHangs.CountAsync(d => d.TrangThai == "Đã thanh toán" || d.TrangThai == "Hoàn tất")
				};

				System.Diagnostics.Debug.WriteLine($"ThongKe: ChoXacNhan={thongKe.ChoXacNhan}, DaHuy={thongKe.DaHuy}, DaThanhToanVaHoanTat={thongKe.DaThanhToanVaHoanTat}");

				// Truyền ViewModel sang view
				return View(thongKe);
			}
			catch (Exception ex)
			{
				// Ghi log lỗi để kiểm tra
				System.Diagnostics.Debug.WriteLine($"Error in HomeAdminController: {ex.Message}\nStackTrace: {ex.StackTrace}");
				
				// Set default ViewBag values
				ViewBag.TongSach = 0;
				ViewBag.TongKhachHang = 0;
				ViewBag.BestSellerBooks = new List<BestSellerViewModel>();
				ViewBag.TongDoanhThu = 0;
				ViewBag.TongDonHang = 0;
				ViewBag.RevenueData = Newtonsoft.Json.JsonConvert.SerializeObject(new List<decimal>(new decimal[12]));
				
				// Trả về ViewModel mặc định nếu có lỗi
				var thongKeMacDinh = new ThongKeDonHang
				{
					ChoXacNhan = 0,
					DaHuy = 0,
					DaThanhToanVaHoanTat = 0
				};
				return View(thongKeMacDinh);
			}
		}
	}
}