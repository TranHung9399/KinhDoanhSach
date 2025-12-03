using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity; // Added for Async support
using System.Threading.Tasks; // Added for Task

namespace banSach.Controllers
{
    public class HomeController : BaseController
    {
        public async Task<ActionResult> Index()
        {
            // Cache categories (simple caching for this request scope, or could be static/MemoryCache if needed globally)
            // For now, just AsNoTracking for performance
            var loaiList = await db.Loais.AsNoTracking().ToListAsync();
            ViewBag.LoaiList = loaiList;

            // REMOVED: var sachList = db.Saches.ToList(); - This was loading the entire table!

            // Sách mới: 8 cuốn mới nhất - Optimized with AsNoTracking
            ViewBag.NewBooks = await db.Saches.AsNoTracking()
                .Where(s => s.Status == 1)
                .OrderByDescending(s => s.NgayNhapHang)
                .Take(8)
                .ToListAsync();

            // Sách bán chạy: Optimized to fetch data efficiently
            // Note: GroupBy in EF6 can be tricky. We'll do the aggregation in DB.
            var bestSellerIds = await db.ChiTietDonHangs
                .AsNoTracking()
                .Where(ct => ct.Sach.Status == 1)
                .GroupBy(ct => ct.MaSach)
                .Select(g => new
                {
                    MaSach = g.Key,
                    TotalSold = g.Sum(x => x.SoLuong)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(8)
                .Select(x => x.MaSach)
                .ToListAsync();

            var bestSellerBooks = await db.Saches
                .AsNoTracking()
                .Where(s => bestSellerIds.Contains(s.MaSach))
                .ToListAsync();

            // Re-order in memory to match the Top Sold order
            ViewBag.BestSellers = bestSellerIds
                .Select(id => bestSellerBooks.FirstOrDefault(s => s.MaSach == id))
                .Where(s => s != null)
                .ToList();

            // Fix: The View expects a Model for "All Books" tab (Model.Take(8))
            // We fetch just 8 books to satisfy the view without loading the whole table.
            var allBooks = await db.Saches.AsNoTracking()
                .Where(s => s.Status == 1)
                .OrderBy(s => s.NgayNhapHang) // Or OrderBy(s => s.TenSach)
                .Take(8)
                .ToListAsync();

            return View(allBooks);
        }

        public ActionResult TimKiem(string skey)
        {
            // Truyền danh sách loại sách vào ViewBag để hiển thị danh mục (nếu cần)
            ViewBag.LoaiList = db.Loais.AsNoTracking().ToList();
            ViewBag.SearchKey = skey; // Lưu từ khóa tìm kiếm để hiển thị lại

            // Nếu skey rỗng hoặc null, trả về danh sách rỗng hoặc thông báo
            if (string.IsNullOrWhiteSpace(skey))
            {
                return View(new List<banSach.Models.Sach>()); // Trả về view TimKiem với danh sách rỗng
            }

            // Tìm kiếm sách theo tên hoặc mô tả với AsNoTracking() cho read-only query
            var searchResults = db.Saches
                .AsNoTracking()
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
            catch (Exception)
            {
                TempData["ContactError"] = "Có lỗi xảy ra khi gửi liên hệ. Vui lòng thử lại sau.";
                return RedirectToAction("LienHe");
            }
        }


    }
}