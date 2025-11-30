using banSach.Models;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Data.Entity;

namespace banSach.Controllers
{
    public class BaseController : Controller
    {
        protected QLBanSachEntities db = new QLBanSachEntities();

		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			var maKH = Session["MaKH"]?.ToString();
			if (!string.IsNullOrEmpty(maKH))
			{
				var gioHang = db.GioHangs.Include("ChiTietGioHangs").FirstOrDefault(g => g.MaKH == maKH);
				ViewBag.Tongsoluong = gioHang?.ChiTietGioHangs.Sum(ct => ct.SoLuong ?? 0) ?? 0;
			}
			else
			{
				var cart = Session["Cart"] as List<ChiTietGioHang>;
				ViewBag.Tongsoluong = cart?.Sum(x => x.SoLuong ?? 0) ?? 0;
			}
			base.OnActionExecuting(filterContext); ;
		}

		private void TinhTongSoLuong()
        {
            var maKH = HttpContext.Session["MaKH"]?.ToString();
            if (!string.IsNullOrEmpty(maKH))
            {
                var gioHang = db.GioHangs.Include("ChiTietGioHangs")
                                         .FirstOrDefault(g => g.MaKH == maKH);

                if (gioHang != null && gioHang.ChiTietGioHangs != null)
                {
                    ViewBag.Tongsoluong = gioHang.ChiTietGioHangs.Sum(ct => ct.SoLuong ?? 0);
                }
                else
                {
                    ViewBag.Tongsoluong = 0;
                }
            }
            else
            {
                ViewBag.Tongsoluong = 0;
            }
        }
    }

}
