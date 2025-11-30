using banSach.Models;
using banSach.Helper;
using System;
using System.Linq;
using System.Web.Mvc;
using PagedList;

namespace banSach.Controllers
{
	public class YeuThichController : BaseController
	{

		// GET: Display favorites page
		public ActionResult Index(int? page)
		{
			var maKH = Session["MaKH"]?.ToString();
			if (string.IsNullOrEmpty(maKH))
			{
				TempData["Error"] = "Vui lòng đăng nhập để xem danh sách yêu thích";
				return RedirectToAction("Index", "Dangnhap");
			}

			int pageSize = 12;
			int pageNumber = page ?? 1;

			var favorites = db.YeuThiches
				.Where(y => y.MaKH == maKH)
				.OrderByDescending(y => y.NgayThem)
				.Select(y => y.Sach)
				.Where(s => s.Status == 1)
				.ToList();

			ViewBag.Title = "Danh sách yêu thích";
			return View(favorites.ToPagedList(pageNumber, pageSize));
		}

		// POST: Add to favorites
		[HttpPost]
		[ValidateAjaxAntiForgeryToken]
		public JsonResult AddFavorite(string maSach)
		{
			try
			{
				var maKH = Session["MaKH"]?.ToString();
				if (string.IsNullOrEmpty(maKH))
				{
					return Json(new { success = false, message = "Vui lòng đăng nhập" });
				}

				// Check if already in favorites
				var existing = db.YeuThiches.FirstOrDefault(y => y.MaKH == maKH && y.MaSach == maSach);
				if (existing != null)
				{
					return Json(new { success = false, message = "Sách đã có trong danh sách yêu thích" });
				}

				// Generate new ID
				var maxId = db.YeuThiches.Any()
					? db.YeuThiches.Max(y => y.MaYeuThich)
					: "YT0000";

				// Validate and parse maxId safely
				int numericPart = 0;
				if (!string.IsNullOrEmpty(maxId) && maxId.Length >= 2)
				{
					int.TryParse(maxId.Substring(2), out numericPart);
				}
				numericPart++;
				var newId = "YT" + numericPart.ToString("D4");

				var yeuThich = new YeuThich
				{
					MaYeuThich = newId,
					MaKH = maKH,
					MaSach = maSach,
					NgayThem = DateTime.Now
				};

				db.YeuThiches.Add(yeuThich);
				db.SaveChanges();

				return Json(new { success = true, message = "Đã thêm vào danh sách yêu thích" });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Lỗi: " + ex.Message });
			}
		}

		// POST: Remove from favorites
		[HttpPost]
		[ValidateAjaxAntiForgeryToken]
		public JsonResult RemoveFavorite(string maSach)
		{
			try
			{
				var maKH = Session["MaKH"]?.ToString();
				if (string.IsNullOrEmpty(maKH))
				{
					return Json(new { success = false, message = "Vui lòng đăng nhập" });
				}

				var yeuThich = db.YeuThiches.FirstOrDefault(y => y.MaKH == maKH && y.MaSach == maSach);
				if (yeuThich == null)
				{
					return Json(new { success = false, message = "Không tìm thấy trong danh sách yêu thích" });
				}

				db.YeuThiches.Remove(yeuThich);
				db.SaveChanges();

				return Json(new { success = true, message = "Đã xóa khỏi danh sách yêu thích" });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Lỗi: " + ex.Message });
			}
		}

		// POST: Toggle favorite (add if not exists, remove if exists)
		[HttpPost]
		[ValidateAjaxAntiForgeryToken]
		public JsonResult ToggleFavorite(string maSach)
		{
			try
			{
				var maKH = Session["MaKH"]?.ToString();
				if (string.IsNullOrEmpty(maKH))
				{
					return Json(new { success = false, message = "Vui lòng đăng nhập", needLogin = true });
				}

				var existing = db.YeuThiches.FirstOrDefault(y => y.MaKH == maKH && y.MaSach == maSach);

				if (existing != null)
				{
					// Remove from favorites
					db.YeuThiches.Remove(existing);
					db.SaveChanges();
					return Json(new { success = true, isFavorite = false, message = "Đã xóa khỏi danh sách yêu thích" });
				}
				else
				{
					// Add to favorites
					var maxId = db.YeuThiches.Any()
						? db.YeuThiches.Max(y => y.MaYeuThich)
						: "YT0000";

					// Validate and parse maxId safely
					int numericPart = 0;
					if (!string.IsNullOrEmpty(maxId) && maxId.Length >= 2)
					{
						int.TryParse(maxId.Substring(2), out numericPart);
					}
					numericPart++;
					var newId = "YT" + numericPart.ToString("D4");

					var yeuThich = new YeuThich
					{
						MaYeuThich = newId,
						MaKH = maKH,
						MaSach = maSach,
						NgayThem = DateTime.Now
					};

					db.YeuThiches.Add(yeuThich);
					db.SaveChanges();
					return Json(new { success = true, isFavorite = true, message = "Đã thêm vào danh sách yêu thích" });
				}
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Lỗi: " + ex.Message });
			}
		}

		// GET: Check if book is in favorites
		[HttpGet]
		public JsonResult IsFavorite(string maSach)
		{
			try
			{
				var maKH = Session["MaKH"]?.ToString();
				if (string.IsNullOrEmpty(maKH))
				{
					return Json(new { success = true, isFavorite = false }, JsonRequestBehavior.AllowGet);
				}

				var isFavorite = db.YeuThiches.Any(y => y.MaKH == maKH && y.MaSach == maSach);
				return Json(new { success = true, isFavorite = isFavorite }, JsonRequestBehavior.AllowGet);
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
			}
		}
	}
}