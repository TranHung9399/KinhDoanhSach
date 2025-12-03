using banSach.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace banSach.Controllers
{
    public class ModuleController : Controller
    {
        private QLBanSachEntities db = new QLBanSachEntities();
		// GET: Module
		[ChildActionOnly]
		public ActionResult _WishlistPartial()
		{
			var model = new List<Sach>();

			// Lấy MaKH từ Session (thống nhất với YeuThichController)
			var maKH = Session["MaKH"]?.ToString();

			if (!string.IsNullOrEmpty(maKH))
			{
				try
				{
					// Truy vấn CSDL tối ưu với AsNoTracking() cho read-only query
					// Eager loading với Include để tránh N+1 query
					// Lọc tại database level và Take() trước ToList()
					model = db.YeuThiches
							 .AsNoTracking()
							 .Where(yt => yt.MaKH == maKH)
							 .OrderByDescending(yt => yt.NgayThem)
							 .Take(20)
							 .Select(yt => yt.Sach)
							 .Where(s => s.Status == 1)
							 .ToList();

					// Debug log
					System.Diagnostics.Debug.WriteLine($"Wishlist loaded for {maKH}: {model.Count} items");
				}
				catch (Exception ex)
				{
					// Log lỗi
					System.Diagnostics.Debug.WriteLine($"Error loading wishlist: {ex.Message}");
				}
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("No MaKH in session");
			}

			return PartialView("_WishlistPartial", model);
		}
		// THÊM ACTION MỚI: Load wishlist qua AJAX
		[HttpGet]
		public ActionResult LoadWishlistPartial()
		{
			var model = new List<Sach>();

			var maKH = Session["MaKH"]?.ToString();

			if (!string.IsNullOrEmpty(maKH))
			{
				try
				{
					// Truy vấn tối ưu với AsNoTracking()
					model = db.YeuThiches
							 .AsNoTracking()
							 .Where(yt => yt.MaKH == maKH)
							 .OrderByDescending(yt => yt.NgayThem)
							 .Take(20)
							 .Select(yt => yt.Sach)
							 .Where(s => s.Status == 1)
							 .ToList();
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Error loading wishlist: {ex.Message}");
				}
			}

			return PartialView("_WishlistPartial", model);
		}

		[ChildActionOnly]
		public ActionResult _CartPartial()
		{
			// Lấy MaKH từ Session
			KhachHang kh = Session["KhachHang"] as KhachHang;

			// Model cho View
			List<ChiTietGioHang> cartItems = new List<ChiTietGioHang>();
			decimal tongThanhTien = 0;

			if (kh != null)
			{
				// Tối ưu: Sử dụng AsNoTracking() cho read-only query
				// Eager loading với Include để tránh N+1 query problem
				// Tính tổng tiền ngay tại database với Sum() trên IQueryable
				var maKH = kh.MaKH;
				
				var cart = db.GioHangs
							 .AsNoTracking()
							 .Include("ChiTietGioHangs.Sach")
							 .FirstOrDefault(g => g.MaKH == maKH);

				if (cart != null && cart.ChiTietGioHangs.Any())
				{
					// Lấy 3 sản phẩm mới nhất (đã được eager load)
					cartItems = cart.ChiTietGioHangs
									.OrderByDescending(ct => ct.MaGioHang)
									.Take(3)
									.ToList();

					// Tính tổng tiền từ database (aggregation at SQL level)
					tongThanhTien = db.ChiTietGioHangs
										.AsNoTracking()
										.Where(ct => ct.MaGioHang == cart.MaGioHang)
										.Sum(item => (decimal?)item.SoLuong * item.Sach.GiaChietKhau) ?? 0;
				}
			}

			// Dùng ViewBag để truyền tổng tiền
			ViewBag.TongThanhTienDropdown = tongThanhTien;

			// Truyền danh sách ChiTietGioHang (List) làm model
			return PartialView("_CartPartial", cartItems);
		}
		public ActionResult Menu()
        {
            // Tối ưu: AsNoTracking() cho read-only query
            // Chỉ lấy các cột cần thiết (projection)
            var categories = db.Loais
                              .AsNoTracking()
                              .Where(l => l.Status == 1)
                              .ToList();
            return PartialView("Menu", categories);
        }
    }
}