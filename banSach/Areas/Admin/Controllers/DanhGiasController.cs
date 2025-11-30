using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using banSach.Models;

namespace banSach.Areas.Admin.Controllers
{
	[AdminAuthorize(Roles = "Quản Lý, Nhân viên")]
	[CheckPermission]
	public class DanhGiasController : Controller
	{
		private QLBanSachEntities db = new QLBanSachEntities();

		// GET: Admin/DanhGias
		[CheckPermission(Permission = "DG_VIEW")]
		public ActionResult Index(string searchString, string filterBy)
		{
			var danhGias = db.DanhGias.Include(d => d.KhachHang).Include(d => d.Sach).AsQueryable();

			// Search
			if (!String.IsNullOrEmpty(searchString))
			{
				danhGias = danhGias.Where(d => d.Sach.TenSach.Contains(searchString)
					|| d.KhachHang.HoTen.Contains(searchString)
					|| d.BinhLuan.Contains(searchString));
			}

			// Filter by rating
			if (!String.IsNullOrEmpty(filterBy))
			{
				int rating;
				if (int.TryParse(filterBy, out rating))
				{
					danhGias = danhGias.Where(d => d.SoSao == rating);
				}
			}

			ViewBag.SearchString = searchString;
			ViewBag.FilterBy = filterBy;

			return View(danhGias.OrderByDescending(d => d.NgayDanhGia).ToList());
		}

		// GET: Admin/DanhGias/Details/5
		[CheckPermission(Permission = "DG_DETAIL")]
		public ActionResult Details(string id)
		{
			if (id == null)
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}
			DanhGia danhGia = db.DanhGias.Find(id);
			if (danhGia == null)
			{
				return HttpNotFound();
			}
			return View(danhGia);
		}

		// GET: Admin/DanhGias/Delete/5
		[CheckPermission(Permission = "DG_DELETE")]
		public ActionResult Delete(string id)
		{
			if (id == null)
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}
			DanhGia danhGia = db.DanhGias.Find(id);
			if (danhGia == null)
			{
				return HttpNotFound();
			}
			return View(danhGia);
		}

		// POST: Admin/DanhGias/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		[CheckPermission(Permission = "DG_DELETE")]
		public ActionResult DeleteConfirmed(string id)
		{
			DanhGia danhGia = db.DanhGias.Find(id);
			db.DanhGias.Remove(danhGia);
			db.SaveChanges();
			TempData["Success"] = "Xóa đánh giá thành công!";
			return RedirectToAction("Index");
		}

		// GET: Admin/DanhGias/Statistics
		[CheckPermission(Permission = "DG_VIEW")]
		public ActionResult Statistics()
		{
			var stats = db.Saches
				.Where(s => s.Status == 1)
				.Select(s => new
				{
					Sach = s,
					SoDanhGia = s.DanhGias.Count,
					DiemTrungBinh = s.DanhGias.Any() ? s.DanhGias.Average(d => d.SoSao) : (double?)null
				})
				.OrderByDescending(x => x.SoDanhGia)
				.Take(20)
				.ToList();

			return View(stats);
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