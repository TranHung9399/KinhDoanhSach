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

            // Tìm kiếm sách
            if (message.Contains("tìm") || message.Contains("sách") || message.Contains("tác giả") || 
                message.Contains("nhà xuất bản") || message.Contains("mua"))
            {
                return "SEARCH_BOOK";
            }

            // Theo dõi đơn hàng
            if (message.Contains("đơn hàng") || message.Contains("đơn") || message.Contains("mã đơn") ||
                message.Contains("tình trạng") || message.Contains("trạng thái") || message.Contains("dh"))
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

            // Gợi ý sách (chung)
            if (message.Contains("gợi ý") || message.Contains("giới thiệu") || message.Contains("đề xuất") ||
                message.Contains("nên đọc") || message.Contains("hay"))
            {
                return "BOOK_RECOMMENDATION";
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

                // Lấy danh sách sách từ database
                var books = _db.Saches
                    .Where(s => s.TenSach.Contains(keyword) || 
                               s.VietSaches.Any(v => v.TacGia.TenTG.Contains(keyword)) ||
                               s.NhaXuatBan.TenNXB.Contains(keyword))
                    .Select(s => new
                    {
                        s.MaSach,
                        s.TenSach,
                        TacGia = s.VietSaches.FirstOrDefault() != null ? s.VietSaches.FirstOrDefault().TacGia.TenTG : "Chưa xác định",
                        NhaXuatBan = s.NhaXuatBan.TenNXB,
                        s.GiaBan,
                        s.SoLuongTon,
                        s.MoTa
                    })
                    .Take(5)
                    .ToList();

                if (books.Any())
                {
                    var bookList = string.Join("\n", books.Select(b =>
                        $"- {b.TenSach} (Tác giả: {b.TacGia}, NXB: {b.NhaXuatBan}, Giá: {b.GiaBan:N0}đ, Còn: {b.SoLuongTon} cuốn)\n  Mô tả: {(b.MoTa?.Length > 100 ? b.MoTa.Substring(0, 100) + "..." : b.MoTa)}\n  Link: /Book/Details/{b.MaSach}"));

                    return $"Các sách tìm thấy với từ khóa '{keyword}':\n{bookList}";
                }
                else
                {
                    return $"Không tìm thấy sách nào phù hợp với từ khóa '{keyword}'.";
                }
            }
            catch
            {
                return "Không thể tìm kiếm sách lúc này.";
            }
        }

        private string ExtractSearchKeyword(string message)
        {
            // Chuyển về chữ thường để xử lý
            var lowerMessage = message.ToLower();
            
            // Danh sách các từ khóa cần loại bỏ
            var stopWords = new[] { 
                "tìm kiếm sách", "tìm sách", "tìm kiếm", "tìm", 
                "mua sách", "mua", 
                "cho tôi hỏi về sách", "hỏi về sách",
                "tác giả", "nhà xuất bản", "nxb",
                "có bán", "bán"
            };

            foreach (var word in stopWords)
            {
                if (lowerMessage.Contains(word))
                {
                    lowerMessage = lowerMessage.Replace(word, " ");
                }
            }
            
            // Xóa các ký tự đặc biệt nếu cần, nhưng giữ lại số và chữ
            // Ở đây chỉ đơn giản là trim và xóa khoảng trắng thừa
            while (lowerMessage.Contains("  "))
            {
                lowerMessage = lowerMessage.Replace("  ", " ");
            }

            return lowerMessage.Trim();
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
                        TacGia = s.VietSaches.FirstOrDefault() != null ? s.VietSaches.FirstOrDefault().TacGia.TenTG : "Chưa xác định",
                        s.GiaBan,
                        TheLoai = s.Loai.TenLoai,
                        s.MoTa
                    })
                    .ToList();

                if (books.Any())
                {
                    var bookList = string.Join("\n", books.Select(b =>
                        $"- {b.TenSach} ({b.TheLoai})\n  Tác giả: {b.TacGia} | Giá: {b.GiaBan:N0}đ\n  {(b.MoTa?.Length > 80 ? b.MoTa.Substring(0, 80) + "..." : b.MoTa)}\n  Link: /Book/Details/{b.MaSach}"));

                    return $"Gợi ý sách cho bạn:\n{bookList}";
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

**THÔNG TIN CẦN DÙNG:**
{contextInfo}

**NGUYÊN TẮC:**
1. Luôn chào hỏi lịch sự, xưng hô 'bạn' hoặc 'anh/chị'
2. Khi giới thiệu sách: nêu rõ tên, tác giả, giá, link xem chi tiết
3. Khi tra đơn hàng: hiển thị đầy đủ trạng thái, ngày đặt, tổng tiền
4. Khi thông báo mã giảm giá: nêu rõ mã, giá trị, thời hạn
5. Khi gợi ý sách cá nhân: giải thích lý do gợi ý (dựa trên sở thích/lịch sử mua)
6. Khi trả lời chính sách: trích dẫn chính xác từ thông tin cung cấp
7. Khi xử lý khiếu nại: tỏ thái độ thông cảm, cung cấp hotline và quy trình xử lý
8. Nếu không có thông tin: lịch sự xin lỗi và hướng dẫn rõ ràng
9. Luôn kết thúc bằng câu hỏi thân thiện để tiếp tục hỗ trợ

**LƯU Ý:**
- Nếu khách hỏi về sản phẩm không phải sách: Lịch sự giải thích chuyên về sách
- Nếu thiếu thông tin (VD: không có mã đơn): Yêu cầu bổ sung cụ thể
- Đối với thông tin GIÁ BÁN, SỐ LƯỢNG TỒN, TRẠNG THÁI ĐƠN HÀNG: Chỉ sử dụng thông tin từ dữ liệu được cung cấp. Tuyệt đối không tự bịa.
- Đối với NỘI DUNG SÁCH, MÔ TẢ, THÔNG TIN TÁC GIẢ: Nếu dữ liệu thiếu, bạn CÓ THỂ sử dụng kiến thức của mình để giới thiệu thêm cho khách hàng, nhưng hãy nói rõ đó là thông tin bổ sung.
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
