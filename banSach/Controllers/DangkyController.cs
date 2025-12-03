using banSach.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Data.Entity;

namespace banSach.Controllers
{
    public class DangkyController : BaseController
    {

        // GET: Register
        public ActionResult Index()
        {
            return View();
        }

        // POST: Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(KhachHang khachHang)
        {
            if (ModelState.IsValid)
            {
                // Lấy giá trị ConfirmPassword từ form
                string confirmPassword = Request.Form["ConfirmPassword"];

                // Kiểm tra xác nhận mật khẩu
                //if (khachHang.MatKhau != confirmPassword)
                //{
                //    TempData["Error"] = "Mật khẩu xác nhận không khớp!";
                //    return View(khachHang);
                //}

                // Kiểm tra điều kiện mật khẩu mạnh
                 if (khachHang.MatKhau.Length < 8 || !khachHang.MatKhau.Any(char.IsUpper) || !khachHang.MatKhau.Any(char.IsLower) || !khachHang.MatKhau.Any(char.IsDigit))
                {
                    TempData["Error"] = "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường và chữ số.";
                    return View(khachHang);
                }

                // Tối ưu: AsNoTracking() cho read-only check
                // Kiểm tra email hoặc số điện thoại đã tồn tại
                if (await db.KhachHangs.AsNoTracking().AnyAsync(kh => kh.Email == khachHang.Email))
                {
                    TempData["Error"] = "Email đã tồn tại. Nếu bạn quên mật khẩu, bạn có thể <a href='" + Url.Action("ForgotPassword", "Dangnhap") + "' style='color:#1976d2;text-decoration:underline;'>thiết lập lại mật khẩu tại đây</a>.";
                    return View(khachHang);
                }

                // Tạo mã khách hàng tự động (KH001, KH002,...)
                // Tối ưu: Dùng async và Max tại SQL level
                var lastCustomer = await db.KhachHangs
                    .OrderByDescending(kh => kh.MaKH)
                    .FirstOrDefaultAsync();
                int newId = lastCustomer == null ? 1 : int.Parse(lastCustomer.MaKH.Replace("KH", "")) + 1;
                khachHang.MaKH = $"KH{newId:D3}";

                // Lưu vào cơ sở dữ liệu
                db.KhachHangs.Add(khachHang);
                await db.SaveChangesAsync();

                TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Index", "Dangnhap");
            }

            // Nếu ModelState không hợp lệ, trả lại view với thông tin đã nhập
            TempData["Error"] = "Vui lòng kiểm tra lại thông tin đăng ký.";
            return View(khachHang);
        }
    }
}