namespace banSach.Models
{

	using System.Collections.Generic;
	using banSach.Models;

	public class CheckoutViewModel
	{
		public string HoTen { get; set; }
		public string SoDienThoai { get; set; }
		public string Email { get; set; }
		public string QuocGia { get; set; }
		public string TinhThanhPho { get; set; }
		public string QuanHuyen { get; set; }
		public string PhuongXa { get; set; }
		public string DiaChiChiTiet { get; set; }
		public string MaGiamGia { get; set; }
		public string PhuongThucGiaoHang { get; set; }
		public string PhuongThucThanhToan { get; set; }
		public bool YeuCauHoaDonDienTu { get; set; }
		public decimal PhiVanChuyen { get; set; }
		public decimal TongTien { get; set; }
		public decimal GiamGia { get; set; }
		public List<ChiTietGioHang> CartItems { get; set; }
	}
}