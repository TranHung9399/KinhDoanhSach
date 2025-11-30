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
					// Truy vấn CSDL để lấy sách yêu thích của KH này
					model = (from yt in db.YeuThiches
							 join s in db.Saches on yt.MaSach equals s.MaSach
							 where yt.MaKH == maKH && s.Status == 1
							 orderby yt.NgayThem descending
							 select s)
							 .Take(20) // Giới hạn 20 sách
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
					model = (from yt in db.YeuThiches
							 join s in db.Saches on yt.MaSach equals s.MaSach
							 where yt.MaKH == maKH && s.Status == 1
							 orderby yt.NgayThem descending
							 select s)
							 .Take(20)
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
				// Lấy giỏ hàng (object) của khách hàng từ DB
				GioHang cart = db.GioHangs
								 .Include("ChiTietGioHangs.Sach") // Load chi tiết và sách
								 .FirstOrDefault(g => g.MaKH == kh.MaKH);

				if (cart != null && cart.ChiTietGioHangs.Any())
				{
					// Lấy các sản phẩm
					cartItems = cart.ChiTietGioHangs
									.OrderByDescending(ct => ct.MaGioHang)
									.Take(3) // Lấy 3 sản phẩm mới nhất
									.ToList();

					// Tính tổng tiền
					tongThanhTien = cart.ChiTietGioHangs
										.Sum(item => (decimal)item.SoLuong * item.Sach.GiaChietKhau.GetValueOrDefault(0));
				}
			}

			// Dùng ViewBag để truyền tổng tiền
			ViewBag.TongThanhTienDropdown = tongThanhTien;

			// Truyền danh sách ChiTietGioHang (List) làm model
			return PartialView("_CartPartial", cartItems);
		}
		public ActionResult Menu()
        {
            var categories = db.Loais.Where(l => l.Status == 1).ToList();
            return PartialView("Menu", categories);
        }
    }
}