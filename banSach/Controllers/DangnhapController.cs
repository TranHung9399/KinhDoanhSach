using banSach.Models;
using banSach.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Data.Entity;

namespace banSach.Controllers
{
    public class DangnhapController : BaseController
    {
        // GET: Login
        public ActionResult Index()
        {
            return View();
        }

        // POST: Dangnhap
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(FormCollection form)
        {
            string username = form["Username"];
            string password = form["Password"];

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Vui lòng nhập số điện thoại hoặc email và mật khẩu!";
                return View();
            }

            var khachHang = await db.KhachHangs
                .FirstOrDefaultAsync(kh => (kh.SoDienThoai == username || kh.Email == username) && kh.MatKhau == password);

            if (khachHang != null)
            {
                // ✅ KIỂM TRA TRẠNG THÁI TÀI KHOẢN
                if (khachHang.TrangThai == 0)
                {
                    TempData["Error"] = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên để được hỗ trợ!";
                    return View();
                }
                
                Session["KhachHang"] = khachHang;
                Session["MaKH"] = khachHang.MaKH;
                Session["HoTen"] = khachHang.HoTen;

				if (Session["Cart"] != null)
				{
					var cart = (List<ChiTietGioHang>)Session["Cart"];
					string maKH = khachHang.MaKH;

					var gioHang = await db.GioHangs.FirstOrDefaultAsync(g => g.MaKH == maKH);
					if (gioHang == null)
					{
						gioHang = new GioHang { MaGioHang = Guid.NewGuid().ToString(), MaKH = maKH, NgayTao = DateTime.Now };
						db.GioHangs.Add(gioHang);
						await db.SaveChangesAsync();
					}

					foreach (var item in cart)
					{
						var chiTiet = await db.ChiTietGioHangs.FirstOrDefaultAsync(ct => ct.MaGioHang == gioHang.MaGioHang && ct.MaSach == item.MaSach);
						if (chiTiet == null)
						{
							db.ChiTietGioHangs.Add(new ChiTietGioHang
							{
								MaChiTiet = Guid.NewGuid().ToString(),
								MaGioHang = gioHang.MaGioHang,
								MaSach = item.MaSach,
								SoLuong = item.SoLuong,
								DonGia = item.DonGia
							});
						}
						else
						{
							chiTiet.SoLuong += item.SoLuong;
						}
					}
					await db.SaveChangesAsync();
					Session["Cart"] = null; // Xoá session sau khi merge
				}
                TempData["Success"] = "Đăng nhập thành công!";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                TempData["Error"] = "Số điện thoại, email hoặc mật khẩu không đúng!";
                return View();
            }
        }
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> ForgotPassword(string Email)
        {
            var user = await db.KhachHangs.FirstOrDefaultAsync(k => k.Email == Email);
            if (user == null)
            {
                TempData["Error"] = "Email không tồn tại trong hệ thống!";
                return View();
            }

            // Tạo token ngẫu nhiên
            string token = Guid.NewGuid().ToString();
            Session["ResetToken_" + token] = user.MaKH;

            // Tạo link đặt lại mật khẩu
            string resetLink = Url.Action("ResetPassword", "Dangnhap", new { token = token }, protocol: Request.Url.Scheme);

            string subject = "Yêu cầu đặt lại mật khẩu";
            string body = $@"
<p>Xin chào <b>{user.HoTen}</b>,</p>
<p>Bạn đã yêu cầu đặt lại mật khẩu. Vui lòng nhấn vào nút bên dưới để thiết lập lại:</p>
<p style='text-align: center; margin: 30px 0;'>
    <a href='{resetLink}' target='_blank' 
       style='display: inline-block; padding: 12px 24px; background-color: #ff6f00; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;'>
        Đặt Lại Mật Khẩu
    </a>
</p>
<p>Nếu bạn không yêu cầu điều này, vui lòng bỏ qua email.</p>";


            SendMail sendMail = new SendMail();
            // Note: SendMailFunction is likely synchronous. 
            // Ideally we should refactor SendMail to be async, but for now wrapping it in Task.Run prevents blocking the request thread.
            await Task.Run(() => sendMail.SendMailFunction(user.Email, subject, body));
            
            return View();
        }

        public ActionResult ResetPassword(string token)
        {
            if (Session["ResetToken_" + token] == null)
            {
                return Content("Link không hợp lệ hoặc đã hết hạn.");
            }
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> ResetPassword(string token, string newPassword)
        {
            // Kiểm tra xem token có hợp lệ không
            var maKH = Session["ResetToken_" + token];
            if (maKH == null)
            {
                TempData["Error"] = "Token không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction("ResetPassword", new { token = token }); // Quay lại trang reset password với thông báo lỗi
            }

            // Tìm người dùng từ cơ sở dữ liệu
            var user = await db.KhachHangs.FindAsync(maKH);
            if (user == null)
            {
                TempData["Error"] = "Người dùng không tồn tại!";
                return RedirectToAction("ResetPassword", new { token = token }); // Quay lại trang reset password với thông báo lỗi
            }

            // Kiểm tra tính mạnh mẽ của mật khẩu
            if (newPassword.Length < 8 || !newPassword.Any(char.IsUpper) || !newPassword.Any(char.IsLower) || !newPassword.Any(char.IsDigit))
            {
                TempData["Error"] = "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường và chữ số.";
                return RedirectToAction("ResetPassword", new { token = token }); // Quay lại trang reset password với thông báo lỗi
            }

            // Lưu mật khẩu mới vào cơ sở dữ liệu (không mã hóa)
            user.MatKhau = newPassword; // Lưu mật khẩu trực tiếp

            // Lưu thay đổi vào cơ sở dữ liệu
            await db.SaveChangesAsync();

            // Xóa session và thông báo thành công
            Session.Remove("ResetToken_" + token);
            TempData["Success"] = "Đổi mật khẩu thành công!";

            // Chuyển hướng về trang đăng nhập
            return RedirectToAction("Index", "Dangnhap");
        }

        // Đăng nhập Google
        [AllowAnonymous]
        public ActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleCallback", "Dangnhap") };
            HttpContext.GetOwinContext().Authentication.Challenge(properties, "Google");
            return new HttpUnauthorizedResult();
        }

        // Callback từ Google
        [AllowAnonymous]
        public async Task<ActionResult> GoogleCallback()
        {
            try
            {
                var loginInfo = await HttpContext.GetOwinContext().Authentication.AuthenticateAsync("ExternalCookie");
                
                if (loginInfo == null || loginInfo.Identity == null)
                {
                    TempData["Error"] = "Đăng nhập Google thất bại!";
                    return RedirectToAction("Index");
                }

                var email = loginInfo.Identity.FindFirst(ClaimTypes.Email)?.Value;
                var name = loginInfo.Identity.FindFirst(ClaimTypes.Name)?.Value;
                var picture = loginInfo.Identity.FindFirst("urn:google:picture")?.Value;

                if (string.IsNullOrEmpty(email))
                {
                    TempData["Error"] = "Không lấy được email từ Google!";
                    return RedirectToAction("Index");
                }

                // Tìm hoặc tạo mới khách hàng
                var khachHang = await db.KhachHangs.FirstOrDefaultAsync(kh => kh.Email == email);
                
                if (khachHang == null)
                {
                    // Tạo mã khách hàng tự động
                    var lastCustomer = await db.KhachHangs
                        .OrderByDescending(kh => kh.MaKH)
                        .FirstOrDefaultAsync();
                    int newId = lastCustomer == null ? 1 : int.Parse(lastCustomer.MaKH.Replace("KH", "")) + 1;
                    
                    khachHang = new KhachHang
                    {
                        MaKH = $"KH{newId:D3}",
                        HoTen = name ?? "Google User",
                        Email = email,
                        MatKhau = Guid.NewGuid().ToString(), // Random password for Google accounts
                        SoDienThoai = "", // Có thể yêu cầu user cập nhật sau
                        DiaChi = "",
                        TrangThai = 1 // ✅ Mặc định: Tài khoản mới = Active
                    };
                    
                    db.KhachHangs.Add(khachHang);
                    await db.SaveChangesAsync();
                }
                else
                {
                    // ✅ KIỂM TRA TRẠNG THÁI TÀI KHOẢN GOOGLE
                    if (khachHang.TrangThai == 0)
                    {
                        TempData["Error"] = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên để được hỗ trợ!";
                        HttpContext.GetOwinContext().Authentication.SignOut("ExternalCookie");
                        return RedirectToAction("Index");
                    }
                }

                // Merge cart nếu có
                if (Session["Cart"] != null)
                {
                    var cart = (List<ChiTietGioHang>)Session["Cart"];
                    string maKH = khachHang.MaKH;

                    var gioHang = await db.GioHangs.FirstOrDefaultAsync(g => g.MaKH == maKH);
                    if (gioHang == null)
                    {
                        gioHang = new GioHang { MaGioHang = Guid.NewGuid().ToString(), MaKH = maKH, NgayTao = DateTime.Now };
                        db.GioHangs.Add(gioHang);
                        await db.SaveChangesAsync();
                    }

                    foreach (var item in cart)
                    {
                        var chiTiet = await db.ChiTietGioHangs.FirstOrDefaultAsync(ct => ct.MaGioHang == gioHang.MaGioHang && ct.MaSach == item.MaSach);
                        if (chiTiet == null)
                        {
                            db.ChiTietGioHangs.Add(new ChiTietGioHang
                            {
                                MaChiTiet = Guid.NewGuid().ToString(),
                                MaGioHang = gioHang.MaGioHang,
                                MaSach = item.MaSach,
                                SoLuong = item.SoLuong,
                                DonGia = item.DonGia
                            });
                        }
                        else
                        {
                            chiTiet.SoLuong += item.SoLuong;
                        }
                    }
                    await db.SaveChangesAsync();
                    Session["Cart"] = null;
                }

                // Set session
                Session["KhachHang"] = khachHang;
                Session["MaKH"] = khachHang.MaKH;
                Session["HoTen"] = khachHang.HoTen;
                
                // Sign out external cookie
                HttpContext.GetOwinContext().Authentication.SignOut("ExternalCookie");
                
                TempData["Success"] = "Đăng nhập Google thành công!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Index");
            }
        }




        // GET: Dangnhap/Logout
        public ActionResult Logout()
        {
            // Lưu thông tin tạm để log (optional)
            var userName = Session["HoTen"]?.ToString();
            
            // Xóa toàn bộ session
            Session.Clear();
            Session.Abandon(); // Đảm bảo session được hủy hoàn toàn
            
            // Thông báo đăng xuất thành công
            TempData["Success"] = "Đăng xuất thành công!";
            
            // Log (optional)
            if (!string.IsNullOrEmpty(userName))
            {
                System.Diagnostics.Debug.WriteLine($"User {userName} logged out at {DateTime.Now}");
            }
            
            return RedirectToAction("Index", "Home");
        }
    }
}