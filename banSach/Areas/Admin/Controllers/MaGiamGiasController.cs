using System;
using System.Linq;
using System.Web.Mvc;
using banSach.Models;
using PagedList;
using System.Net;
using System.Data.Entity;

namespace banSach.Areas.Admin.Controllers
{
    [AdminAuthorize(Roles = "Quản Lý")]
    [CheckPermission]
    public class MaGiamGiasController : Controller
    {
        private QLBanSachEntities db = new QLBanSachEntities();

        // GET: Admin/MaGiamGias
        [CheckPermission(Permission = "MGG_VIEW")]
        public ActionResult Index(string searchString, string status, int? page)
        {
            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentStatus = status;

            var maGiamGias = db.MaGiamGias.AsQueryable();

            // Lọc theo từ khóa tìm kiếm
            if (!String.IsNullOrEmpty(searchString))
            {
                maGiamGias = maGiamGias.Where(m => m.MaCode.Contains(searchString) || m.TenChuongTrinh.Contains(searchString));
            }

            // Lọc theo trạng thái
            var now = DateTime.Now;
            if (!string.IsNullOrEmpty(status))
            {
                switch (status)
                {
                    case "active":
                        maGiamGias = maGiamGias.Where(m => m.NgayBatDau <= now && m.NgayKetThuc >= now && m.SoLuongMa > (m.DaSuDung ?? 0));
                        break;
                    case "upcoming":
                        maGiamGias = maGiamGias.Where(m => m.NgayBatDau > now);
                        break;
                    case "expired":
                        maGiamGias = maGiamGias.Where(m => m.NgayKetThuc < now);
                        break;
                    case "outofstock":
                        maGiamGias = maGiamGias.Where(m => m.SoLuongMa <= (m.DaSuDung ?? 0));
                        break;
                }
            }

            maGiamGias = maGiamGias.OrderByDescending(m => m.NgayBatDau);

            int pageSize = 10;
            int pageNumber = (page ?? 1);

            return View(maGiamGias.ToPagedList(pageNumber, pageSize));
        }

        // GET: Admin/MaGiamGias/Details/5
        [CheckPermission(Permission = "MGG_DETAIL")]
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MaGiamGia maGiamGia = db.MaGiamGias.Find(id);
            if (maGiamGia == null)
            {
                return HttpNotFound();
            }
            return View(maGiamGia);
        }

        // GET: Admin/MaGiamGias/Create
        [CheckPermission(Permission = "MGG_CREATE")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/MaGiamGias/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CheckPermission(Permission = "MGG_CREATE")]
        public ActionResult Create([Bind(Include = "MaCode,TenChuongTrinh,PhanTramGiam,SoTienGiam,NgayBatDau,NgayKetThuc,SoLuongMa,DaSuDung")] MaGiamGia maGiamGia)
        {
            if (ModelState.IsValid)
            {
                db.MaGiamGias.Add(maGiamGia);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(maGiamGia);
        }

        // GET: Admin/MaGiamGias/Edit/5
        [CheckPermission(Permission = "MGG_EDIT")]
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MaGiamGia maGiamGia = db.MaGiamGias.Find(id);
            if (maGiamGia == null)
            {
                return HttpNotFound();
            }
            return View(maGiamGia);
        }

        // POST: Admin/MaGiamGias/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CheckPermission(Permission = "MGG_EDIT")]
        public ActionResult Edit([Bind(Include = "MaCode,TenChuongTrinh,PhanTramGiam,SoTienGiam,NgayBatDau,NgayKetThuc,SoLuongMa,DaSuDung")] MaGiamGia maGiamGia)
        {
            if (ModelState.IsValid)
            {
                db.Entry(maGiamGia).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(maGiamGia);
        }

        // GET: Admin/MaGiamGias/Delete/5
        [CheckPermission(Permission = "MGG_DELETE")]
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MaGiamGia maGiamGia = db.MaGiamGias.Find(id);
            if (maGiamGia == null)
            {
                return HttpNotFound();
            }
            return View(maGiamGia);
        }

        // POST: Admin/MaGiamGias/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [CheckPermission(Permission = "MGG_DELETE")]
        public ActionResult DeleteConfirmed(string id)
        {
            MaGiamGia maGiamGia = db.MaGiamGias.Find(id);
            db.MaGiamGias.Remove(maGiamGia);
            db.SaveChanges();
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
