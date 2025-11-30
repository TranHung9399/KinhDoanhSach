using banSach.Models;
using banSach.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using PagedList;

namespace banSach.Controllers
{
    public class BookController : BaseController
    {
        // GET: Book
        public ActionResult Index(string id, string type, string sort, int? page, string[] publishers, decimal? minPrice, decimal? maxPrice)
        {
            int pageSize = 12;
            int pageNumber = page ?? 1;

            // 1. Base Query
            var books = db.Saches.Where(s => s.Status == 1);

            // 2. Prepare ViewBags for Filters
            ViewBag.SidebarCategories = db.Loais.Where(l => l.Status == 1).ToList();
            ViewBag.Publishers = db.NhaXuatBans.ToList();
            
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
                var category = db.Loais.Find(id);
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

            return View(books.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var book = db.Saches.Find(id);
            if (book == null || book.Status != 1)
            {
                return HttpNotFound();
            }

            // Fetch authors and their roles via VietSach
            var authors = (from vs in db.VietSaches
                           join tg in db.TacGias on vs.MaTG equals tg.MaTG
                           where vs.MaSach == id && tg.Status == 1
                           select new SachChonViewModel
                           {
                               MaSach = vs.MaSach,
                               TenSach = vs.Sach.TenSach,
                               VaiTro = vs.VaiTro,
                               TenTG = tg.TenTG,
                               TieuSu=tg.TieuSu,
                           }).ToList();

            // Log the results for debugging
            System.Diagnostics.Debug.WriteLine($"Book MaSach: {id}, Author Count: {authors.Count}");
            foreach (var author in authors)
            {
                System.Diagnostics.Debug.WriteLine($"Author: {author.TenTG}, Role: {author.VaiTro}");
            }

            ViewBag.Authors = authors;

            // Giả sử Model là 1 quyển sách
            var relatedBooks = db.Saches
                .Where(s => s.MaSach != book.MaSach)
                .OrderByDescending(s => s.NgayNhapHang)
                .Take(10)
                .ToList();
            ViewBag.RelatedBooks = relatedBooks;

            return View(book);
        }
    }
}