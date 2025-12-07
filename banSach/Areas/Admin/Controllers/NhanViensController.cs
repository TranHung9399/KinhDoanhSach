using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.UI;
using banSach.Models;
using PagedList;

namespace banSach.Areas.Admin.Controllers
{
	[AdminAuthorize(Roles = "Quản Lý")]
	[CheckPermission]
	public class NhanViensController : Controller
	{
		private QLBanSachEntities db = new QLBanSachEntities();

		// GET: Admin/NhanViens
		[CheckPermission(Permission = "NV_VIEW")]
		public ActionResult Index(int? page, string searchString, string searchType)
		{
			var nhanViens = db.NhanViens.Include(n => n.ChucVu);
			ViewBag.CurrentFilter = searchString;
			ViewBag.CurrentSearchType = searchType ?? "name";

			if (!string.IsNullOrEmpty(searchString))
			{
				if (searchType == "id")
				{
					nhanViens = nhanViens.Where(n => n.MaNhanVien.Contains(searchString));
				}
				else
				{
					nhanViens = nhanViens.Where(n => n.HoTen.Contains(searchString));
				}
			}

			// Tính toán thống kê trước khi phân trang
			var allNhanViens = nhanViens.ToList();
			ViewBag.TongNhanVien = allNhanViens.Count;
			ViewBag.SoQuanLy = allNhanViens.Count(nv => nv.ChucVu != null && nv.ChucVu.TenCV.Contains("Quản Lý"));
			ViewBag.SoNhanVien = allNhanViens.Count(nv => nv.ChucVu != null && !nv.ChucVu.TenCV.Contains("Quản Lý"));
			ViewBag.SoChucVu = allNhanViens.Select(nv => nv.MaCV).Distinct().Count();

			int pageSize = 5;
			int pageNumber = (page ?? 1);
			return View(allNhanViens.OrderBy(n => n.MaNhanVien).ToPagedList(pageNumber, pageSize));
		}

		// GET: Admin/NhanViens/Details/5
		[CheckPermission(Permission = "NV_DETAIL")]
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
			if (string.IsNullOrEmpty(id))
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}

			NhanVien nhanVien = await db.NhanViens.FindAsync(id);
			if (nhanVien == null)
			{
				return HttpNotFound();
			}

			return View(nhanVien);
		}

		// POST: Admin/NhanViens/ToggleStatus
		[CheckPermission(Permission = "NV_EDIT")]
		public ActionResult ToggleStatus(string id)
		{
			var nhanVien = db.NhanViens.Find(id);
			if (nhanVien != null)
			{
				// Toggle status: true = Active, false = Inactive
				nhanVien.TrangThai = !(nhanVien.TrangThai ?? false);
				db.Entry(nhanVien).State = System.Data.Entity.EntityState.Modified;
				db.SaveChanges();
				TempData["Message"] = nhanVien.TrangThai == true ? "Đã kích hoạt tài khoản nhân viên!" : "Đã vô hiệu hóa tài khoản nhân viên!";
			}
			else
			{
				TempData["ErrorMessage"] = "Không tìm thấy nhân viên!";
			}
			return RedirectToAction("Index");
		}

		// GET: Admin/NhanViens/Create
		[CheckPermission(Permission = "NV_CREATE")]
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
			SetViewBagChucVu();
			return View();
		}

		// POST: Admin/NhanViens/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		[CheckPermission(Permission = "NV_CREATE")]
		public async Task<ActionResult> Create([Bind(Include = "HoTen,SoDienThoai,Email,TenTK,MatKhau,MaCV")] NhanVien nhanVien)
		{
			if (ModelState.IsValid)
			{
				// Sinh mã tự động
				var lastNV = db.NhanViens.OrderByDescending(nv => nv.MaNhanVien).FirstOrDefault();
				string newMaNV = "NV001";

				if (lastNV != null)
				{
					string lastMa = lastNV.MaNhanVien.Replace("NV", "");
					int so = int.Parse(lastMa) + 1;
					newMaNV = "NV" + so.ToString("D3");
				}

				nhanVien.MaNhanVien = newMaNV;

				db.NhanViens.Add(nhanVien);
				await db.SaveChangesAsync();

				// Thông báo thành công
				TempData["Message"] = "Nhân viên đã được thêm thành công!";
				return RedirectToAction("Index");
			}

			SetViewBagChucVu(nhanVien.MaCV);
			return View(nhanVien);
		}


		// GET: Admin/NhanViens/Edit/5
		[CheckPermission(Permission = "NV_EDIT")]
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
			if (string.IsNullOrEmpty(id))
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}

			NhanVien nhanVien = await db.NhanViens.FindAsync(id);
			if (nhanVien == null)
			{
				return HttpNotFound();
			}

			SetViewBagChucVu(nhanVien.MaCV);
			return View(nhanVien);
		}

		// POST: Admin/NhanViens/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		[CheckPermission(Permission = "NV_EDIT")]
		public async Task<ActionResult> Edit([Bind(Include = "MaNhanVien,HoTen,SoDienThoai,Email,TenTK,MatKhau,MaCV")] NhanVien nhanVien)
		{
			if (ModelState.IsValid)
			{
				db.Entry(nhanVien).State = EntityState.Modified;
				await db.SaveChangesAsync();

				// Thông báo thành công
				TempData["Message"] = "Chỉnh sửa nhân viên thành công!";
				return RedirectToAction("Index");
			}

			SetViewBagChucVu(nhanVien.MaCV);
			return View(nhanVien);
		}


		// GET: Admin/NhanViens/Delete/5
		[CheckPermission(Permission = "NV_DELETE")]
		public async Task<ActionResult> Delete(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}

			NhanVien nhanVien = await db.NhanViens.FindAsync(id);
			if (nhanVien == null)
			{
				return HttpNotFound();
			}

			return View(nhanVien);
		}

		// POST: Admin/NhanViens/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		[CheckPermission(Permission = "NV_DELETE")]
		public async Task<ActionResult> DeleteConfirmed(string id)
		{
			NhanVien nhanVien = await db.NhanViens.FindAsync(id);
			db.NhanViens.Remove(nhanVien);
			await db.SaveChangesAsync();

			// Thông báo thành công
			TempData["Message"] = "Nhân viên đã được xóa thành công!";
			return RedirectToAction("Index");
		}


		private void SetViewBagChucVu(string selectedMaCV = null)
		{
			ViewBag.MaCV = new SelectList(db.ChucVus, "MaCV", "TenCV", selectedMaCV);
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