using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using banSach.Models;
using PagedList;

namespace bansach.Areas.Admin.Controllers
{
    [AdminAuthorize(Roles = "Quản Lý")]
    public class QuyensController : Controller
    {
        private QLBanSachEntities db = new QLBanSachEntities();

        // GET: Admin/Quyens
        public ActionResult Index(int? page, string searchString)
        {
            var quyens = db.Quyens.Include(q => q.ChucNang);
            ViewBag.CurrentFilter = searchString;

            if (!string.IsNullOrEmpty(searchString))
            {
                quyens = quyens.Where(q => q.MaQuyen.Contains(searchString) || q.TenQuyen.Contains(searchString));
            }

            ViewBag.TongQuyen = quyens.Count();

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(quyens.OrderBy(q => q.MaQuyen).ToPagedList(pageNumber, pageSize));
        }

        // GET: Admin/Quyens/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Quyen quyen = db.Quyens.Include(q => q.ChucNang).Include(q => q.ChucVus).FirstOrDefault(q => q.MaQuyen == id);
            if (quyen == null)
            {
                return HttpNotFound();
            }
            return View(quyen);
        }

        // GET: Admin/Quyens/Create
        public ActionResult Create()
        {
            ViewBag.MaChucNang = new SelectList(db.ChucNangs, "MaChucNang", "TenChucNang");
            return View();
        }

        // POST: Admin/Quyens/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaQuyen,TenQuyen,MaChucNang")] Quyen quyen)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra mã quyền đã tồn tại chưa
                if (db.Quyens.Any(q => q.MaQuyen == quyen.MaQuyen))
                {
                    ModelState.AddModelError("MaQuyen", "Mã quyền đã tồn tại!");
                    ViewBag.MaChucNang = new SelectList(db.ChucNangs, "MaChucNang", "TenChucNang", quyen.MaChucNang);
                    return View(quyen);
                }

                db.Quyens.Add(quyen);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Thêm quyền thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.MaChucNang = new SelectList(db.ChucNangs, "MaChucNang", "TenChucNang", quyen.MaChucNang);
            return View(quyen);
        }

        // GET: Admin/Quyens/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Quyen quyen = db.Quyens.Find(id);
            if (quyen == null)
            {
                return HttpNotFound();
            }
            ViewBag.MaChucNang = new SelectList(db.ChucNangs, "MaChucNang", "TenChucNang", quyen.MaChucNang);
            return View(quyen);
        }

        // POST: Admin/Quyens/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaQuyen,TenQuyen,MaChucNang")] Quyen quyen)
        {
            if (ModelState.IsValid)
            {
                db.Entry(quyen).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Cập nhật quyền thành công!";
                return RedirectToAction("Index");
            }
            ViewBag.MaChucNang = new SelectList(db.ChucNangs, "MaChucNang", "TenChucNang", quyen.MaChucNang);
            return View(quyen);
        }

        // GET: Admin/Quyens/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Quyen quyen = db.Quyens.Include(q => q.ChucNang).FirstOrDefault(q => q.MaQuyen == id);
            if (quyen == null)
            {
                return HttpNotFound();
            }
            return View(quyen);
        }

        // POST: Admin/Quyens/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            try
            {
                Quyen quyen = db.Quyens.Find(id);
                
                // Kiểm tra xem quyền có đang được gán cho chức vụ nào không
                if (quyen.ChucVus.Any())
                {
                    TempData["ErrorMessage"] = "Không thể xóa quyền này vì đang được gán cho chức vụ!";
                    return RedirectToAction("Index");
                }

                db.Quyens.Remove(quyen);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Xóa quyền thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // GET: Admin/Quyens/ManageChucVu/5
        public ActionResult ManageChucVu(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            Quyen quyen = db.Quyens.Include(q => q.ChucNang).Include(q => q.ChucVus).FirstOrDefault(q => q.MaQuyen == id);
            if (quyen == null)
            {
                return HttpNotFound();
            }

            ViewBag.AllChucVus = db.ChucVus.ToList();
            return View(quyen);
        }

        // POST: Admin/Quyens/AddChucVu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddChucVu(string maQuyen, string maCV)
        {
            try
            {
                var quyen = db.Quyens.Include(q => q.ChucVus).FirstOrDefault(q => q.MaQuyen == maQuyen);
                var chucVu = db.ChucVus.Find(maCV);

                if (quyen != null && chucVu != null)
                {
                    if (!quyen.ChucVus.Contains(chucVu))
                    {
                        quyen.ChucVus.Add(chucVu);
                        db.SaveChanges();
                        
                        // Làm mới quyền của các user đang online có chức vụ này
                        RefreshPermissionsForRole(maCV);
                        
                        TempData["SuccessMessage"] = "Cấp quyền cho chức vụ thành công!";
                    }
                    else
                    {
                        TempData["InfoMessage"] = "Chức vụ này đã có quyền!";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("ManageChucVu", new { id = maQuyen });
        }

        // POST: Admin/Quyens/RemoveChucVu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveChucVu(string maQuyen, string maCV)
        {
            try
            {
                var quyen = db.Quyens.Include(q => q.ChucVus).FirstOrDefault(q => q.MaQuyen == maQuyen);
                var chucVu = db.ChucVus.Find(maCV);

                if (quyen != null && chucVu != null)
                {
                    if (quyen.ChucVus.Contains(chucVu))
                    {
                        quyen.ChucVus.Remove(chucVu);
                        db.SaveChanges();
                        
                        // Làm mới quyền của các user đang online có chức vụ này
                        RefreshPermissionsForRole(maCV);
                        
                        TempData["SuccessMessage"] = "Thu hồi quyền từ chức vụ thành công!";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("ManageChucVu", new { id = maQuyen });
        }

        /// <summary>
        /// Làm mới quyền cho user hiện tại nếu là chức vụ bị thay đổi
        /// </summary>
        private void RefreshPermissionsForRole(string maCV)
        {
            // Kiểm tra nếu user hiện tại có chức vụ này
            var currentUser = Session["AdminUser"] as NhanVien;
            if (currentUser != null && currentUser.MaCV == maCV)
            {
                // Reload permissions từ database
                var chucVu = db.ChucVus
                    .Include("Quyens")
                    .FirstOrDefault(cv => cv.MaCV == maCV);
                
                if (chucVu != null)
                {
                    var permissions = chucVu.Quyens.Select(q => q.MaQuyen).ToList();
                    Session["UserPermissions"] = permissions;
                }
                else
                {
                    Session["UserPermissions"] = new System.Collections.Generic.List<string>();
                }
            }
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
