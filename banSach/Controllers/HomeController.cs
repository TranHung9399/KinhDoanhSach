using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace banSach.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            var loaiList = db.Loais.ToList(); // Lấy tất cả các loại sách
            ViewBag.LoaiList = loaiList;
            var sachList = db.Saches.ToList(); // Lấy tất cả sách
            // Sách mới: 8 cuốn mới nhất
            ViewBag.NewBooks = db.Saches.Where(s => s.Status == 1).OrderByDescending(s => s.NgayNhapHang).Take(8).ToList();
            // Sách bán chạy: 8 cuốn có tổng số lượng bán nhiều nhất
            var bestSellerIds = db.ChiTietDonHangs
                .Where(ct => ct.Sach.Status == 1)
                .GroupBy(ct => ct.MaSach)
                .Select(g => new {
                    MaSach = g.Key,
                    TotalSold = g.Sum(x => x.SoLuong)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(8)
                .ToList()
                .Select(x => x.MaSach)
                .ToList();

            var bestSellerBooks = db.Saches
                .Where(s => bestSellerIds.Contains(s.MaSach))
                .ToList();

            // Đảm bảo thứ tự đúng theo bestSellerIds
            ViewBag.BestSellers = bestSellerIds
                .Select(id => bestSellerBooks.FirstOrDefault(s => s.MaSach == id))
                .Where(s => s != null)
                .ToList();
            return View(sachList);
        }

        public ActionResult TimKiem(string skey)
        {
            // Truyền danh sách loại sách vào ViewBag để hiển thị danh mục (nếu cần)
            ViewBag.LoaiList = db.Loais.ToList();
            ViewBag.SearchKey = skey; // Lưu từ khóa tìm kiếm để hiển thị lại

            // Nếu skey rỗng hoặc null, trả về danh sách rỗng hoặc thông báo
            if (string.IsNullOrWhiteSpace(skey))
            {
                return View(new List<banSach.Models.Sach>()); // Trả về view TimKiem với danh sách rỗng
            }

            // Tìm kiếm sách theo tên hoặc mô tả
            var searchResults = db.Saches
                .Where(s => s.TenSach.Contains(skey) || (s.MoTa != null && s.MoTa.Contains(skey)))
                .ToList();

            // Trả về view TimKiem với danh sách sách tìm được
            return View(searchResults);
        }

        public ActionResult GioiThieu()
        {
            return View();
        }
        public ActionResult ChinhSachBanHang()
        {
            return View();
        }
        public ActionResult ChinhSachBaoMat()
        {
            return View();
        }

        public ActionResult LienHe()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SendContact(string name, string email, string message)
        {
            try
            {
                // Xử lý gửi email hoặc lưu vào database
                // Ở đây có thể thêm logic gửi email thông báo
                
                TempData["ContactMessage"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất có thể.";
                return RedirectToAction("LienHe");
            }
            catch (Exception ex)
            {
                TempData["ContactError"] = "Có lỗi xảy ra khi gửi liên hệ. Vui lòng thử lại sau.";
                return RedirectToAction("LienHe");
            }
        }


    }
}