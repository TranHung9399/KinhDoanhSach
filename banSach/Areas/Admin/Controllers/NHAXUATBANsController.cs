using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using banSach.Models;
using PagedList;

namespace banSach.Areas.Admin.Controllers
{
	[AdminAuthorize(Roles = "Quản Lý")]
	[CheckPermission]
	public class NhaXuatBansController : Controller
	{
		private QLBanSachEntities db = new QLBanSachEntities();

		// GET: Admin/NhaXuatBans
		[CheckPermission(Permission = "NXB_VIEW")]
		public ActionResult Index(int? page, string searchString, string searchType)
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

			var nxb = db.NhaXuatBans.Include(n => n.Saches).AsQueryable();
			ViewBag.CurrentFilter = searchString;
			ViewBag.CurrentSearchType = searchType ?? "name";

			if (!string.IsNullOrEmpty(searchString))
			{
				if (searchType == "id")
				{
					nxb = nxb.Where(n => n.MaNXB.Contains(searchString));
				}
				else
				{
					nxb = nxb.Where(n => n.TenNXB.Contains(searchString));
				}
			}

			int pageSize = 10;
			int pageNumber = (page ?? 1);
			return View(nxb.OrderBy(n => n.MaNXB).ToPagedList(pageNumber, pageSize));
		}

		// GET: Admin/NhaXuatBans/Details/5
		[CheckPermission(Permission = "NXB_DETAIL")]
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
			NhaXuatBan nhaXuatBan = await db.NhaXuatBans.FindAsync(id);
			if (nhaXuatBan == null)
			{
				return HttpNotFound();
			}
			return View(nhaXuatBan);
		}

		// GET: Admin/NhaXuatBans/Create
		[CheckPermission(Permission = "NXB_CREATE")]
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

		// POST: Admin/NhaXuatBans/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		[CheckPermission(Permission = "NXB_CREATE")]
		public async Task<ActionResult> Create([Bind(Include = "TenNXB,DiaChi")] NhaXuatBan nhaXuatBan)
		{
			if (ModelState.IsValid)
			{
				// Tạo mã NXB tự động
				var lastNXB = db.NhaXuatBans.OrderByDescending(n => n.MaNXB).FirstOrDefault();
				string newMaNXB = "NXB001";

				if (lastNXB != null)
				{
					string lastMa = lastNXB.MaNXB.Replace("NXB", "");
					int so = int.Parse(lastMa) + 1;
					newMaNXB = "NXB" + so.ToString("D3");
				}

				nhaXuatBan.MaNXB = newMaNXB;

				db.NhaXuatBans.Add(nhaXuatBan);
				await db.SaveChangesAsync();
				TempData["SuccessMessage"] = "Thêm nhà xuất bản thành công!";
				return RedirectToAction("Index");
			}

			return View(nhaXuatBan);
		}

		// GET: Admin/NhaXuatBans/Edit/5
		[CheckPermission(Permission = "NXB_EDIT")]
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
			NhaXuatBan nhaXuatBan = await db.NhaXuatBans.FindAsync(id);
			if (nhaXuatBan == null)
			{
				return HttpNotFound();
			}
			return View(nhaXuatBan);
		}

		// POST: Admin/NhaXuatBans/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		[CheckPermission(Permission = "NXB_EDIT")]
		public async Task<ActionResult> Edit([Bind(Include = "MaNXB,TenNXB,DiaChi")] NhaXuatBan nhaXuatBan)
		{
			if (ModelState.IsValid)
			{
				db.Entry(nhaXuatBan).State = EntityState.Modified;
				await db.SaveChangesAsync();
				TempData["SuccessMessage"] = "Cập nhật nhà xuất bản thành công!";
				return RedirectToAction("Index");
			}
			return View(nhaXuatBan);
		}

		// GET: Admin/NhaXuatBans/Delete/5
		[CheckPermission(Permission = "NXB_DELETE")]
		public async Task<ActionResult> Delete(string id)
		{
			if (id == null)
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}
			NhaXuatBan nhaXuatBan = await db.NhaXuatBans.FindAsync(id);
			if (nhaXuatBan == null)
			{
				return HttpNotFound();
			}
			return View(nhaXuatBan);
		}

		// POST: Admin/NhaXuatBans/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		[CheckPermission(Permission = "NXB_DELETE")]
		public async Task<ActionResult> DeleteConfirmed(string id)
		{
			NhaXuatBan nhaXuatBan = await db.NhaXuatBans.FindAsync(id);

			// Kiểm tra xem có sách nào thuộc NXB này không
			if (nhaXuatBan.Saches.Any())
			{
				TempData["ErrorMessage"] = "Không thể xóa nhà xuất bản này vì còn sách được xuất bản!";
				return RedirectToAction("Index");
			}

			db.NhaXuatBans.Remove(nhaXuatBan);
			await db.SaveChangesAsync();
			TempData["SuccessMessage"] = "Xóa nhà xuất bản thành công!";
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