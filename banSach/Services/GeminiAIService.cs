using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using banSach.Models;
using Newtonsoft.Json;

namespace banSach.Services
{
    public class GeminiAIService
    {
        private readonly string _apiKey;
        private readonly string _apiUrl;
        private readonly QLBanSachEntities _db;
        private static readonly HttpClient _httpClient = new HttpClient();

        public GeminiAIService()
        {
            _apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];
            _apiUrl = ConfigurationManager.AppSettings["GeminiApiUrl"];
            _db = new QLBanSachEntities();
        }

    public async Task<string> ProcessUserMessage(string userMessage, List<ChatMessage> chatHistory, string userId = null)
        {
            try
            {
                // Kiểm tra intent của người dùng
                var intent = DetectIntent(userMessage);

                string contextInfo = "";
                switch (intent)
                {
                    case "SEARCH_BOOK":
                        contextInfo = GetBookSearchContext(userMessage);
                        break;
                    case "ORDER_STATUS":
                        contextInfo = GetOrderStatusContext(userMessage);
                        break;
                    case "DISCOUNT_CODE":
                        contextInfo = GetDiscountContext();
                        break;
                    case "BOOK_RECOMMENDATION":
                        contextInfo = GetRecommendationContext();
                        break;
                    case "PERSONAL_RECOMMENDATION":
                        contextInfo = GetPersonalRecommendationContext(userId);
                        break;
                    case "POLICY_QUESTION":
                        contextInfo = GetPolicyContext();
                        break;
                    case "COMPLAINT":
                        contextInfo = GetComplaintContext();
                        break;
                }

                // Tạo system prompt
                var systemPrompt = BuildSystemPrompt(contextInfo);

                // Gọi Gemini API
                var response = await CallGeminiAPI(systemPrompt, userMessage, chatHistory);

                // Lưu lịch sử chat
                chatHistory.Add(new ChatMessage { Role = "user", Content = userMessage });
                chatHistory.Add(new ChatMessage { Role = "assistant", Content = response });

                return response;
            }
            catch (Exception ex)
            {
                return $"Xin lỗi, hệ thống đang gặp sự cố. Vui lòng thử lại sau! Chi tiết lỗi: {ex.Message}";
            }
        }

        private string DetectIntent(string message)
        {
            message = message.ToLower();

            // Cá nhân hóa (ưu tiên cao hơn gợi ý chung)
            if ((message.Contains("gợi ý") || message.Contains("giới thiệu")) && 
                (message.Contains("cho tôi") || message.Contains("của tôi") || message.Contains("mình")))
            {
                return "PERSONAL_RECOMMENDATION";
            }

            // Theo dõi đơn hàng (kiểm tra sớm để tránh nhầm với tìm kiếm sách)
            if (message.Contains("đơn hàng") || message.Contains("mã đơn") ||
                message.Contains("tình trạng") || message.Contains("trạng thái") || 
                (message.Contains("đơn") && (message.Contains("dh") || message.Contains("kiểm tra") || message.Contains("theo dõi"))))
            {
                return "ORDER_STATUS";
            }

            // Mã giảm giá
            if (message.Contains("giảm giá") || message.Contains("mã giảm") || message.Contains("voucher") ||
                message.Contains("khuyến mãi") || message.Contains("coupon"))
            {
                return "DISCOUNT_CODE";
            }

            // Chính sách
            if (message.Contains("chính sách") || message.Contains("đổi trả") || message.Contains("bảo hành") ||
                message.Contains("vận chuyển") || message.Contains("ship") || message.Contains("hoàn tiền"))
            {
                return "POLICY_QUESTION";
            }

            // Khiếu nại
            if (message.Contains("khiếu nại") || message.Contains("phàn nàn") || message.Contains("tệ") ||
                message.Contains("hỏng") || message.Contains("rách") || message.Contains("nhầm") || message.Contains("sai"))
            {
                return "COMPLAINT";
            }

            // Gợi ý sách (chung) - kiểm tra trước SEARCH_BOOK
            if ((message.Contains("gợi ý") || message.Contains("giới thiệu") || message.Contains("đề xuất") ||
                message.Contains("nên đọc")) && !message.Contains("tìm"))
            {
                return "BOOK_RECOMMENDATION";
            }

            // Tìm kiếm sách - MỞ RỘNG LOGIC
            // Trường hợp 1: Có từ khóa tìm kiếm rõ ràng
            if (message.Contains("tìm") || message.Contains("sách") || message.Contains("tác giả") || 
                message.Contains("nhà xuất bản") || message.Contains("nxb") || message.Contains("mua") ||
                message.Contains("có bán") || message.Contains("bán không"))
            {
                return "SEARCH_BOOK";
            }

            // Trường hợp 2: Câu hỏi về sách cụ thể (có "cuốn", "quyển", "truyện")
            if (message.Contains("cuốn") || message.Contains("quyển") || message.Contains("truyện") ||
                message.Contains("tiểu thuyết") || message.Contains("tập"))
            {
                return "SEARCH_BOOK";
            }

            // Trường hợp 3: Câu hỏi chứa "cần", "muốn" (ám chỉ tìm kiếm)
            if ((message.Contains("cần") || message.Contains("muốn") || message.Contains("có")) && 
                message.Length > 10) // Đảm bảo câu đủ dài để không nhầm với câu chào
            {
                return "SEARCH_BOOK";
            }

            return "GENERAL";
        }
        
        // ... (GetBookSearchContext, GetOrderStatusContext, GetDiscountContext, GetRecommendationContext kept as is) ...

        private string GetPersonalRecommendationContext(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return "Khách hàng chưa đăng nhập. Hãy gợi ý các sách bán chạy nhất và khuyên họ đăng nhập để có gợi ý chính xác hơn.";
            }

            try
            {
                // Lấy lịch sử mua hàng và yêu thích
                var boughtBooks = _db.DonDatHangs
                    .Where(d => d.MaKH == userId)
                    .SelectMany(d => d.ChiTietDonHangs)
                    .Select(ct => ct.Sach.Loai.TenLoai)
                    .Distinct()
                    .ToList();

                var likedBooks = _db.YeuThiches
                    .Where(y => y.MaKH == userId)
                    .Select(y => y.Sach.Loai.TenLoai)
                    .Distinct()
                    .ToList();

                var preferredGenres = boughtBooks.Union(likedBooks).Distinct().ToList();

                if (!preferredGenres.Any())
                {
                    return "Khách hàng chưa có lịch sử mua hàng hay yêu thích. Hãy gợi ý các sách bán chạy nhất.";
                }

                // Gợi ý sách cùng thể loại
                var recommendations = _db.Saches
                    .Where(s => preferredGenres.Contains(s.Loai.TenLoai))
                    .OrderByDescending(s => s.NgayNhapHang)
                    .Take(5)
                    .Select(s => new
                    {
                        s.TenSach,
                        TacGia = s.VietSaches.FirstOrDefault() != null ? s.VietSaches.FirstOrDefault().TacGia.TenTG : "Chưa xác định",
                        s.GiaBan,
                        TheLoai = s.Loai.TenLoai
                    })
                    .ToList();

                var recList = string.Join("\n", recommendations.Select(b =>
                    $"- {b.TenSach} ({b.TheLoai}) - {b.TacGia} - {b.GiaBan:N0}đ"));

                return $"Dựa trên sở thích của khách hàng (Thể loại: {string.Join(", ", preferredGenres)}), đây là các gợi ý:\n{recList}";
            }
            catch
            {
                return "Không thể lấy thông tin cá nhân lúc này.";
            }
        }

        private string GetPolicyContext()
        {
            return @"CHÍNH SÁCH CỦA CỬA HÀNG:
1. Vận chuyển:
- Miễn phí ship cho đơn từ 500k.
- Phí ship nội thành: 20k, ngoại thành: 30k.
- Thời gian giao: 2-3 ngày.

2. Đổi trả:
- Đổi trả trong vòng 7 ngày nếu sách lỗi (rách, in sai, hư hỏng do vận chuyển).
- Yêu cầu: Có video quay khi mở hàng.
- Hoàn tiền 100% nếu không có sách đổi.

3. Thanh toán:
- Hỗ trợ COD, Chuyển khoản, VNPay, Momo.";
        }

        private string GetComplaintContext()
        {
            return @"THÔNG TIN HỖ TRỢ KHIẾU NẠI:
- Hotline: 1900 1234 (8h-17h hàng ngày)
- Email: hotro@hieusachconcuyo.com
- Zalo: 0987654321
- Địa chỉ: 123 Đường Sách, TP.HCM

Quy trình xử lý:
1. Tiếp nhận thông tin qua Hotline/Email.
2. Xác minh trong 24h.
3. Đề xuất phương án giải quyết (đổi hàng/hoàn tiền/đền bù).";
        }

        private string GetBookSearchContext(string message)
        {
            try
            {
                // Trích xuất từ khóa tìm kiếm
                string keyword = ExtractSearchKeyword(message);

                // Tìm kiếm với nhiều chiến lược
                var books = SearchBooksMultipleStrategies(keyword);

                if (books.Any())
                {
                    var bookList = string.Join("\n\n", books.Select((b, index) =>
                        $"{index + 1}. **{b.TenSach}**\n" +
                        $"   - Hình ảnh: {(string.IsNullOrEmpty(b.Hinh) ? "/img/sach/default.jpg" : $"/img/sach/{b.Hinh}")}\n" +
                        $"   - Tác giả: {b.TacGia}\n" +
                        $"   - Nhà xuất bản: {b.NhaXuatBan}\n" +
                        $"   - Giá gốc: {b.GiaBan:N0}đ\n" +
                        $"   - Giá chiết khấu: {b.GiaChietKhau:N0}đ (Tiết kiệm {(b.GiaBan - b.GiaChietKhau):N0}đ)\n" +
                        $"   - Còn lại: {b.SoLuongTon} cuốn\n" +
                        $"   - Mô tả: {(string.IsNullOrEmpty(b.MoTa) ? "Chưa có mô tả" : (b.MoTa.Length > 150 ? b.MoTa.Substring(0, 150) + "..." : b.MoTa))}\n" +
                        $"   - Link chi tiết: /Book/Details/{b.MaSach}"));

                    return $@"✅ TÌM THẤY {books.Count} SÁCH PHÙ HỢP VỚI TỪ KHÓA ""{keyword}"":

{bookList}

🔔 LƯU Ý QUAN TRỌNG CHO AI:
- Đây là TOÀN BỘ thông tin sách CÓ TRONG CỬA HÀNG.
- TUYỆT ĐỐI CHỈ giới thiệu các sách trong danh sách trên.
- KHÔNG được tự bịa thêm sách khác hoặc thông tin giá/tồn kho không có trong danh sách.
- Nếu khách hỏi về sách không có trong danh sách trên, hãy thông báo: ""Xin lỗi, hiện tại cửa hàng chưa có sách này.""

📸 HƯỚNG DẪN HIỂN THỊ HÌNH ẢNH:
- Với mỗi sách, hãy hiển thị hình ảnh dưới dạng: <img src='[đường dẫn hình ảnh]' style='max-width: 150px; cursor: pointer;' onclick='window.location.href=""[link chi tiết]""' />
- Đường dẫn hình ảnh và link chi tiết đã được cung cấp trong danh sách trên.
- Hình ảnh phải có thể click để chuyển tới trang chi tiết sách.";
                }
                else
                {
                    return $@"❌ KHÔNG TÌM THẤY SÁCH PHÙ HỢP VỚI TỪ KHÓA ""{keyword}""

🔔 LƯU Ý QUAN TRỌNG:
- Cửa hàng KHÔNG CÓ sách này trong kho.
- TUYỆT ĐỐI KHÔNG được tự bịa thông tin về sách này.
- Hãy lịch sự thông báo với khách hàng rằng hiện tại cửa hàng chưa có sách ""{keyword}"".
- Có thể gợi ý khách hàng:
  + Để lại thông tin để nhận thông báo khi có hàng
  + Xem các sách tương tự đang có sẵn
  + Liên hệ hotline để được tư vấn thêm";
                }
            }
            catch (Exception ex)
            {
                return $"❌ LỖI TÌM KIẾM: Không thể tìm kiếm sách lúc này. Chi tiết lỗi: {ex.Message}";
            }
        }

        private List<dynamic> SearchBooksMultipleStrategies(string keyword)
        {
            var books = new List<dynamic>();

            try
            {
                // Chiến lược 1: Tìm kiếm chính xác (exact match)
                books = _db.Saches
                    .Where(s => s.TenSach.ToLower().Contains(keyword.ToLower()))
                    .Select(s => new
                    {
                        s.MaSach,
                        s.TenSach,
                        s.Hinh,
                        TacGia = s.VietSaches.FirstOrDefault() != null ? s.VietSaches.FirstOrDefault().TacGia.TenTG : "Chưa xác định",
                        NhaXuatBan = s.NhaXuatBan.TenNXB,
                        s.GiaBan,
                        GiaChietKhau = s.GiaChietKhau ?? s.GiaBan,
                        s.SoLuongTon,
                        s.MoTa
                    })
                    .Take(5)
                    .ToList<dynamic>();

                // Chiến lược 2: Nếu không tìm thấy, tìm theo tác giả
                if (!books.Any())
                {
                    books = _db.Saches
                        .Where(s => s.VietSaches.Any(v => v.TacGia.TenTG.ToLower().Contains(keyword.ToLower())))
                        .Select(s => new
                        {
                            s.MaSach,
                            s.TenSach,
                            s.Hinh,
                            TacGia = s.VietSaches.FirstOrDefault() != null ? s.VietSaches.FirstOrDefault().TacGia.TenTG : "Chưa xác định",
                            NhaXuatBan = s.NhaXuatBan.TenNXB,
                            s.GiaBan,
                            GiaChietKhau = s.GiaChietKhau ?? s.GiaBan,
                            s.SoLuongTon,
                            s.MoTa
                        })
                        .Take(5)
                        .ToList<dynamic>();
                }

                // Chiến lược 3: Nếu vẫn không tìm thấy, tìm theo NXB
                if (!books.Any())
                {
                    books = _db.Saches
                        .Where(s => s.NhaXuatBan.TenNXB.ToLower().Contains(keyword.ToLower()))
                        .Select(s => new
                        {
                            s.MaSach,
                            s.TenSach,
                            s.Hinh,
                            TacGia = s.VietSaches.FirstOrDefault() != null ? s.VietSaches.FirstOrDefault().TacGia.TenTG : "Chưa xác định",
                            NhaXuatBan = s.NhaXuatBan.TenNXB,
                            s.GiaBan,
                            GiaChietKhau = s.GiaChietKhau ?? s.GiaBan,
                            s.SoLuongTon,
                            s.MoTa
                        })
                        .Take(5)
                        .ToList<dynamic>();
                }

                // Chiến lược 4: Tìm kiếm fuzzy - tách keyword thành các từ riêng lẻ
                if (!books.Any() && keyword.Contains(" "))
                {
                    var words = keyword.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    books = _db.Saches
                        .Where(s => words.Any(w => s.TenSach.ToLower().Contains(w.ToLower())))
                        .Select(s => new
                        {
                            s.MaSach,
                            s.TenSach,
                            s.Hinh,
                            TacGia = s.VietSaches.FirstOrDefault() != null ? s.VietSaches.FirstOrDefault().TacGia.TenTG : "Chưa xác định",
                            NhaXuatBan = s.NhaXuatBan.TenNXB,
                            s.GiaBan,
                            GiaChietKhau = s.GiaChietKhau ?? s.GiaBan,
                            s.SoLuongTon,
                            s.MoTa
                        })
                        .Take(5)
                        .ToList<dynamic>();
                }
            }
            catch
            {
                // Nếu có lỗi, trả về danh sách rỗng
                books = new List<dynamic>();
            }

            return books;
        }

        private string ExtractSearchKeyword(string message)
        {
            // Chuyển về chữ thường để xử lý
            var lowerMessage = message.ToLower();
            var originalMessage = message.Trim();
            
            // Danh sách các từ khóa cần loại bỏ (chỉ giữ lại các từ khóa quan trọng)
            var stopWords = new[] { 
                "tôi cần tìm sách cuốn",
                "tôi cần tìm sách",
                "tìm kiếm sách",
                "tìm sách",
                "tìm kiếm",
                "cho tôi hỏi về sách",
                "hỏi về sách",
                "mua sách",
                "có bán",
                "bán không",
                "cho tôi",
                "tôi muốn",
                "tôi cần",
                "tác giả",
                "nhà xuất bản"
            };

            // Sắp xếp theo độ dài giảm dần để loại bỏ cụm từ dài trước
            stopWords = stopWords.OrderByDescending(w => w.Length).ToArray();

            foreach (var word in stopWords)
            {
                if (lowerMessage.Contains(word))
                {
                    lowerMessage = lowerMessage.Replace(word, " ");
                }
            }
            
            // Loại bỏ các từ đơn lẻ ít ý nghĩa (chỉ khi không phải là phần của tên sách)
            var singleStopWords = new[] { "tìm", "mua", "bán", "nxb", "tôi", "có", "không" };
            foreach (var word in singleStopWords)
            {
                // Chỉ loại bỏ nếu nó là từ đơn lẻ (có khoảng trắng trước/sau)
                lowerMessage = System.Text.RegularExpressions.Regex.Replace(
                    lowerMessage, 
                    $@"\b{word}\b", 
                    " ", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
            }
            
            // Xóa khoảng trắng thừa
            while (lowerMessage.Contains("  "))
            {
                lowerMessage = lowerMessage.Replace("  ", " ");
            }

            // Trim và trả về từ khóa
            var keyword = lowerMessage.Trim();
            
            // Nếu sau khi loại bỏ stopwords mà không còn gì hoặc quá ngắn (< 2 ký tự)
            // thì trả về message gốc
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 2)
            {
                keyword = originalMessage;
            }

            return keyword;
        }

        private string GetOrderStatusContext(string message)
        {
            try
            {
                // Trích xuất mã đơn hàng từ tin nhắn
                var words = message.Split(' ');
                string maDonHang = null;

                foreach (var word in words)
                {
                    if (word.StartsWith("DH") || (word.Length > 5 && int.TryParse(word, out _)))
                    {
                        maDonHang = word;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(maDonHang))
                {
                    return "Vui lòng cung cấp mã đơn hàng. Ví dụ: 'Kiểm tra đơn hàng DH12345'";
                }

                // Tìm đơn hàng
                var order = _db.DonDatHangs
                    .Where(d => d.MaDonHang == maDonHang || 
                               ("DH" + d.MaDonHang) == maDonHang)
                    .Select(d => new
                    {
                        d.MaDonHang,
                        d.NgayDat,
                        d.TrangThai,
                        d.TongTien,
                        d.DiaChi,
                        ChiTiet = d.ChiTietDonHangs.Select(ct => new
                        {
                            TenSach = ct.Sach.TenSach,
                            ct.SoLuong,
                            DonGia = ct.DonGia
                        }).ToList()
                    })
                    .FirstOrDefault();

                if (order != null)
                {
                    var chiTiet = string.Join("\n", order.ChiTiet.Select(ct =>
                        $"  • {ct.TenSach} x{ct.SoLuong} - {ct.DonGia:N0}đ"));

                    return $@"Thông tin đơn hàng #{order.MaDonHang}:
- Ngày đặt: {order.NgayDat:dd/MM/yyyy HH:mm}
- Trạng thái: {order.TrangThai}
- Tổng tiền: {order.TongTien:N0}đ
- Địa chỉ giao: {order.DiaChi}
- Chi tiết:
{chiTiet}";
                }
                else
                {
                    return $"Không tìm thấy đơn hàng '{maDonHang}'. Vui lòng kiểm tra lại mã đơn hàng.";
                }
            }
            catch (Exception ex)
            {
                return $"Không thể tra cứu đơn hàng lúc này. Lỗi: {ex.Message}";
            }
        }

        private string GetDiscountContext()
        {
            try
            {
                var now = DateTime.Now;
                var discounts = _db.MaGiamGias
                    .Where(m => m.NgayBatDau <= now && m.NgayKetThuc >= now && m.TrangThai == true)
                    .Select(m => new
                    {
                        m.MaCode,
                        TenChuongTrinh = m.TenChuongTrinh,
                        GiaTri = m.SoTienGiam ?? m.PhanTramGiam,
                        m.NgayKetThuc,
                        SoLuongToiDa = m.SoLuongMa,
                        SoLuongDaSuDung = m.DaSuDung
                    })
                    .ToList();

                if (discounts.Any())
                {
                    var discountList = string.Join("\n", discounts.Select(d =>
                        $"- Mã: {d.MaCode} - Giảm {d.GiaTri:N0}đ\n  {d.TenChuongTrinh}\n  Hạn: {d.NgayKetThuc:dd/MM/yyyy} | Còn: {(d.SoLuongToiDa ?? 0) - (d.SoLuongDaSuDung ?? 0)} mã"));

                    return $"Các mã giảm giá đang hoạt động:\n{discountList}";
                }
                else
                {
                    return "Hiện tại không có mã giảm giá nào.";
                }
            }
            catch
            {
                return "Không thể lấy thông tin mã giảm giá lúc này.";
            }
        }

        private string GetRecommendationContext()
        {
            try
            {
                // Lấy sách bán chạy hoặc mới nhất
                var books = _db.Saches
                    .OrderByDescending(s => s.NgayNhapHang)
                    .Take(5)
                    .Select(s => new
                    {
                        s.MaSach,
                        s.TenSach,
                        s.Hinh,
                        TacGia = s.VietSaches.FirstOrDefault() != null ? s.VietSaches.FirstOrDefault().TacGia.TenTG : "Chưa xác định",
                        s.GiaBan,
                        GiaChietKhau = s.GiaChietKhau ?? s.GiaBan,
                        TheLoai = s.Loai.TenLoai,
                        s.MoTa,
                        s.SoLuongTon
                    })
                    .ToList();

                if (books.Any())
                {
                    var bookList = string.Join("\n", books.Select(b =>
                        $"- {b.TenSach} ({b.TheLoai})\n" +
                        $"  Hình ảnh: {(string.IsNullOrEmpty(b.Hinh) ? "/img/sach/default.jpg" : $"/img/sach/{b.Hinh}")}\n" +
                        $"  Tác giả: {b.TacGia}\n" +
                        $"  Giá gốc: {b.GiaBan:N0}đ | Giá chiết khấu: {b.GiaChietKhau:N0}đ\n" +
                        $"  Còn: {b.SoLuongTon} cuốn\n" +
                        $"  {(b.MoTa?.Length > 80 ? b.MoTa.Substring(0, 80) + "..." : b.MoTa)}\n" +
                        $"  Link: /Book/Details/{b.MaSach}"));

                    return $@"Gợi ý sách cho bạn (Sách mới nhất):
{bookList}

📸 Hãy hiển thị hình ảnh cho mỗi cuốn sách bằng HTML như đã hướng dẫn.";
                }
                else
                {
                    return "Không có sách để gợi ý lúc này.";
                }
            }
            catch
            {
                return "Không thể gợi ý sách lúc này.";
            }
        }

        private string BuildSystemPrompt(string contextInfo)
        {
            return $@"Bạn là trợ lý ảo thông minh của Hiệu Sách Con Cú Vợ 🦉.

**VAI TRÒ:**
- Tư vấn sách, hỗ trợ khách hàng nhiệt tình, chuyên nghiệp
- Trả lời bằng tiếng Việt, lịch sự, thân thiện, vui vẻ
- Sử dụng emoji phù hợp để tạo sự gần gũi

**THÔNG TIN TỪ CƠ SỞ DỮ LIỆU (RAG - Retrieval-Augmented Generation):**
{contextInfo}

**NGUYÊN TẮC RAG - CỰC KỲ QUAN TRỌNG:**
1. 🚨 TUYỆT ĐỐI CHỈ sử dụng thông tin từ phần ""THÔNG TIN TỪ CƠ SỞ DỮ LIỆU"" bên trên
2. 🚨 NGHIÊM CẤM tự bịa đặt thông tin về:
   - Giá bán
   - Giá chiết khấu
   - Số lượng tồn kho
   - Trạng thái đơn hàng
   - Tên sách không có trong danh sách
   - Thông tin tác giả/NXB không được cung cấp
3. 🚨 Nếu thông tin KHÔNG CÓ trong phần ""THÔNG TIN TỪ CƠ SỞ DỮ LIỆU"":
   - Thông báo rõ ràng: ""Xin lỗi, hiện tại cửa hàng chưa có sách này""
   - Đề xuất: ""Bạn có thể để lại thông tin để được thông báo khi có hàng, hoặc liên hệ hotline: 1900 1234""
4. 🚨 Đối với thông tin CHUNG về nội dung sách (không liên quan đến giá/tồn kho):
   - CHỈ được bổ sung nếu khách hỏi cụ thể về nội dung/chủ đề
   - Phải nói rõ: ""Theo kiến thức chung, cuốn sách này...""
   - Luôn ưu tiên thông tin từ database nếu có

**HƯỚNG DẪN HIỂN THỊ HÌNH ẢNH SÁCH:**
1. 📸 BẮT BUỘC hiển thị hình ảnh cho mỗi cuốn sách khi giới thiệu
2. Format HTML để hiển thị hình ảnh:
   ```html
   <div style='border: 1px solid #ddd; padding: 10px; margin: 10px 0; border-radius: 8px;'>
     <a href='[Link chi tiết]' style='text-decoration: none; color: inherit;'>
       <img src='[Đường dẫn hình ảnh]' alt='[Tên sách]' style='max-width: 150px; max-height: 200px; cursor: pointer; border-radius: 4px; display: block; margin-bottom: 10px;' />
       <strong>[Tên sách]</strong>
     </a>
     <p>💰 Giá: <strike>[Giá gốc]</strike> <strong style='color: #e74c3c;'>[Giá chiết khấu]</strong></p>
     <p>📦 Còn: [Số lượng] cuốn</p>
     <p>✍️ Tác giả: [Tên tác giả]</p>
   </div>
   ```
3. Hình ảnh phải có thể click để chuyển đến trang chi tiết sách
4. Sử dụng thẻ <a> bao quanh hình ảnh để tạo link
5. Luôn hiển thị giá chiết khấu (nổi bật hơn) và giá gốc (gạch ngang)

**HƯỚNG DẪN TRẢ LỜI:**
1. Luôn chào hỏi lịch sự, xưng hô 'bạn' hoặc 'anh/chị'
2. Khi giới thiệu sách: 
   - CHỈ giới thiệu sách CÓ TRONG danh sách
   - BẮT BUỘC hiển thị hình ảnh (sử dụng HTML như hướng dẫn trên)
   - Nêu rõ tên, tác giả, giá gốc (gạch ngang), giá chiết khấu (nổi bật), số lượng còn
   - Sử dụng emoji: 📚 (sách), 💰 (giá), 📦 (tồn kho), ✍️ (tác giả)
3. Khi tra đơn hàng: 
   - CHỈ hiển thị thông tin từ database
   - Hiển thị đầy đủ trạng thái, ngày đặt, tổng tiền
4. Khi thông báo mã giảm giá: 
   - CHỈ thông báo mã CÓ TRONG database
   - Nêu rõ mã, giá trị, thời hạn
5. Khi gợi ý sách cá nhân: 
   - Giải thích lý do gợi ý (dựa trên sở thích/lịch sử mua)
   - Hiển thị hình ảnh cho mỗi sách
6. Khi trả lời chính sách: 
   - Trích dẫn chính xác từ thông tin cung cấp
7. Khi xử lý khiếu nại: 
   - Tỏ thái độ thông cảm
   - Cung cấp hotline và quy trình xử lý
8. Khi KHÔNG TÌM THẤY thông tin:
   - Lịch sự xin lỗi
   - Hướng dẫn rõ ràng: ""Hiện tại cửa hàng chưa có sách này""
   - Đề xuất liên hệ hotline: 1900 1234
9. Luôn kết thúc bằng câu hỏi thân thiện để tiếp tục hỗ trợ

**CÁC TRƯỜNG HỢP ĐẶC BIỆT:**
- Nếu khách hỏi về sản phẩm không phải sách: Lịch sự giải thích cửa hàng chuyên về sách
- Nếu thiếu thông tin (VD: không có mã đơn): Yêu cầu bổ sung cụ thể
- Nếu khách yêu cầu thông tin không có trong database: Thông báo lịch sự và đề xuất liên hệ trực tiếp

**VÍ DỤ TRẢ LỜI ĐÚNG:**
❌ SAI: ""Cuốn Đắc Nhân Tâm giá 80,000đ, còn 50 cuốn"" (khi database không có)
✅ ĐÚNG: ""Xin lỗi anh/chị, hiện tại cửa hàng chưa có sách 'Đắc Nhân Tâm'. Anh/chị có thể liên hệ hotline 1900 1234 để được tư vấn thêm nhé!""

❌ SAI: Chỉ hiển thị text không có hình ảnh
✅ ĐÚNG: Hiển thị hình ảnh + thông tin + giá chiết khấu nổi bật + link chi tiết

**VÍ DỤ HIỂN THỊ SÁCH:**
```html
<div style='border: 1px solid #ddd; padding: 10px; margin: 10px 0; border-radius: 8px;'>
  <a href='/Book/Details/S001' style='text-decoration: none; color: inherit;'>
    <img src='/img/sach/dacnhantam.jpg' alt='Đắc Nhân Tâm' style='max-width: 150px; max-height: 200px; cursor: pointer; border-radius: 4px; display: block; margin-bottom: 10px;' />
    <strong>📚 Đắc Nhân Tâm</strong>
  </a>
  <p>✍️ Tác giả: Dale Carnegie</p>
  <p>💰 Giá: <strike>86,000đ</strike> <strong style='color: #e74c3c;'>77,400đ</strong> (Tiết kiệm 8,600đ)</p>
  <p>📦 Còn: 15 cuốn</p>
  <p style='font-size: 0.9em; color: #666;'>Cuốn sách kinh điển về kỹ năng giao tiếp...</p>
  <a href='/Book/Details/S001' style='color: #3498db;'>👉 Xem chi tiết</a>
</div>
```
";
        }

        private async Task<string> CallGeminiAPI(string systemPrompt, string userMessage, List<ChatMessage> chatHistory)
        {
            try
            {
                // Kiểm tra API key
                if (string.IsNullOrEmpty(_apiKey) || _apiKey == "YOUR_GEMINI_API_KEY_HERE")
                {
                    return "⚠️ Chưa cấu hình API Key. Vui lòng xem hướng dẫn trong AI_CHATBOT_QUICKSTART.md";
                }

                // Xây dựng conversation history
                var contents = new List<object>();

                // Thêm system prompt
                contents.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = systemPrompt } }
                });
                contents.Add(new
                {
                    role = "model",
                    parts = new[] { new { text = "Tôi hiểu. Tôi là trợ lý ảo của Hiệu Sách Con Cú Vợ, sẵn sàng hỗ trợ khách hàng một cách chuyên nghiệp và thân thiện." } }
                });

                // Thêm lịch sử chat (lấy 10 tin nhắn gần nhất)
                var recentHistory = chatHistory.Count > 10 ? chatHistory.Skip(chatHistory.Count - 10).ToList() : chatHistory;
                foreach (var msg in recentHistory)
                {
                    contents.Add(new
                    {
                        role = msg.Role == "user" ? "user" : "model",
                        parts = new[] { new { text = msg.Content } }
                    });
                }

                // Thêm tin nhắn hiện tại
                contents.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = userMessage } }
                });

                var requestBody = new
                {
                    contents = contents,
                    generationConfig = new
                    {
                        temperature = 0.7,
                        topK = 40,
                        topP = 0.95,
                        maxOutputTokens = 1024
                    }
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var apiUrl = $"{_apiUrl}?key={_apiKey}";
                
                // Log request (chỉ dùng khi debug)
                System.Diagnostics.Debug.WriteLine($"[CHATBOT] Calling Gemini API: {_apiUrl}");
                
                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                // Log response (chỉ dùng khi debug)
                System.Diagnostics.Debug.WriteLine($"[CHATBOT] Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"[CHATBOT] Response Body: {responseJson}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<GeminiResponse>(responseJson);

                    if (result?.Candidates != null && result.Candidates.Length > 0)
                    {
                        var candidate = result.Candidates[0];
                        if (candidate?.Content?.Parts != null && candidate.Content.Parts.Length > 0)
                        {
                            var reply = candidate.Content.Parts[0].Text;
                            if (!string.IsNullOrEmpty(reply))
                            {
                                return reply;
                            }
                        }
                    }

                    // Nếu không có candidates, có thể bị blocked
                    return "⚠️ Gemini AI không thể tạo phản hồi. Có thể nội dung bị chặn hoặc API quá tải. Vui lòng thử lại!";
                }
                else
                {
                    // Log chi tiết lỗi HTTP
                    return $"⚠️ Lỗi API (HTTP {(int)response.StatusCode}): {responseJson}";
                }
            }
            catch (JsonException jsonEx)
            {
                return $"⚠️ Lỗi parse JSON: {jsonEx.Message}";
            }
            catch (HttpRequestException httpEx)
            {
                return $"⚠️ Lỗi kết nối mạng: {httpEx.Message}";
            }
            catch (Exception ex)
            {
                return $"⚠️ Lỗi không xác định: {ex.GetType().Name} - {ex.Message}";
            }
        }
    }

    // Các model class
    public class ChatMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    public class GeminiResponse
    {
        public GeminiCandidate[] Candidates { get; set; }
    }

    public class GeminiCandidate
    {
        public GeminiContent Content { get; set; }
    }

    public class GeminiContent
    {
        public GeminiPart[] Parts { get; set; }
    }

    public class GeminiPart
    {
        public string Text { get; set; }
    }
}
