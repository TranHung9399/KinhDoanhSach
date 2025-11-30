using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using banSach.Models;
using PagedList;

namespace bansach.Areas.Admin.Controllers
{
    //[AdminAuthorize(Roles = "Quản Lý")]
    public class ChucNangsController : Controller
    {
        private QLBanSachEntities db = new QLBanSachEntities();

        // GET: Admin/ChucNangs
        public ActionResult Index(int? page, string searchString)
        {
            var chucNangs = db.ChucNangs.AsQueryable();
            ViewBag.CurrentFilter = searchString;

            if (!string.IsNullOrEmpty(searchString))
            {
                chucNangs = chucNangs.Where(c => c.MaChucNang.Contains(searchString) || c.TenChucNang.Contains(searchString));
            }

            ViewBag.TongChucNang = chucNangs.Count();

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(chucNangs.OrderBy(c => c.MaChucNang).ToPagedList(pageNumber, pageSize));
        }

        // GET: Admin/ChucNangs/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ChucNang chucNang = db.ChucNangs.Find(id);
            if (chucNang == null)
            {
                return HttpNotFound();
            }
            return View(chucNang);
        }

        // GET: Admin/ChucNangs/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/ChucNangs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaChucNang,TenChucNang")] ChucNang chucNang)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra mã chức năng đã tồn tại chưa
                if (db.ChucNangs.Any(c => c.MaChucNang == chucNang.MaChucNang))
                {
                    ModelState.AddModelError("MaChucNang", "Mã chức năng đã tồn tại!");
                    return View(chucNang);
                }

                db.ChucNangs.Add(chucNang);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Thêm chức năng thành công!";
                return RedirectToAction("Index");
            }

            return View(chucNang);
        }

        // GET: Admin/ChucNangs/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ChucNang chucNang = db.ChucNangs.Find(id);
            if (chucNang == null)
            {
                return HttpNotFound();
            }
            return View(chucNang);
        }

        // POST: Admin/ChucNangs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaChucNang,TenChucNang")] ChucNang chucNang)
        {
            if (ModelState.IsValid)
            {
                db.Entry(chucNang).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Cập nhật chức năng thành công!";
                return RedirectToAction("Index");
            }
            return View(chucNang);
        }

        // GET: Admin/ChucNangs/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ChucNang chucNang = db.ChucNangs.Find(id);
            if (chucNang == null)
            {
                return HttpNotFound();
            }
            return View(chucNang);
        }

        // POST: Admin/ChucNangs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            try
            {
                ChucNang chucNang = db.ChucNangs.Find(id);
                
                // Kiểm tra xem chức năng có đang được sử dụng không
                if (chucNang.Quyens.Any())
                {
                    TempData["ErrorMessage"] = "Không thể xóa chức năng này vì đang được sử dụng trong quyền!";
                    return RedirectToAction("Index");
                }

                db.ChucNangs.Remove(chucNang);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Xóa chức năng thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }
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
