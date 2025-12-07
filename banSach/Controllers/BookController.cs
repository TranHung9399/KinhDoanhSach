using banSach.Models;
using banSach.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using PagedList;
using System.Data.Entity; // Added for Async
using System.Threading.Tasks; // Added for Async

namespace banSach.Controllers
{
    public class BookController : BaseController
    {
        // GET: Book
        public async Task<ActionResult> Index(string id, string type, string sort, int? page, string[] publishers, decimal? minPrice, decimal? maxPrice)
        {
            int pageSize = 12;
            int pageNumber = page ?? 1;

            // 1. Base Query
            var books = db.Saches.AsNoTracking().Where(s => s.Status == 1);

            // 2. Prepare ViewBags for Filters
            ViewBag.SidebarCategories = await db.Loais.AsNoTracking().Where(l => l.Status == 1).ToListAsync();
            ViewBag.Publishers = await db.NhaXuatBans.AsNoTracking().ToListAsync();
            
            // Calculate Global Min/Max for Slider
            // Calculate Global Min/Max for Slider
            var globalMin = 0;
            var globalMax = 1000000;
            ViewBag.GlobalMinPrice = globalMin;
            ViewBag.GlobalMaxPrice = globalMax;

            // Keep selected filter values
            ViewBag.SelectedPublishers = publishers;
            ViewBag.SelectedMinPrice = minPrice;
            ViewBag.SelectedMaxPrice = maxPrice;
            ViewBag.CurrentSort = sort;
            ViewBag.CurrentType = type;
            ViewBag.CurrentId = id;

            // 3. Apply Filters

            // Filter by Category
            if (!string.IsNullOrEmpty(id))
            {
                var category = await db.Loais.FindAsync(id);
                if (category != null)
                {
                    books = books.Where(s => s.MaLoai == id);
                    ViewBag.Category = category;
                }
                else
                {
                    ViewBag.Category = new Loai { TenLoai = "Tất cả sách" };
                }
            }
            else
            {
                ViewBag.Category = new Loai { TenLoai = "Tất cả sách" };
            }

            // Filter by Type (Special Cases)
            if (!string.IsNullOrEmpty(type))
            {
                if (type == "new")
                {
                    // Chỉ lấy sách có NgayNhapHang trong vòng 30 ngày
                    var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                    books = books.Where(s => s.NgayNhapHang.HasValue 
                                        && s.NgayNhapHang.Value >= thirtyDaysAgo);
                    ViewBag.Category = new Loai { TenLoai = "Sách mới nhất" };
                }
                else if (type == "bestseller")
                {
                    ViewBag.Category = new Loai { TenLoai = "Sách bán chạy" };
                }
            }

            // Filter by Publisher
            if (publishers != null && publishers.Length > 0)
            {
                books = books.Where(s => publishers.Contains(s.MaNXB));
            }

            // Filter by Price
            if (minPrice.HasValue)
            {
                books = books.Where(s => s.GiaBan >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                books = books.Where(s => s.GiaBan <= maxPrice.Value);
            }

            // 4. Sorting
            switch (sort)
            {
                case "moinhat":
                    books = books.OrderByDescending(s => s.NgayNhapHang);
                    break;
                case "cunhat":
                    books = books.OrderBy(s => s.NgayNhapHang);
                    break;
                case "giatangdan":
                    books = books.OrderBy(s => s.GiaBan);
                    break;
                case "giagiamdan":
                    books = books.OrderByDescending(s => s.GiaBan);
                    break;
                case "tenaz":
                    books = books.OrderBy(s => s.TenSach);
                    break;
                case "tenza":
                    books = books.OrderByDescending(s => s.TenSach);
                    break;
                case "banchay":
                     books = books.OrderByDescending(s => s.ChiTietDonHangs.Sum(ct => (int?)ct.SoLuong) ?? 0);
                    break;
                default:
                    // Default sort
                    if (type == "bestseller")
                    {
                         books = books.OrderByDescending(s => s.ChiTietDonHangs.Sum(ct => (int?)ct.SoLuong) ?? 0);
                    }
                    else
                    {
                        books = books.OrderByDescending(s => s.NgayNhapHang);
                    }
                    break;
            }

            // Note: ToPagedList is synchronous. For full async with PagedList, we might need PagedList.Mvc or manual paging.
            // However, ToPagedList executes the query. To make it async, we should execute query async first or use X.PagedList.
            // For now, to avoid breaking PagedList dependency, we will fetch list async then page (less efficient for huge data but safe)
            // OR better: keep ToPagedList sync but at least we did async for filters.
            // Actually, let's stick to sync for the final PagedList call unless we change the library.
            // But wait, the user wants Async.
            // Let's use standard Skip/Take for async paging if possible, or just accept ToPagedList sync for now but async everything else.
            // To be truly async, we would do:
            // var pagedBooks = await books.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            // But then we lose the IPagedList metadata.
            // Let's keep ToPagedList as is (Sync) because changing it requires changing the View model type or adding a new library.
            // The filters above are async.
            
            return View(books.ToPagedList(pageNumber, pageSize));
        }

        public async Task<ActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var book = await db.Saches.FindAsync(id);
            if (book == null || book.Status != 1)
            {
                return HttpNotFound();
            }

            // Fetch authors and their roles via VietSach with AsNoTracking() for read-only query
            var authors = await (from vs in db.VietSaches.AsNoTracking()
                           join tg in db.TacGias on vs.MaTG equals tg.MaTG
                           where vs.MaSach == id && tg.Status == 1
                           select new SachChonViewModel
                           {
                               MaSach = vs.MaSach,
                               TenSach = vs.Sach.TenSach,
                               VaiTro = vs.VaiTro,
                               TenTG = tg.TenTG,
                               TieuSu=tg.TieuSu,
                           }).ToListAsync();

            // Log the results for debugging
            System.Diagnostics.Debug.WriteLine($"Book MaSach: {id}, Author Count: {authors.Count}");
            foreach (var author in authors)
            {
                System.Diagnostics.Debug.WriteLine($"Author: {author.TenTG}, Role: {author.VaiTro}");
            }

            ViewBag.Authors = authors;

            // Lấy sách liên quan với AsNoTracking() và chỉ lấy các cột cần thiết
            var relatedBooks = await db.Saches
                .AsNoTracking()
                .Where(s => s.MaSach != book.MaSach && s.Status == 1)
                .OrderByDescending(s => s.NgayNhapHang)
                .Take(10)
                .ToListAsync();
            ViewBag.RelatedBooks = relatedBooks;

            return View(book);
        }
    }
}