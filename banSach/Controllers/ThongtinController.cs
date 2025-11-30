using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using banSach.Models;
using PagedList;

namespace banSach.Controllers
{
    public class ThongtinController : BaseController
    {

        // GET: Thongtin
        public ActionResult Index(int? page)
        {
            if (Session["KhachHang"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem thông tin cá nhân!";
                return RedirectToAction("Index", "Dangnhap");
            }

            var maKH = Session["MaKH"]?.ToString();
            var khachHang = db.KhachHangs.FirstOrDefault(kh => kh.MaKH == maKH);
            if (khachHang == null)
            {
                Session.Clear();
                TempData["ErrorMessage"] = "Thông tin tài khoản không tồn tại!";
                return RedirectToAction("Index", "Dangnhap");
            }

            // ✅ Pagination settings
            int pageSize = 5; // 5 đơn hàng mỗi trang
            int pageNumber = (page ?? 1);

            // ✅ CẬP NHẬT: Query đơn hàng theo MaKH với phân trang
            var donHangs = db.DonDatHangs
                            .Include(dh => dh.ChiTietDonHangs.Select(ct => ct.Sach))
                            .Include(dh => dh.ThanhToans)  // ✅ THÊM: Include ThanhToans
                            .Where(dh => dh.MaKH == maKH)  // ✅ Query theo Foreign Key MaKH
                            .OrderByDescending(dh => dh.NgayDat)
                            .ToPagedList(pageNumber, pageSize); // ✅ Áp dụng phân trang

            // ✅ CẬP NHẬT: Sử dụng TongTien từ database (nếu có)
            var tongTienDict = new Dictionary<string, decimal>();
            foreach (var dh in donHangs)
            {
                decimal tongTien;
                
                // Nếu TongTien đã được lưu trong database thì dùng (dữ liệu mới)
                if (dh.TongTien.HasValue && dh.TongTien.Value > 0)
                {
                    tongTien = dh.TongTien.Value;
                }
                else
                {
                    // Tính lại cho dữ liệu cũ (chưa có TongTien)
                    var tienHang = dh.ChiTietDonHangs.Sum(ct => (ct.SoLuong ?? 0) * (ct.DonGia ?? 0));
                    var phiShip = dh.PhiVanChuyen ?? 0;
                    tongTien = tienHang + phiShip;
                }
                
                tongTienDict[dh.MaDonHang] = tongTien;
            }

            ViewBag.DonHangs = donHangs;
            ViewBag.TongTienDict = tongTienDict;

            return View(khachHang);
        }

        // POST: Thongtin/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(KhachHang model)
        {
            if (Session["KhachHang"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để chỉnh sửa thông tin!";
                return RedirectToAction("Index", "Dangnhap");
            }

            if (ModelState.IsValid)
            {
                var maKH = Session["MaKH"]?.ToString();
                var khachHang = db.KhachHangs.FirstOrDefault(kh => kh.MaKH == maKH);
                if (khachHang == null)
                {
                    Session.Clear();
                    TempData["ErrorMessage"] = "Thông tin tài khoản không tồn tại!";
                    return RedirectToAction("Index", "Dangnhap");
                }

                // Kiểm tra trùng số điện thoại
                if (!string.IsNullOrEmpty(model.SoDienThoai) && db.KhachHangs.Any(kh => kh.SoDienThoai == model.SoDienThoai && kh.MaKH != maKH))
                {
                    TempData["ErrorMessage"] = "Số điện thoại đã được sử dụng!";
                    return View("Index", model);
                }
                // Kiểm tra trùng email
                if (!string.IsNullOrEmpty(model.Email) && db.KhachHangs.Any(kh => kh.Email == model.Email && kh.MaKH != maKH))
                {
                    TempData["ErrorMessage"] = "Email đã được sử dụng!";
                    return View("Index", model);
                }

                khachHang.HoTen = model.HoTen;
                khachHang.SoDienThoai = model.SoDienThoai;
                khachHang.Email = model.Email;
                khachHang.DiaChi = model.DiaChi;
                khachHang.NgaySinh = model.NgaySinh;

                db.SaveChanges();

                Session["HoTen"] = khachHang.HoTen;
                Session["KhachHang"] = khachHang;

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Index");
            }

            TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại!";
            return View("Index", model);
        }

		// POST: Thongtin/DoiMatKhau
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult DoiMatKhau(string oldPassword, string newPassword, string confirmPassword)
		{
			if (Session["KhachHang"] == null)
			{
				TempData["ErrorMessage"] = "Vui lòng đăng nhập để đổi mật khẩu!";
				return RedirectToAction("Index", "Dangnhap");
			}

			var maKH = Session["MaKH"]?.ToString();
			var khachHang = db.KhachHangs.FirstOrDefault(kh => kh.MaKH == maKH);
			if (khachHang == null)
			{
				Session.Clear();
				TempData["ErrorMessage"] = "Thông tin tài khoản không tồn tại!";
				return RedirectToAction("Index", "Dangnhap");
			}

			// ✅ SỬA: Trim khoảng trắng và kiểm tra null
			var oldPwd = (oldPassword ?? "").Trim();
			var storedPwd = (khachHang.MatKhau ?? "").Trim();

			// ✅ THÊM: Debug log để kiểm tra
			System.Diagnostics.Debug.WriteLine($"Old Password Input: '{oldPwd}' (Length: {oldPwd.Length})");
			System.Diagnostics.Debug.WriteLine($"Stored Password: '{storedPwd}' (Length: {storedPwd.Length})");
			System.Diagnostics.Debug.WriteLine($"Are Equal: {oldPwd == storedPwd}");

			if (oldPwd != storedPwd)
			{
				TempData["ErrorMessage"] = "Mật khẩu cũ không đúng!";
				return RedirectToAction("Index");
			}

			// ✅ SỬA: Trim và validate mật khẩu mới
			var newPwd = (newPassword ?? "").Trim();
			var confirmPwd = (confirmPassword ?? "").Trim();

			if (string.IsNullOrEmpty(newPwd) || newPwd.Length < 6)
			{
				TempData["ErrorMessage"] = "Mật khẩu mới phải có ít nhất 6 ký tự!";
				return RedirectToAction("Index");
			}

			if (newPwd != confirmPwd)
			{
				TempData["ErrorMessage"] = "Mật khẩu mới và xác nhận không khớp!";
				return RedirectToAction("Index");
			}

			// ✅ SỬA: Lưu mật khẩu đã trim
			khachHang.MatKhau = newPwd;
			db.SaveChanges();

			TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
			return RedirectToAction("Index");
		}

		public ActionResult HuyDonHang(string id)
        {
            if (Session["KhachHang"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập!";
                return RedirectToAction("Index", "Dangnhap");
            }

            var maKH = Session["MaKH"]?.ToString();
            var donHang = db.DonDatHangs.FirstOrDefault(dh => dh.MaDonHang == id && dh.MaKH == maKH);
            
            if (donHang != null && donHang.TrangThai == "Chờ xác nhận")
            {
                donHang.TrangThai = "Đã hủy";
                
                // ✅ THÊM: Cập nhật trạng thái thanh toán
                var thanhToan = db.ThanhToans.FirstOrDefault(t => t.MaDonHang == id);
                if (thanhToan != null)
                {
                    thanhToan.TrangThaiThanhToan = "Đã hủy";
                }
                
                // ✅ THÊM: Hoàn lại số lượng sách vào kho
                var chiTietDonHangs = db.ChiTietDonHangs.Where(ct => ct.MaDonHang == id).ToList();
                foreach (var chiTiet in chiTietDonHangs)
                {
                    var sach = db.Saches.Find(chiTiet.MaSach);
                    if (sach != null)
                    {
                        sach.SoLuongTon += chiTiet.SoLuong ?? 0;
                    }
                }
                
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đơn hàng đã được hủy thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng.";
            }
            return RedirectToAction("Index");
        }

        // GET: Thongtin/ChiTietDonHang
        public ActionResult ChiTietDonHang(string id)
        {
            if (Session["KhachHang"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem chi tiết đơn hàng!";
                return RedirectToAction("Index", "Dangnhap");
            }

            var maKH = Session["MaKH"]?.ToString();
            
            // ✅ CẬP NHẬT: Kiểm tra đơn hàng thuộc về khách hàng
            var donHang = db.DonDatHangs
                .Include(dh => dh.ThanhToans)
                .Include(dh => dh.SuDungMaGiamGias) // ✅ THÊM: Include SuDungMaGiamGias
                .FirstOrDefault(dh => dh.MaDonHang == id && dh.MaKH == maKH);

            if (donHang == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("Index");
            }

            var chiTiet = db.ChiTietDonHangs
                            .Include(ct => ct.Sach)
                            .Include(ct => ct.Sach.VietSaches.Select(v => v.TacGia)) // ✅ THÊM: Include tác giả
                            .Where(ct => ct.MaDonHang == id)
                            .ToList();

            // ✅ Sử dụng TongTien từ database
            decimal tongTien = donHang.TongTien ?? chiTiet.Sum(ct => (ct.SoLuong ?? 0) * (ct.DonGia ?? 0));

            // ✅ THÊM: Lấy thông tin mã giảm giá (nếu có)
            if (!string.IsNullOrEmpty(donHang.MaGiamGia))
            {
                var suDungMGG = db.SuDungMaGiamGias
                    .Include(s => s.MaGiamGia)
                    .FirstOrDefault(s => s.MaDonHang == id);
                ViewBag.SuDungMaGiamGia = suDungMGG;
            }

            ViewBag.MaDonHang = id;
            ViewBag.NgayDat = donHang.NgayDat;
            ViewBag.TrangThai = donHang.TrangThai;
            ViewBag.TongTien = tongTien;
            ViewBag.DonHang = donHang;

            return View(chiTiet);
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
