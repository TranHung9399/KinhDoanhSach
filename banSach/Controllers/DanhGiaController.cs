using banSach.Models;
using banSach.Helper;
using System;
using System.Linq;
using System.Web.Mvc;

namespace banSach.Controllers
{
	public class DanhGiaController : BaseController
	{

		// GET: Get reviews for a book (AJAX)
		[HttpGet]
		public JsonResult GetReviews(string maSach)
		{
			try
			{
				var reviews = db.DanhGias
					.Where(d => d.MaSach == maSach)
					.OrderByDescending(d => d.NgayDanhGia)
					.Select(d => new
					{
						MaDanhGia = d.MaDanhGia,
						MaKH = d.MaKH,
						TenKH = d.KhachHang.HoTen,
						SoSao = d.SoSao,
						BinhLuan = d.BinhLuan,
						NgayDanhGia = d.NgayDanhGia
					})
					.ToList();

				return Json(new { success = true, data = reviews }, JsonRequestBehavior.AllowGet);
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
			}
		}

		// POST: Add a new review
		[HttpPost]
		[ValidateAjaxAntiForgeryToken]
		public JsonResult AddReview(string maSach, int soSao, string binhLuan)
		{
			try
			{
				// Check if user is logged in
				var maKH = Session["MaKH"]?.ToString();
				if (string.IsNullOrEmpty(maKH))
				{
					return Json(new { success = false, message = "Vui lòng đăng nhập để đánh giá" });
				}

				// Check if user already reviewed this book
				var existingReview = db.DanhGias.FirstOrDefault(d => d.MaSach == maSach && d.MaKH == maKH);
				if (existingReview != null)
				{
					return Json(new { success = false, message = "Bạn đã đánh giá sách này rồi. Vui lòng sửa đánh giá cũ." });
				}

				// Generate new ID
				var maxId = db.DanhGias.Any()
					? db.DanhGias.Max(d => d.MaDanhGia)
					: "DG0000";

				// Validate and parse maxId safely
				int numericPart = 0;
				if (!string.IsNullOrEmpty(maxId) && maxId.Length >= 2)
				{
					int.TryParse(maxId.Substring(2), out numericPart);
				}
				numericPart++;
				var newId = "DG" + numericPart.ToString("D4");

				// Get customer name before creating the review
				var khachHang = db.KhachHangs.Find(maKH);
				var tenKH = khachHang?.HoTen ?? "Unknown";

				var danhGia = new DanhGia
				{
					MaDanhGia = newId,
					MaSach = maSach,
					MaKH = maKH,
					SoSao = soSao,
					BinhLuan = binhLuan,
					NgayDanhGia = DateTime.Now
				};

				db.DanhGias.Add(danhGia);
				db.SaveChanges();

				return Json(new
				{
					success = true,
					message = "Đánh giá của bạn đã được gửi thành công",
					data = new
					{
						MaDanhGia = danhGia.MaDanhGia,
						TenKH = tenKH, // Use the fetched customer name
						SoSao = danhGia.SoSao,
						BinhLuan = danhGia.BinhLuan,
						NgayDanhGia = danhGia.NgayDanhGia?.ToString("dd/MM/yyyy HH:mm") // Format the date properly
					}
				});
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Lỗi: " + ex.Message });
			}
		}

		// POST: Edit review
		[HttpPost]
		[ValidateAjaxAntiForgeryToken]
		public JsonResult EditReview(string maDanhGia, int soSao, string binhLuan)
		{
			try
			{
				var maKH = Session["MaKH"]?.ToString();
				if (string.IsNullOrEmpty(maKH))
				{
					return Json(new { success = false, message = "Vui lòng đăng nhập" });
				}

				var danhGia = db.DanhGias.Find(maDanhGia);
				if (danhGia == null)
				{
					return Json(new { success = false, message = "Không tìm thấy đánh giá" });
				}

				if (danhGia.MaKH != maKH)
				{
					return Json(new { success = false, message = "Bạn không có quyền sửa đánh giá này" });
				}

				danhGia.SoSao = soSao;
				danhGia.BinhLuan = binhLuan;
				danhGia.NgayDanhGia = DateTime.Now;

				db.SaveChanges();

				return Json(new { success = true, message = "Cập nhật đánh giá thành công" });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Lỗi: " + ex.Message });
			}
		}

		// POST: Delete review
		[HttpPost]
		[ValidateAjaxAntiForgeryToken]
		public JsonResult DeleteReview(string maDanhGia)
		{
			try
			{
				var maKH = Session["MaKH"]?.ToString();
				if (string.IsNullOrEmpty(maKH))
				{
					return Json(new { success = false, message = "Vui lòng đăng nhập" });
				}

				var danhGia = db.DanhGias.Find(maDanhGia);
				if (danhGia == null)
				{
					return Json(new { success = false, message = "Không tìm thấy đánh giá" });
				}

				if (danhGia.MaKH != maKH)
				{
					return Json(new { success = false, message = "Bạn không có quyền xóa đánh giá này" });
				}

				db.DanhGias.Remove(danhGia);
				db.SaveChanges();

				return Json(new { success = true, message = "Xóa đánh giá thành công" });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Lỗi: " + ex.Message });
			}
		}

		// GET: Get average rating for a book
		[HttpGet]
		public JsonResult GetAverageRating(string maSach)
		{
			try
			{
				var reviews = db.DanhGias.Where(d => d.MaSach == maSach).ToList();
				var count = reviews.Count;
				var average = count > 0 ? reviews.Average(d => d.SoSao ?? 0) : 0;

				return Json(new { success = true, average = Math.Round(average, 1), count = count }, JsonRequestBehavior.AllowGet);
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
			}
		}
	}
}