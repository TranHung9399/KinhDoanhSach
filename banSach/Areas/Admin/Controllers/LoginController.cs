using banSach.Models;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;

namespace banSach.Areas.Admin.Controllers
{
    public class LoginController : Controller
    {
        private QLBanSachEntities db = new QLBanSachEntities();

        // GET: Admin/Login
        public ActionResult Index()
        {
            return View();
        }

        // POST: Admin/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string TenTK, string MatKhau)
        {
            if (string.IsNullOrEmpty(TenTK) || string.IsNullOrEmpty(MatKhau))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin đăng nhập.";
                return View();
            }

            var user = db.NhanViens.FirstOrDefault(u => u.TenTK == TenTK && u.MatKhau == MatKhau);

            if (user != null)
            {
                // ✅ KIỂM TRA TRẠNG THÁI TÀI KHOẢN NHÂN VIÊN
                if (user.TrangThai == false || user.TrangThai == null)
                {
                    TempData["ErrorMessage"] = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản lý để được hỗ trợ!";
                    return View();
                }
                
                FormsAuthentication.SetAuthCookie(user.TenTK, false);
                Session["AdminUser"] = user;
                var chucVu = db.ChucVus.FirstOrDefault(cv => cv.MaCV == user.MaCV);
                Session["ChucVu"] = chucVu != null ? chucVu.TenCV : "Unknown";
                return RedirectToAction("Index", "HomeAdmin", new { Area = "Admin" });
            }
            else
            {
                TempData["ErrorMessage"] = "Tên tài khoản hoặc mật khẩu không đúng.";
                return View();
            }
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Abandon();
            return RedirectToAction("Index", "Login");
        }
    }
}