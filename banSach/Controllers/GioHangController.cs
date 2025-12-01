using banSach.Models;
using banSach.Helper;
using System;
using System.Linq;
using System.Web.Mvc;
using banSach.Other;
using System.Configuration;
using System.Collections.Generic;
using System.Data.Entity;
using System.Threading.Tasks;
using banSach.Services;

namespace banSach.Controllers
{
	public class GioHangController : BaseController
	{

		// GET: GioHang
		public async Task<ActionResult> Index()
		{
			var maKH = Session["MaKH"]?.ToString();
			GioHang gioHang;

			if (!string.IsNullOrEmpty(maKH))
			{
				// Đã đăng nhập: lấy giỏ hàng từ DB
				gioHang = await db.GioHangs.Include("ChiTietGioHangs.Sach").FirstOrDefaultAsync(g => g.MaKH == maKH);
				if (gioHang == null)
				{
					gioHang = new GioHang
					{
						MaGioHang = Guid.NewGuid().ToString(),
						MaKH = maKH,
						NgayTao = DateTime.Now,
						ChiTietGioHangs = new List<ChiTietGioHang>()
					};
				}
			}
			else
			{
				// Chưa đăng nhập: lấy giỏ hàng từ session
				var cart = Session["Cart"] as List<ChiTietGioHang>;
				if (cart != null)
				{
                    // Note: Since this is session data, we might not want to query DB for every item if we can avoid it.
                    // But to be safe and consistent with original logic:
                    // We can't easily async foreach.
                    // Let's collect IDs and fetch all at once async.
                    var bookIds = cart.Select(c => c.MaSach).ToList();
                    var books = await db.Saches.AsNoTracking().Where(s => bookIds.Contains(s.MaSach)).ToListAsync();

					foreach (var ct in cart)
					{
						ct.Sach = books.FirstOrDefault(s => s.MaSach == ct.MaSach);
					}
				}
				var gioHangSession = new GioHang
				{
					MaGioHang = null,
					MaKH = null,
					NgayTao = DateTime.Now,
					ChiTietGioHangs = cart ?? new List<ChiTietGioHang>()
				};
				return View(gioHangSession);
			}
			ViewBag.IsLoggedIn = !string.IsNullOrEmpty(Session["MaKH"]?.ToString());
			return View(gioHang);
		}

		// GET: GioHang/ThemGiohang
		// GET: GioHang/ThemGiohang
		[HttpPost]
		public async Task<JsonResult> ThemGiohangAjax(string iMasach, int qty = 1)
		{
			var maKH = Session["MaKH"]?.ToString();
			if (string.IsNullOrEmpty(maKH))
			{
				// Chưa đăng nhập: thêm vào session Cart
				var sach = await db.Saches.FindAsync(iMasach);
				if (sach == null || sach.Status != 1 || sach.SoLuongTon <= 0)
					return Json(new { success = false, message = "Sách không tồn tại hoặc đã hết hàng." });

				List<ChiTietGioHang> cart;
				if (Session["Cart"] == null)
					cart = new List<ChiTietGioHang>();
				else
					cart = (List<ChiTietGioHang>)Session["Cart"];

				var chiTiet = cart.FirstOrDefault(ct => ct.MaSach == iMasach);
				if (chiTiet == null)
				{
					cart.Add(new ChiTietGioHang
					{
						MaSach = iMasach,
						SoLuong = qty,
						DonGia = sach.GiaChietKhau
					});
				}
				else
				{
					chiTiet.SoLuong = (chiTiet.SoLuong ?? 0) + qty;
				}
				Session["Cart"] = cart;
				var tongSoLuong = cart.Sum(ct => ct.SoLuong ?? 0);
				return Json(new { success = true, message = "Đã thêm sách vào giỏ hàng!", tongsoluong = tongSoLuong });
			}
			else
			{
				// Đã đăng nhập: thêm vào DB
				var sach = await db.Saches.FindAsync(iMasach);
				if (sach == null || sach.Status != 1 || sach.SoLuongTon <= 0)
					return Json(new { success = false, message = "Sách không tồn tại hoặc đã hết hàng." });

				var gioHang = await db.GioHangs.FirstOrDefaultAsync(g => g.MaKH == maKH);
				if (gioHang == null)
				{
					gioHang = new GioHang
					{
						MaGioHang = Guid.NewGuid().ToString(),
						MaKH = maKH,
						NgayTao = DateTime.Now
					};
					db.GioHangs.Add(gioHang);
					await db.SaveChangesAsync();
				}

				var chiTiet = await db.ChiTietGioHangs.FirstOrDefaultAsync(ct => ct.MaGioHang == gioHang.MaGioHang && ct.MaSach == iMasach);
				if (chiTiet == null)
				{
					chiTiet = new ChiTietGioHang
					{
						MaChiTiet = Guid.NewGuid().ToString(),
						MaGioHang = gioHang.MaGioHang,
						MaSach = iMasach,
						SoLuong = qty,
						DonGia = sach.GiaChietKhau
					};
					db.ChiTietGioHangs.Add(chiTiet);
				}
				else
				{
					chiTiet.SoLuong = (chiTiet.SoLuong ?? 0) + qty;
				}
				await db.SaveChangesAsync();

				var tongSoLuong = await db.ChiTietGioHangs.Where(ct => ct.MaGioHang == gioHang.MaGioHang).SumAsync(ct => ct.SoLuong) ?? 0;
				return Json(new { success = true, message = "Đã thêm sách vào giỏ hàng!", tongsoluong = tongSoLuong });
			}

		}

		[HttpPost]
		public async Task<ActionResult> UpdateQuantity(string maGioHang, string maSach, int soLuong)
		{
			try
			{
				var chiTiet = await db.ChiTietGioHangs
					.FirstOrDefaultAsync(ct => ct.MaGioHang == maGioHang && ct.MaSach == maSach);

				if (chiTiet == null)
					return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng." });

				var sach = await db.Saches.FindAsync(maSach);
				if (sach == null)
					return Json(new { success = false, message = "Không tìm thấy sách." });

				if (soLuong <= 0)
				{
					db.ChiTietGioHangs.Remove(chiTiet);
				}
				else
				{
					if (soLuong > sach.SoLuongTon)
						return Json(new { success = false, message = "Số lượng vượt quá tồn kho." });

					chiTiet.SoLuong = soLuong;
					db.Entry<ChiTietGioHang>(chiTiet).State = System.Data.Entity.EntityState.Modified;
				}

				await db.SaveChangesAsync();

				decimal newSubtotal = (soLuong <= 0) ? 0 : soLuong * (chiTiet.DonGia ?? 0);
				decimal newTotal = await db.ChiTietGioHangs
					.Where(ct => ct.MaGioHang == maGioHang)
					.SumAsync(ct => (ct.SoLuong ?? 0) * (ct.DonGia ?? 0));

				return Json(new
				{
					success = true,
					subtotal = newSubtotal,
					total = newTotal,
					formattedSubtotal = String.Format("{0:N0}", newSubtotal),
					formattedTotal = String.Format("{0:N0}", newTotal)
				});
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Lỗi: " + ex.Message });
			}
		}
		[HttpPost]
		public JsonResult UpdateQuantitySession(string maSach, int soLuong)
		{
			var cart = Session["Cart"] as List<ChiTietGioHang>;
			if (cart == null)
				return Json(new { success = false, message = "Giỏ hàng trống." });

			var item = cart.FirstOrDefault(ct => ct.MaSach == maSach);
			if (item == null)
				return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

			if (soLuong <= 0)
			{
				cart.Remove(item);
			}
			else
			{
				item.SoLuong = soLuong;
			}
			Session["Cart"] = cart;

			decimal newSubtotal = soLuong <= 0 ? 0 : soLuong * (item.DonGia ?? 0);
			decimal newTotal = cart.Sum(ct => (ct.SoLuong ?? 0) * (ct.DonGia ?? 0));
			return Json(new
			{
				success = true,
				subtotal = newSubtotal,
				total = newTotal,
				formattedSubtotal = String.Format("{0:N0}", newSubtotal),
				formattedTotal = String.Format("{0:N0}", newTotal)
			});
		}

		// GET: GioHang/RemoveItem
		public async Task<ActionResult> RemoveItem(string maGioHang, string maSach)
		{
			var chiTiet = await db.ChiTietGioHangs.FirstOrDefaultAsync(ct => ct.MaGioHang == maGioHang && ct.MaSach == maSach);
			if (chiTiet != null)
			{
				db.ChiTietGioHangs.Remove(chiTiet);
				await db.SaveChangesAsync();
			}

			return RedirectToAction("Index");
		}

		private void TinhTongSoLuong()
		{
			var maKH = Session["MaKH"]?.ToString();
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
		[HttpPost]
		public JsonResult RemoveItemSession(string maSach)
		{
			var cart = Session["Cart"] as List<ChiTietGioHang>;
			if (cart != null)
			{
				var item = cart.FirstOrDefault(ct => ct.MaSach == maSach);
				if (item != null)
				{
					cart.Remove(item);
					Session["Cart"] = cart;

					decimal newTotal = cart.Sum(ct => (ct.SoLuong ?? 0) * (ct.DonGia ?? 0));
					return Json(new
					{
						success = true,
						formattedTotal = String.Format("{0:N0}", newTotal)
					});
				}
			}
			return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
		}

		[HttpGet]
		public JsonResult GetAvailableDiscounts()
		{
			try
			{
				var today = DateTime.Now;

				var discountsFromDb = db.MaGiamGias
					.Where(m =>
						(m.TrangThai ?? true) &&
						(!m.NgayBatDau.HasValue || DbFunctions.TruncateTime(m.NgayBatDau) <= today) &&
						(!m.NgayKetThuc.HasValue || DbFunctions.TruncateTime(m.NgayKetThuc) >= today))
					.ToList();

				var discounts = discountsFromDb
					.Select(m =>
					{
						var used = m.DaSuDung ?? 0;
						var remaining = m.SoLuongMa.HasValue ? Math.Max(0, m.SoLuongMa.Value - used) : (int?)null;
						var type = ResolveDiscountType(m);

						return new
						{
							m.MaCode,
							m.TenChuongTrinh,
							m.PhanTramGiam,
							m.SoTienGiam,
							LoaiMa = type,
							SoLuongConLai = remaining,
							NgayKetThuc = m.NgayKetThuc.HasValue
								? m.NgayKetThuc.Value.ToString("dd/MM/yyyy")
								: "",
							DonHangToiThieu = m.DonHangToiThieu,
							GiamToiDa = m.GiamToiDa
						};
					})
					.Where(x => x.SoLuongConLai == null || x.SoLuongConLai > 0)
					.ToList();

				return Json(new { success = true, data = discounts }, JsonRequestBehavior.AllowGet);
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
			}
		}

		[HttpPost]
		public JsonResult ValidateDiscountCode(string code)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(code))
				{
					return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });
				}

				var normalizedCode = code.Trim();
				var discount = db.MaGiamGias.FirstOrDefault(m => m.MaCode == normalizedCode);

				if (discount == null)
				{
					return Json(new { success = false, message = "Mã giảm giá không tồn tại." });
				}

				var now = DateTime.Now;
				if (!(discount.TrangThai ?? true))
				{
					return Json(new { success = false, message = "Mã giảm giá đã bị vô hiệu hóa." });
				}

				if (discount.NgayBatDau.HasValue && discount.NgayBatDau.Value > now)
				{
					return Json(new { success = false, message = "Mã giảm giá chưa tới thời gian áp dụng." });
				}

				if (discount.NgayKetThuc.HasValue && discount.NgayKetThuc.Value < now)
				{
					return Json(new { success = false, message = "Mã giảm giá đã hết hạn." });
				}

				var used = discount.DaSuDung ?? 0;
				if (discount.SoLuongMa.HasValue && used >= discount.SoLuongMa.Value)
				{
					return Json(new { success = false, message = "Mã giảm giá đã được sử dụng hết." });
				}

				var discountType = ResolveDiscountType(discount);
				var discountValue = discountType == "percent"
					? (decimal)(discount.PhanTramGiam ?? 0)
					: discountType == "fixed"
						? (discount.SoTienGiam ?? 0)
						: 0;

				var payload = new
				{
					type = discountType,
					value = discountValue,
					maxDiscount = discount.GiamToiDa ?? 0,
					minOrderAmount = discount.DonHangToiThieu ?? 0,
					expiresAt = discount.NgayKetThuc?.ToString("dd/MM/yyyy")
				};

				return Json(new { success = true, discount = payload });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Không thể xác thực mã giảm giá: " + ex.Message });
			}
		}

		private static string ResolveDiscountType(MaGiamGia discount)
		{
			if (discount == null)
			{
				return "fixed";
			}

			// ✅ KIỂM TRA TRƯỚC: Nếu có LoaiMa được set rõ ràng, dùng nó
			if (!string.IsNullOrWhiteSpace(discount.LoaiMa))
			{
				var loaiMa = discount.LoaiMa.ToLower().Trim();
				if (loaiMa == "percent" || loaiMa == "fixed" || loaiMa == "freeship")
				{
					return loaiMa;
				}
			}

			// Fallback: Suy luận từ các field khác (logic cũ)
			if (discount.PhanTramGiam.HasValue && discount.PhanTramGiam.Value > 0)
			{
				return "percent";
			}

			if (discount.SoTienGiam.HasValue && discount.SoTienGiam.Value > 0)
			{
				return "fixed";
			}

			return "freeship";
		}

		public ActionResult Checkout()
		{
			var maKH = Session["MaKH"]?.ToString();
			var model = new CheckoutViewModel();

			if (string.IsNullOrEmpty(maKH))
			{
				// Chưa đăng nhập: lấy giỏ hàng từ session
				var cart = Session["Cart"] as List<ChiTietGioHang>;
				if (cart == null || !cart.Any())
				{
					TempData["Error"] = "Giỏ hàng của bạn đang trống.";
					return RedirectToAction("Index");
				}
				model.CartItems = cart;
				// KHÔNG tự động đổ thông tin người dùng vào form
			}
			else
			{
				// Đã đăng nhập: lấy giỏ hàng từ DB
				var gioHang = db.GioHangs.Include("ChiTietGioHangs.Sach").FirstOrDefault(g => g.MaKH == maKH);

				if (gioHang == null || !gioHang.ChiTietGioHangs.Any())
				{
					TempData["Error"] = "Giỏ hàng của bạn đang trống.";
					return RedirectToAction("Index");
				}

				// KHÔNG tự động điền thông tin KH vào model
				model.CartItems = gioHang.ChiTietGioHangs.ToList();
			}

			return View("Checkout", model);
		}

		[HttpPost]
		public async Task<ActionResult> DatHang(string HoTen, string SoDienThoai, string Email, string QuocGia, string TinhThanhPho,
			string QuanHuyen, string PhuongXa, string DiaChiChiTiet, string PhuongThucGiaoHang, string PhuongThucThanhToan,
			bool YeuCauHoaDonDienTu = false, decimal PhiVanChuyen = 0, string MaGiamGia = "", decimal GiamGia = 0, decimal TongTien = 0)
		{
			List<ChiTietGioHang> cart;
			var maKH = Session["MaKH"]?.ToString();

			if (string.IsNullOrEmpty(maKH))
			{
				// Khách lẻ: lấy giỏ hàng từ session
				cart = Session["Cart"] as List<ChiTietGioHang>;
			}
			else
			{
				// Đã đăng nhập: lấy giỏ hàng từ database
				var gioHang = db.GioHangs.Include("ChiTietGioHangs").FirstOrDefault(g => g.MaKH == maKH);
				cart = gioHang?.ChiTietGioHangs.ToList();
			}

			if (cart == null || !cart.Any())
			{
				TempData["Error"] = "Giỏ hàng của bạn đang trống.";
				return RedirectToAction("Index");
			}

			// Tính tổng tiền
			decimal tongTienHang = cart.Sum(item => (item.SoLuong ?? 0) * (item.DonGia ?? 0));
			decimal tongTienThanhToan = tongTienHang + PhiVanChuyen - GiamGia;

			// Tạo địa chỉ đầy đủ
			string diaChiDayDu = $"{DiaChiChiTiet}, {PhuongXa}, {QuanHuyen}, {TinhThanhPho}, {QuocGia}";

			// Lưu thông tin đơn hàng vào session
			var orderDetails = new Dictionary<string, object>
			{
				{ "HoTen", HoTen },
				{ "SoDienThoai", SoDienThoai },
				{ "Email", Email },
				{ "DiaChi", diaChiDayDu },
				{ "TotalAmount", tongTienThanhToan },
				{ "PhiVanChuyen", PhiVanChuyen },
				{ "MaGiamGia", MaGiamGia },
				{ "GiamGia", GiamGia },
				{ "YeuCauHoaDonDienTu", YeuCauHoaDonDienTu },
				{ "PhuongThucThanhToan", PhuongThucThanhToan }
			};

			var cartItems = cart.Select(item => new Dictionary<string, object>
			{
				{ "MaSach", item.MaSach },
				{ "SoLuong", item.SoLuong },
				{ "DonGia", item.DonGia }
			}).ToList();

			orderDetails["CartItems"] = cartItems;
			Session["PendingOrder"] = orderDetails;

			// Xử lý theo phương thức thanh toán
			if (PhuongThucThanhToan == "2") // VNPay
			{
				var vnPayTxnRef = GenerateVnPayTxnRef();
				orderDetails["VnPayTxnRef"] = vnPayTxnRef;

				var vnPayService = new VnPayService();
				var vnPayRequest = new VnPayPaymentRequest
				{
					OrderId = vnPayTxnRef,
					Amount = tongTienThanhToan,
					OrderInfo = $"Thanh toán đơn hàng cho KH {HoTen}",
					IpAddress = Util.GetIpAddress(),
					OrderType = "other",
					Locale = "vn"
				};

				var paymentUrl = vnPayService.CreatePaymentUrl(vnPayRequest);
				return Redirect(paymentUrl);
			}
			else if (PhuongThucThanhToan == "3") // MoMo
			{
				string endpoint = ConfigurationManager.AppSettings["MomoEndpoint"];
				string partnerCode = ConfigurationManager.AppSettings["MomoPartnerCode"];
				string accessKey = ConfigurationManager.AppSettings["MomoAccessKey"];
				string secretKey = ConfigurationManager.AppSettings["MomoSecretKey"];
				string returnUrl = ConfigurationManager.AppSettings["MomoReturnUrl"];
				string notifyUrl = ConfigurationManager.AppSettings["MomoNotifyUrl"];

				string requestId = DateTime.Now.Ticks.ToString();
				string orderId = DateTime.Now.Ticks.ToString();
				string orderInfo = $"Thanh toán đơn hàng cho {HoTen}";
				long amount = (long)tongTienThanhToan;

				var momoLib = new MomoLib
				{
					PartnerCode = partnerCode,
					AccessKey = accessKey,
					SecretKey = secretKey,
					RequestId = requestId,
					OrderId = orderId,
					OrderInfo = orderInfo,
					ReturnUrl = returnUrl,
					NotifyUrl = notifyUrl,
					Amount = amount
				};

				var momoResponse = await momoLib.CreatePaymentAsync(endpoint);

				if (momoResponse.ResultCode == 0 && !string.IsNullOrEmpty(momoResponse.PayUrl))
				{
					return Redirect(momoResponse.PayUrl);
				}
				else
				{
					TempData["Error"] = "Lỗi kết nối đến MoMo: " + momoResponse.Message;
					return RedirectToAction("Checkout");
				}
			}
			else // Thanh toán tiền mặt (COD)
			{
				return await TaoDonHangCOD(orderDetails, cart, diaChiDayDu, tongTienHang, PhiVanChuyen, GiamGia, MaGiamGia);
			}
		}

		private string GenerateVnPayTxnRef()
		{
			return DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
		}

		private async Task<ActionResult> TaoDonHangCOD(Dictionary<string, object> orderDetails, List<ChiTietGioHang> cart,
			string diaChiDayDu, decimal tongTienHang, decimal phiVanChuyen, decimal giamGia, string maGiamGia)
		{
			var maKH = Session["MaKH"]?.ToString(); // ✅ Lấy MaKH từ session

			var donHang = new DonDatHang
			{
				MaDonHang = Guid.NewGuid().ToString(),
				MaKH = maKH, // ✅ THÊM: Gán MaKH
				HoTen = orderDetails["HoTen"]?.ToString(),
				SoDienThoai = orderDetails["SoDienThoai"]?.ToString(),
				Email = orderDetails["Email"]?.ToString(),
				DiaChi = diaChiDayDu,
				NgayDat = DateTime.Now,
				TrangThai = "Chờ xác nhận",
				PhuongThucThanhToan = 1, // COD
				PhiVanChuyen = phiVanChuyen, // ✅ THÊM
				MaGiamGia = string.IsNullOrEmpty(maGiamGia) ? null : maGiamGia, // ✅ THÊM
				TongTien = tongTienHang + phiVanChuyen - giamGia // ✅ THÊM
			};
			db.DonDatHangs.Add(donHang);

			string emailBody = await TaoEmailBody(donHang, cart, tongTienHang, phiVanChuyen, giamGia, maGiamGia);

            // Batch fetch books to update stock
            var bookIds = cart.Select(c => c.MaSach).ToList();
            var booksToUpdate = await db.Saches.Where(s => bookIds.Contains(s.MaSach)).ToListAsync();

			foreach (var item in cart)
			{
                var sach = booksToUpdate.FirstOrDefault(s => s.MaSach == item.MaSach);
				if (sach != null)
					sach.SoLuongTon -= item.SoLuong ?? 0;

				db.ChiTietDonHangs.Add(new ChiTietDonHang
				{
					MaDonHang = donHang.MaDonHang,
					MaSach = item.MaSach,
					SoLuong = item.SoLuong,
					DonGia = item.DonGia
				});
			}

			// ✅ THÊM: Tạo bản ghi thanh toán
			var thanhToan = new ThanhToan
			{
				MaThanhToan = "TT" + DateTime.Now.ToString("yyyyMMddHHmmss") + donHang.MaDonHang.Substring(Math.Max(0, donHang.MaDonHang.Length - 4)),
				MaDonHang = donHang.MaDonHang,
				PhuongThucThanhToan = 1, // COD
				TrangThaiThanhToan = "Chờ thanh toán",
				SoTienThanhToan = donHang.TongTien ?? 0,
				NgayTao = DateTime.Now
			};
			db.ThanhToans.Add(thanhToan);

			// ✅ THÊM: Lưu vào bảng SuDungMaGiamGia (nếu có dùng mã giảm giá)
			if (!string.IsNullOrEmpty(maGiamGia) && giamGia > 0)
			{
				var suDung = new SuDungMaGiamGia
				{
					MaCode = maGiamGia,
					MaDonHang = donHang.MaDonHang,
					MaKH = maKH,
					SoTienGiam = giamGia,
					NgaySuDung = DateTime.Now
				};
				db.SuDungMaGiamGias.Add(suDung);

				// Cập nhật số lần đã sử dụng mã giảm giá
				var maGiamGiaObj = await db.MaGiamGias.FirstOrDefaultAsync(m => m.MaCode == maGiamGia);
				if (maGiamGiaObj != null)
				{
					maGiamGiaObj.DaSuDung = (maGiamGiaObj.DaSuDung ?? 0) + 1;
				}
			}

			await db.SaveChangesAsync();

			// Gửi email xác nhận
			SendMail sendMail = new SendMail();
			sendMail.SendMailFunction(donHang.Email, "Xác nhận đơn hàng từ Cửa hàng sách BOOKSTORE", emailBody);

			// Xóa giỏ hàng
			await XoaGioHang();

			TempData["Success"] = "Đặt hàng thành công!";
			return RedirectToAction("Index", "Home");
		}

		public async Task<ActionResult> PaymentConfirm()
		{
			var vnPayService = new VnPayService();
			var response = vnPayService.ParsePaymentResponse(Request.QueryString);

			if (Request.QueryString.Count == 0)
			{
				ViewBag.Message = "Không có thông tin thanh toán.";
				TempData["Error"] = ViewBag.Message;
				return View();
			}

			if (!response.IsValidSignature)
			{
				ViewBag.Message = string.IsNullOrEmpty(response.Message)
					? "Chữ ký không hợp lệ."
					: response.Message;
				TempData["Error"] = ViewBag.Message;
				return View();
			}

			var pendingOrder = Session["PendingOrder"] as Dictionary<string, object>;
			var expectedTxnRef = pendingOrder != null && pendingOrder.ContainsKey("VnPayTxnRef")
				? pendingOrder["VnPayTxnRef"]?.ToString()
				: null;

			if (pendingOrder == null || string.IsNullOrEmpty(expectedTxnRef))
			{
				ViewBag.Message = "Không tìm thấy thông tin đơn hàng trong phiên.";
				TempData["Error"] = ViewBag.Message;
				return View();
			}

			if (!string.Equals(expectedTxnRef, response.OrderId, StringComparison.Ordinal))
			{
				ViewBag.Message = "Mã giao dịch không trùng khớp.";
				TempData["Error"] = ViewBag.Message;
				return View();
			}

			if (response.IsSuccess)
			{
				await XuLyThanhToanThanhCong(response.OrderId, response.TransactionNo, 2);
				ViewBag.Message = $"Thanh toán thành công! Mã giao dịch: {response.TransactionNo}";
				TempData["Success"] = "Đặt hàng thành công!";
			}
			else
			{
				ViewBag.Message = $"Giao dịch bị từ chối. Mã lỗi: {response.ResponseCode}";
				TempData["Error"] = ViewBag.Message;
			}

			return View();
		}

		public async Task<ActionResult> MomoPaymentConfirm()
		{
			if (Request.QueryString.Count > 0)
			{
				string secretKey = ConfigurationManager.AppSettings["MomoSecretKey"];
				
				// Lấy dữ liệu từ MoMo callback
				var momoData = new Dictionary<string, string>();
				foreach (string key in Request.QueryString.AllKeys)
				{
					if (!string.IsNullOrEmpty(key))
					{
						momoData[key] = Request.QueryString[key];
					}
				}

				// Xác thực chữ ký
				var momoLib = new MomoLib();
				bool isValidSignature = momoLib.ValidateCallback(momoData, secretKey);

				if (!isValidSignature)
				{
					ViewBag.Message = "Chữ ký không hợp lệ. Giao dịch có thể bị giả mạo.";
					TempData["Error"] = ViewBag.Message;
					return View("PaymentConfirm");
				}

				// Kiểm tra kết quả thanh toán
				int resultCode = 0;
				if (momoData.ContainsKey("resultCode"))
				{
					int.TryParse(momoData["resultCode"], out resultCode);
				}

				if (resultCode == 0)
				{
					// Thanh toán thành công
					string orderId = momoData.GetValueOrDefault("orderId", "");
					string transId = momoData.GetValueOrDefault("transId", "");
					string amount = momoData.GetValueOrDefault("amount", "0");

					// Xử lý đơn hàng
					await XuLyThanhToanThanhCong(orderId, transId, 3); // 3 = MoMo

					ViewBag.Message = $"Thanh toán MoMo thành công! Mã đơn hàng: {orderId} | Mã giao dịch: {transId} | Số tiền: {long.Parse(amount):N0}đ";
					TempData["Success"] = "Đặt hàng thành công!";
				}
				else
				{
					// Thanh toán thất bại
					string message = momoData.GetValueOrDefault("message", "Thanh toán thất bại");
					ViewBag.Message = $"Thanh toán MoMo thất bại: {message} (Mã lỗi: {resultCode})";
					TempData["Error"] = ViewBag.Message;
				}
			}
			else
			{
				ViewBag.Message = "Không có thông tin thanh toán từ MoMo.";
				TempData["Error"] = ViewBag.Message;
			}

			return View("PaymentConfirm");
		}

		private async Task XuLyThanhToanThanhCong(string orderId, string transactionId, int phuongThucThanhToan)
		{
			var orderDetails = Session["PendingOrder"] as Dictionary<string, object>;
			if (orderDetails == null) return;

			string hoTen = orderDetails["HoTen"]?.ToString();
			string soDienThoai = orderDetails["SoDienThoai"]?.ToString();
			string email = orderDetails["Email"]?.ToString();
			string diaChi = orderDetails["DiaChi"]?.ToString();
			decimal phiVanChuyen = Convert.ToDecimal(orderDetails["PhiVanChuyen"]);
			string maGiamGia = orderDetails["MaGiamGia"]?.ToString();
			decimal giamGia = Convert.ToDecimal(orderDetails["GiamGia"]);
			var cartItems = orderDetails["CartItems"] as List<Dictionary<string, object>>;

			if (string.IsNullOrEmpty(hoTen) || cartItems == null || !cartItems.Any()) return;

			var maKH = Session["MaKH"]?.ToString(); // ✅ Lấy MaKH từ session

			var donHang = new DonDatHang
			{
				MaDonHang = Guid.NewGuid().ToString(),
				MaKH = maKH, // ✅ THÊM: Gán MaKH
				HoTen = hoTen,
				SoDienThoai = soDienThoai,
				Email = email,
				DiaChi = diaChi,
				NgayDat = DateTime.Now,
				TrangThai = "Đã xác nhận", // ✅ Đã thanh toán online -> Đã xác nhận luôn
				PhuongThucThanhToan = phuongThucThanhToan,
				PhiVanChuyen = phiVanChuyen, // ✅ THÊM
				MaGiamGia = string.IsNullOrEmpty(maGiamGia) ? null : maGiamGia // ✅ THÊM
			};

			decimal tongTien = 0;
			foreach (var item in cartItems)
			{
				string maSach = item["MaSach"]?.ToString();
				int soLuong = Convert.ToInt32(item["SoLuong"]);
				decimal donGia = Convert.ToDecimal(item["DonGia"]);

				var sach = await db.Saches.FindAsync(maSach);
				var thanhTien = soLuong * donGia;
				tongTien += thanhTien;

				if (sach != null)
					sach.SoLuongTon -= soLuong;

				db.ChiTietDonHangs.Add(new ChiTietDonHang
				{
					MaDonHang = donHang.MaDonHang,
					MaSach = maSach,
					SoLuong = soLuong,
					DonGia = donGia
				});
			}

			// ✅ THÊM: Tính và gán TongTien
			donHang.TongTien = tongTien + phiVanChuyen - giamGia;

			db.DonDatHangs.Add(donHang);

			// ✅ THÊM: Tạo bản ghi thanh toán với trạng thái "Đã thanh toán"
			var thanhToan = new ThanhToan
			{
				MaThanhToan = "TT" + DateTime.Now.ToString("yyyyMMddHHmmss") + donHang.MaDonHang.Substring(Math.Max(0, donHang.MaDonHang.Length - 4)),
				MaDonHang = donHang.MaDonHang,
				MaGiaoDich = transactionId, // ✅ Lưu mã giao dịch từ VNPay
				PhuongThucThanhToan = phuongThucThanhToan,
				TrangThaiThanhToan = "Đã thanh toán", // ✅ Đã thanh toán thành công
				SoTienThanhToan = donHang.TongTien ?? 0,
				NgayThanhToan = DateTime.Now, // ✅ Thời điểm thanh toán
				NgayTao = DateTime.Now
			};
			db.ThanhToans.Add(thanhToan);

			// ✅ THÊM: Lưu vào bảng SuDungMaGiamGia (nếu có dùng mã giảm giá)
			if (!string.IsNullOrEmpty(maGiamGia) && giamGia > 0)
			{
				var suDung = new SuDungMaGiamGia
				{
					MaCode = maGiamGia,
					MaDonHang = donHang.MaDonHang,
					MaKH = maKH,
					SoTienGiam = giamGia,
					NgaySuDung = DateTime.Now
				};
				db.SuDungMaGiamGias.Add(suDung);

				// Cập nhật số lần đã sử dụng mã giảm giá
				var maGiamGiaObj = await db.MaGiamGias.FirstOrDefaultAsync(m => m.MaCode == maGiamGia);
				if (maGiamGiaObj != null)
				{
					maGiamGiaObj.DaSuDung = (maGiamGiaObj.DaSuDung ?? 0) + 1;
				}
			}

			await db.SaveChangesAsync();

			// Gửi email
			var cart = cartItems.Select(i => new ChiTietGioHang
			{
				MaSach = i["MaSach"]?.ToString(),
				SoLuong = Convert.ToInt32(i["SoLuong"]),
				DonGia = Convert.ToDecimal(i["DonGia"])
			}).ToList();

			string emailBody = await TaoEmailBody(donHang, cart, tongTien, phiVanChuyen, giamGia, maGiamGia);
			SendMail sendMail = new SendMail();
			sendMail.SendMailFunction(email, "Xác nhận đơn hàng từ Cửa hàng sách BOOKSTORE", emailBody);

			// Xóa session và giỏ hàng
			Session["PendingOrder"] = null;
			await XoaGioHang();
		}

		private async Task<string> TaoEmailBody(DonDatHang donHang, List<ChiTietGioHang> cart, decimal tongTienHang,
			decimal phiVanChuyen, decimal giamGia, string maGiamGia)
		{
			var phuongThucText = donHang.PhuongThucThanhToan == 1 ? "Tiền mặt (COD)" :
								 donHang.PhuongThucThanhToan == 2 ? "VNPay" : 
								 donHang.PhuongThucThanhToan == 3 ? "MoMo" : "Không xác định";

			string emailBody = $"<div style='color: black;'>" +
				$"<p>Chào {donHang.HoTen},</p>" +
				$"<p>Bạn đã đặt hàng thành công vào lúc {donHang.NgayDat:HH:mm:ss dd/MM/yyyy}.</p>" +
				$"<p>Mã đơn hàng của bạn là: <strong>{donHang.MaDonHang}</strong></p>" +
				$"<p><strong>Phương thức thanh toán:</strong> {phuongThucText}</p>" +
				$"<p><strong>Địa chỉ giao hàng:</strong> {donHang.DiaChi}</p>" +
				"<p>Chi tiết đơn hàng:</p>" +
				"<table border='1' cellspacing='0' cellpadding='5' style='border-collapse: collapse; width: 100%;'>" +
				"<thead><tr>" +
				"<th style='text-align:left;'>Tên sách</th>" +
				"<th>Số lượng</th>" +
				"<th>Đơn giá</th>" +
				"<th>Thành tiền</th>" +
				"</tr></thead><tbody>";

			foreach (var item in cart)
			{
				var sach = await db.Saches.FindAsync(item.MaSach);
				var thanhTien = (item.SoLuong ?? 0) * (item.DonGia ?? 0);

				emailBody += $"<tr>" +
							 $"<td>{(sach != null ? sach.TenSach : item.MaSach)}</td>" +
							 $"<td style='text-align:center;'>{item.SoLuong}</td>" +
							 $"<td style='text-align:right;'>{item.DonGia:N0}₫</td>" +
							 $"<td style='text-align:right;'>{thanhTien:N0}₫</td>" +
							 "</tr>";
			}

			var tongTienCuoi = tongTienHang + phiVanChuyen - giamGia;

			emailBody += $"</tbody></table>" +
						 $"<table style='margin-top: 15px; width: 100%;'>" +
						 $"<tr><td style='text-align:right;'><strong>Tạm tính:</strong></td><td style='text-align:right;'>{tongTienHang:N0}₫</td></tr>" +
						 $"<tr><td style='text-align:right;'><strong>Phí vận chuyển:</strong></td><td style='text-align:right;'>{phiVanChuyen:N0}₫</td></tr>";

			if (giamGia > 0)
			{
				emailBody += $"<tr><td style='text-align:right;'><strong>Giảm giá ({maGiamGia}):</strong></td><td style='text-align:right; color: red;'>-{giamGia:N0}₫</td></tr>";
			}

			emailBody += $"<tr><td style='text-align:right;'><strong>Tổng cộng:</strong></td><td style='text-align:right; color: #007bff; font-size: 1.2em;'><strong>{tongTienCuoi:N0}₫</strong></td></tr>" +
						 $"</table>" +
						 "<p><strong>Cảm ơn quý khách đã mua hàng tại cửa hàng Sách Trực Tuyến!</strong></p>" +
						 "<p>Hotline hỗ trợ: <strong>+060 (800) 801-582</strong></p>" +
						 "<p><em>Trân trọng,</em><br/>HT STORE</p>" +
						 "</div>";

			return emailBody;
		}

		private async Task XoaGioHang()
		{
			var maKH = Session["MaKH"]?.ToString();
			if (string.IsNullOrEmpty(maKH))
			{
				Session["Cart"] = null;
			}
			else
			{
				var gioHang = await db.GioHangs.Include("ChiTietGioHangs").FirstOrDefaultAsync(g => g.MaKH == maKH);
				if (gioHang != null)
				{
					db.ChiTietGioHangs.RemoveRange(gioHang.ChiTietGioHangs);
					db.GioHangs.Remove(gioHang);
					await db.SaveChangesAsync();
				}
			}
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