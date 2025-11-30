using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using banSach.Models;
using banSach.ViewModels;
using PagedList;

namespace banSach.Areas.Admin.Controllers
{
	[AdminAuthorize(Roles = "Quản Lý")]
	[CheckPermission]
	public class TacGiasController : Controller
	{
		private QLBanSachEntities db = new QLBanSachEntities();

		// GET: Admin/TacGias
		[CheckPermission(Permission = "TG_VIEW")]
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

			var tacGias = db.TacGias.Include(t => t.VietSaches).AsQueryable();
			ViewBag.CurrentFilter = searchString;
			ViewBag.CurrentSearchType = searchType ?? "name";

			if (!string.IsNullOrEmpty(searchString))
			{
				if (searchType == "id")
				{
					tacGias = tacGias.Where(t => t.MaTG.Contains(searchString));
				}
				else
				{
					tacGias = tacGias.Where(t => t.TenTG.Contains(searchString));
				}
			}

			int pageSize = 10;
			int pageNumber = (page ?? 1);
			return View(tacGias.OrderBy(t => t.MaTG).ToPagedList(pageNumber, pageSize));
		}

		// GET: Admin/TacGias/Details/5
		[CheckPermission(Permission = "TG_DETAIL")]
		public ActionResult Details(string id)
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

			var tacGia = db.TacGias.Find(id);
			if (tacGia == null)
			{
				return HttpNotFound();
			}

			var dsSach = db.Saches.ToList();
			var dsViet = db.VietSaches.Where(v => v.MaTG == id).ToList();

			var viewModel = new TacGiaModel
			{
				MaTG = tacGia.MaTG,
				TenTG = tacGia.TenTG,
				DiaChi = tacGia.DiaChi,
				TieuSu = tacGia.TieuSu,
				DienThoai = tacGia.DienThoai,
				Saches = dsSach.Select(s => new SachChonViewModel
				{
					MaSach = s.MaSach,
					TenSach = s.TenSach,
					DuocChon = dsViet.Any(v => v.MaSach == s.MaSach),
					VaiTro = dsViet.FirstOrDefault(v => v.MaSach == s.MaSach)?.VaiTro
				}).ToList()
			};

			return View(viewModel);
		}

		// GET: Admin/TacGias/Create
		[CheckPermission(Permission = "TG_CREATE")]
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
			var allSachs = db.Saches.ToList();

			var model = new TacGiaModel
			{
				Saches = allSachs.Select(s => new SachChonViewModel
				{
					MaSach = s.MaSach,
					TenSach = s.TenSach,
					DuocChon = false,
					VaiTro = ""
				}).ToList()
			};

			return View(model);
		}

		// POST: Admin/TacGias/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		[CheckPermission(Permission = "TG_CREATE")]
		public ActionResult Create(TacGiaModel model)
		{
			if (ModelState.IsValid)
			{
				// Tạo mã tác giả mới
				var last = db.TacGias.OrderByDescending(t => t.MaTG).FirstOrDefault();
				string newMaTG = "TG001";
				if (last != null)
				{
					int so = int.Parse(last.MaTG.Substring(2)) + 1;
					newMaTG = "TG" + so.ToString("D3");
				}

				var tacGia = new TacGia
				{
					MaTG = newMaTG,
					TenTG = model.TenTG,
					DiaChi = model.DiaChi,
					TieuSu = model.TieuSu,
					DienThoai = model.DienThoai,
					Status = 1
				};

				db.TacGias.Add(tacGia);

				// Gán sách được chọn
				if (model.Saches != null)
				{
					foreach (var sach in model.Saches.Where(s => s.DuocChon))
					{
						db.VietSaches.Add(new VietSach
						{
							MaTG = newMaTG,
							MaSach = sach.MaSach,
							VaiTro = sach.VaiTro
						});
					}
				}

				db.SaveChanges();
				TempData["SuccessMessage"] = "Thêm tác giả thành công!";
				return RedirectToAction("Index");
			}

			return View(model);
		}

		// GET: Admin/TacGias/Edit/5
		[CheckPermission(Permission = "TG_EDIT")]
		public ActionResult Edit(string id)
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

			var tacGia = db.TacGias.Find(id);
			if (tacGia == null)
			{
				return HttpNotFound();
			}

			var sachs = db.Saches.ToList();
			var sachesChon = sachs.Select(s => new SachChonViewModel
			{
				MaSach = s.MaSach,
				TenSach = s.TenSach,
				DuocChon = tacGia.VietSaches.Any(ts => ts.MaSach == s.MaSach),
				VaiTro = tacGia.VietSaches.FirstOrDefault(ts => ts.MaSach == s.MaSach)?.VaiTro
			}).ToList();

			var model = new TacGiaModel
			{
				MaTG = tacGia.MaTG,
				TenTG = tacGia.TenTG,
				DiaChi = tacGia.DiaChi,
				TieuSu = tacGia.TieuSu,
				DienThoai = tacGia.DienThoai,
				Status = tacGia.Status ?? 1,
				Saches = sachesChon
			};

			return View(model);
		}

		// POST: Admin/TacGias/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		[CheckPermission(Permission = "TG_EDIT")]
		public ActionResult Edit(TacGiaModel model)
		{
			if (ModelState.IsValid)
			{
				var tacGia = db.TacGias.Find(model.MaTG);
				if (tacGia != null)
				{
					tacGia.TenTG = model.TenTG;
					tacGia.DiaChi = model.DiaChi;
					tacGia.TieuSu = model.TieuSu;
					tacGia.DienThoai = model.DienThoai;
					tacGia.Status = 1;

					// Cập nhật các sách đã viết
					if (model.Saches != null)
					{
						foreach (var sach in model.Saches.Where(s => s.DuocChon))
						{
							var vietSach = db.VietSaches.FirstOrDefault(vs => vs.MaTG == tacGia.MaTG && vs.MaSach == sach.MaSach);
							if (vietSach != null)
							{
								vietSach.VaiTro = sach.VaiTro;
							}
							else
							{
								db.VietSaches.Add(new VietSach
								{
									MaTG = tacGia.MaTG,
									MaSach = sach.MaSach,
									VaiTro = sach.VaiTro
								});
							}
						}
					}

					db.Entry(tacGia).State = EntityState.Modified;
					db.SaveChanges();
					TempData["SuccessMessage"] = "Cập nhật tác giả thành công!";
					return RedirectToAction("Index");
				}
				return HttpNotFound();
			}
			return View(model);
		}

		// GET: Admin/TacGias/Delete/5
		[CheckPermission(Permission = "TG_DELETE")]
		public ActionResult Delete(string id)
		{
			if (id == null)
			{
				return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
			}

			TacGia tacGia = db.TacGias.Find(id);
			if (tacGia == null)
			{
				return HttpNotFound();
			}

			return View(tacGia);
		}

		// POST: Admin/TacGias/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		[CheckPermission(Permission = "TG_DELETE")]
		public ActionResult DeleteConfirmed(string id)
		{
			TacGia tacGia = db.TacGias.Find(id);
			if (tacGia == null)
			{
				return HttpNotFound();
			}

			// Xóa các bản ghi liên quan trong bảng VietSach
			var vietSaches = db.VietSaches.Where(v => v.MaTG == id).ToList();
			db.VietSaches.RemoveRange(vietSaches);

			// Sau khi xóa các bản ghi trong bảng VietSach, xóa tác giả
			db.TacGias.Remove(tacGia);
			db.SaveChanges();

			TempData["SuccessMessage"] = "Xóa tác giả thành công!";
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