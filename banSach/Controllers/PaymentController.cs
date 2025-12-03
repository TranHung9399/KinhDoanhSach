using System;
using System.Linq;
using System.Web.Mvc;
using banSach.Models;
using System.Threading.Tasks;
using System.Data.Entity;

namespace banSach.Controllers
{
    public class PaymentController : Controller
    {
        private QLBanSachEntities db = new QLBanSachEntities();

        /// <summary>
        /// Callback từ MoMo sau khi thanh toán
        /// </summary>

        /// <summary>
        /// Hủy đơn hàng (chỉ được hủy khi đơn chưa xác nhận)
        /// </summary>
        public async Task<ActionResult> Cancel(string maDonHang)
        {
            if (Session["KhachHang"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập!";
                return RedirectToAction("Index", "Dangnhap");
            }

            try
            {
                var maKH = Session["MaKH"]?.ToString();
                var donHang = await db.DonDatHangs.FirstOrDefaultAsync(d => d.MaDonHang == maDonHang && d.MaKH == maKH);
                
                if (donHang == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng!";
                    return RedirectToAction("Index", "Thongtin");
                }

                // Chỉ cho phép hủy đơn "Chờ xác nhận"
                if (donHang.TrangThai != "Chờ xác nhận")
                {
                    TempData["ErrorMessage"] = "Chỉ có thể hủy đơn hàng đang chờ xác nhận!";
                    return RedirectToAction("Index", "Thongtin");
                }

                // Cập nhật trạng thái đơn hàng
                donHang.TrangThai = "Đã hủy";

                // Cập nhật trạng thái thanh toán
                var thanhToan = await db.ThanhToans.FirstOrDefaultAsync(t => t.MaDonHang == maDonHang);
                if (thanhToan != null)
                {
                    thanhToan.TrangThaiThanhToan = "Đã hủy";
                }

                // Hoàn lại số lượng sách
                var chiTietDonHangs = await db.ChiTietDonHangs
                    .Where(ct => ct.MaDonHang == maDonHang)
                    .ToListAsync();
                foreach (var chiTiet in chiTietDonHangs)
                {
                    var sach = await db.Saches.FindAsync(chiTiet.MaSach);
                    if (sach != null)
                    {
                        sach.SoLuongTon += chiTiet.SoLuong ?? 0;
                    }
                }

                await db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã hủy đơn hàng thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction("Index", "Thongtin");
        }

        /// <summary>
        /// Xem chi tiết thanh toán
        /// </summary>
        public async Task<ActionResult> Details(string maDonHang)
        {
            if (Session["KhachHang"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập!";
                return RedirectToAction("Index", "Dangnhap");
            }

            var maKH = Session["MaKH"]?.ToString();
            // Tối ưu: AsNoTracking() cho read-only query
            var thanhToan = await db.ThanhToans
                .AsNoTracking()
                .Where(t => t.MaDonHang == maDonHang && t.DonDatHang.MaKH == maKH)
                .FirstOrDefaultAsync();

            if (thanhToan == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin thanh toán!";
                return RedirectToAction("Index", "Thongtin");
            }

            return View(thanhToan);
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