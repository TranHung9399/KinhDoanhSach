using System;
using System.Net;
using System.Net.Mail;

namespace banSach.Helper
{
    public class SendMail
    {
		internal static string ToMD5(string matKhauCu)
		{
			throw new NotImplementedException();
		}

		public bool SendMailFunction(string to, string subject, string body)
        {
            string hostEmail = "smtp.gmail.com";
            int portEmail = 587;
            string emailSender = "hungpro123123@gmail.com";
            string passwordSender = "mbon umqw ekpy cvjb"; // Mật khẩu ứng dụng

            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(emailSender);
                mail.To.Add(to); // 🔹 Đúng biến
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient(hostEmail, portEmail);
                smtp.Credentials = new NetworkCredential(emailSender, passwordSender);
                smtp.EnableSsl = true; // 🔹 BẬT SSL
                smtp.Send(mail);

                smtp.Dispose(); // 🔹 Giải phóng tài nguyên

                Console.WriteLine("✅ Email đã gửi thành công!");
                return true;  // 🔹 Trả về true nếu gửi thành công
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi gửi mail: " + ex.Message);
                return false; // 🔹 Trả về false nếu có lỗi
            }
        }
    }
}
