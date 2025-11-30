using banSach.Models;
using banSach.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace banSach.Areas.Admin.Controllers
{
	public class HomeAdminController : Controller
	{
		private QLBanSachEntities db = new QLBanSachEntities();
		// GET: Admin/HomeAdmin
		[CheckPermission(Permission = "TK_DASHBOARD")]
		public ActionResult Index()
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

				ViewBag.TongSach = db.Saches.Count();
				ViewBag.TongKhachHang = db.KhachHangs.Count();
				// Lấy 10 sách bán chạy nhất
				var bestSellerData = db.ChiTietDonHangs
					.Where(ct => ct.Sach.Status == 1)
					.GroupBy(ct => ct.MaSach)
					.Select(g => new {
						MaSach = g.Key,
						TotalSold = g.Sum(x => x.SoLuong)
					})
					.OrderByDescending(x => x.TotalSold)
					.Take(10)
					.ToList();

				var bestSellerIds = bestSellerData.Select(x => x.MaSach).ToList();
				var bestSellerBooks = db.Saches
					.Where(s => bestSellerIds.Contains(s.MaSach))
					.ToList();

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
				ViewBag.TongDoanhThu = db.DonDatHangs
					.Where(d => d.TrangThai == "Đã thanh toán" || d.TrangThai == "Hoàn tất")
					.Sum(d => (decimal?)d.TongTien) ?? 0;

				// Count completed orders
				ViewBag.TongDonHang = db.DonDatHangs
					.Count(d => d.TrangThai == "Đã thanh toán" || d.TrangThai == "Hoàn tất");

				// Revenue data for chart (monthly revenue)
				ViewBag.RevenueData = Newtonsoft.Json.JsonConvert.SerializeObject(
					Enumerable.Range(1, 12).Select(month =>
						db.DonDatHangs
							.Where(d => (d.TrangThai == "Đã thanh toán" || d.TrangThai == "Hoàn tất")
									 && d.NgayDat.HasValue
									 && d.NgayDat.Value.Year == DateTime.Now.Year
									 && d.NgayDat.Value.Month == month)
							.Sum(d => (decimal?)d.TongTien) ?? 0
					).ToList()
				);

				// Tạo đối tượng ViewModel để lưu trữ thống kê
				var thongKe = new ThongKeDonHang
				{
					ChoXacNhan = db.DonDatHangs.Count(d => d.TrangThai == "Chờ xác nhận"),
					DaHuy = db.DonDatHangs.Count(d => d.TrangThai == "Đã hủy"),
					DaThanhToanVaHoanTat = db.DonDatHangs.Count(d => d.TrangThai == "Đã thanh toán" || d.TrangThai == "Hoàn tất")
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