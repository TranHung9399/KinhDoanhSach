using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace banSach.Models
{
    public class ThongKe
    {
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public string LoaiThongKe { get; set; } // "ngay" hoặc "thang"
        public int TongSoDonHang { get; set; }
        public decimal TongDoanhThu { get; set; }
        public decimal DoanhThuTrungBinh { get; set; }
        public int TongSoSanPhamBan { get; set; }
        public int TongKhachHang { get; set; }
        public List<ChiTietThongKe> ChiTiet { get; set; }
        public List<SanPhamBanChay> TopSanPhamBanChay { get; set; }
        public List<ThongKeTheoLoai> ThongKeTheoLoai { get; set; }
    }

    public class ChiTietThongKe
    {
        public string KhoangThoiGian { get; set; } // Định dạng: "dd/MM/yyyy" hoặc "MM/yyyy"
        public int SoDonHang { get; set; }
        public decimal DoanhThu { get; set; }
    }

    public class SanPhamBanChay
    {
        public string TenSach { get; set; }
        public int SoLuongBan { get; set; }
        public decimal DoanhThu { get; set; }
        public string AnhBia { get; set; }
    }

    public class ThongKeTheoLoai
    {
        public string TenLoai { get; set; }
        public int SoLuongBan { get; set; }
        public decimal DoanhThu { get; set; }
        public decimal TyLe { get; set; }
    }
}