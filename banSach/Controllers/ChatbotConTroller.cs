using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using banSach.Services;
using Newtonsoft.Json;

namespace banSach.Controllers
{
	public class ChatbotController : Controller
	{
		private readonly GeminiAIService _aiService;

		public ChatbotController()
		{
			_aiService = new GeminiAIService();
		}

		[HttpPost]
		public async Task<JsonResult> SendMessage(string userMessage, string historyJson)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(userMessage))
				{
					return Json(new
					{
						success = false,
						reply = "Vui lòng nhập nội dung tin nhắn."
					});
				}

				// Parse chat history
				List<ChatMessage> chatHistory = new List<ChatMessage>();
				if (!string.IsNullOrEmpty(historyJson))
				{
					try
					{
						chatHistory = JsonConvert.DeserializeObject<List<ChatMessage>>(historyJson) ?? new List<ChatMessage>();
					}
					catch
					{
						chatHistory = new List<ChatMessage>();
					}
				}

			// Lấy userId từ session nếu có
			string userId = null;
			if (Session["MaKH"] != null)
			{
				userId = Session["MaKH"].ToString();
			}

			// Xử lý tin nhắn với Gemini AI
			var reply = await _aiService.ProcessUserMessage(userMessage, chatHistory, userId);

				return Json(new
				{
					success = true,
					reply = reply,
					history = JsonConvert.SerializeObject(chatHistory)
				});
			}
			catch (Exception ex)
			{
				return Json(new
				{
					success = false,
					reply = $"⚠️ Xin lỗi, đã xảy ra lỗi: {ex.Message}"
				});
			}
		}

		// API test để kiểm tra kết nối
		[HttpGet]
		public JsonResult TestConnection()
		{
			return Json(new
			{
				success = true,
				message = "Chatbot API is running!",
				timestamp = DateTime.Now
			}, JsonRequestBehavior.AllowGet);
		}
	}
}