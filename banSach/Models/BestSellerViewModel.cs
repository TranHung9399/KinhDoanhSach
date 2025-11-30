using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace banSach.Models
{
    public class BestSellerViewModel
    {
        public string TenSach { get; set; }
        public string Hinh { get; set; }
        public decimal? GiaBan { get; set; }
        public int? SoLuongBan { get; set; }
    }
}
