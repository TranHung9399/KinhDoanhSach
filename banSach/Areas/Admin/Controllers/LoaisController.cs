using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using banSach.Models;
using PagedList;

namespace banSach.Areas.Admin.Controllers
{
	[AdminAuthorize(Roles = "Quản Lý")]
	[CheckPermission]
	public class LoaisController : Controller
	{
		private QLBanSachEntities db = new QLBanSachEntities();

		[CheckPermission(Permission = "LS_EDIT")]
		public ActionResult ToggleStatus(string id)
		{
			var loai = db.Loais.Find(id);
			if (loai != null)
			{
				loai.Status = loai.Status == 1 ? 0 : 1;
				db.SaveChanges();
				TempData["Message"] = "Cập nhật trạng thái thành công!";
			}
			return RedirectToAction("Index");
		}

		// GET: Admin/Loais
		[CheckPermission(Permission = "LS_VIEW")]
		public async Task<ActionResult> Index(int? page)
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
			int pageSize = 10;
			int pageNumber = (page ?? 1);

			var danhSach = db.Loais.Include(l => l.Saches).OrderBy(l => l.MaLoai).ToPagedList(pageNumber, pageSize);
			return View(danhSach);
		}

		// GET: Admin/Loais/Details/5
		[CheckPermission(Permission = "LS_DETAIL")]
		public async Task<ActionResult> Details(string id)
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
			if (id == null)
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}
			Loai loai = await db.Loais.FindAsync(id);
			if (loai == null)
			{
				return HttpNotFound();
			}
			return View(loai);
		}

		// GET: Admin/Loais/Create
		[CheckPermission(Permission = "LS_CREATE")]
		public ActionResult Create()
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
			return View();
		}

		// POST: Admin/Loais/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		[CheckPermission(Permission = "LS_CREATE")]
		public async Task<ActionResult> Create([Bind(Include = "TenLoai,Status")] Loai loai)
		{
			if (ModelState.IsValid)
			{
				// Sinh mã tự động
				var lastLoai = db.Loais.OrderByDescending(l => l.MaLoai).FirstOrDefault();
				string newMaLoai = "L001";

				if (lastLoai != null)
				{
					string lastMa = lastLoai.MaLoai.Replace("L", "");
					int so = int.Parse(lastMa) + 1;
					newMaLoai = "L" + so.ToString("D3");
				}

				loai.MaLoai = newMaLoai;
				loai.Status = 1;
				db.Loais.Add(loai);
				await db.SaveChangesAsync();
				TempData["Message"] = "Thêm thể loại thành công!";
				return RedirectToAction("Index");
			}

			return View(loai);
		}

		// GET: Admin/Loais/Edit/5
		[CheckPermission(Permission = "LS_EDIT")]
		public async Task<ActionResult> Edit(string id)
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
			if (id == null)
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}
			Loai loai = await db.Loais.FindAsync(id);
			if (loai == null)
			{
				return HttpNotFound();
			}
			return View(loai);
		}

		// POST: Admin/Loais/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		[CheckPermission(Permission = "LS_EDIT")]
		public async Task<ActionResult> Edit([Bind(Include = "MaLoai,TenLoai,Status")] Loai loai)
		{
			if (ModelState.IsValid)
			{
				db.Entry(loai).State = EntityState.Modified;
				await db.SaveChangesAsync();
				TempData["Message"] = "Cập nhật thể loại thành công!";
				return RedirectToAction("Index");
			}
			return View(loai);
		}

		// GET: Admin/Loais/Delete/5
		[CheckPermission(Permission = "LS_DELETE")]
		public async Task<ActionResult> Delete(string id)
		{
			if (id == null)
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}
			Loai loai = await db.Loais.FindAsync(id);
			if (loai == null)
			{
				return HttpNotFound();
			}
			return View(loai);
		}

		// POST: Admin/Loais/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		[CheckPermission(Permission = "LS_DELETE")]
		public async Task<ActionResult> DeleteConfirmed(string id)
		{
			Loai loai = await db.Loais.FindAsync(id);

			// Kiểm tra xem có sách nào thuộc thể loại này không
			if (loai.Saches.Any())
			{
				TempData["ErrorMessage"] = "Không thể xóa thể loại này vì còn sách thuộc thể loại!";
				return RedirectToAction("Index");
			}

			db.Loais.Remove(loai);
			await db.SaveChangesAsync();
			TempData["Message"] = "Xóa thể loại thành công!";
			return RedirectToAction("Index");
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