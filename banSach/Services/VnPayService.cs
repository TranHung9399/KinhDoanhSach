using System;
using System.Collections.Specialized;
using System.Configuration;
using banSach.Other;

namespace banSach.Services
{
	public class VnPaySettings
	{
		public string PaymentUrl { get; set; }
		public string ApiUrl { get; set; }
		public string ReturnUrl { get; set; }
		public string TmnCode { get; set; }
		public string HashSecret { get; set; }

		public static VnPaySettings FromConfig()
		{
			return new VnPaySettings
			{
				PaymentUrl = ConfigurationManager.AppSettings["Url"],
				ApiUrl = ConfigurationManager.AppSettings["vnp_Api"],
				ReturnUrl = ConfigurationManager.AppSettings["ReturnUrl"],
				TmnCode = ConfigurationManager.AppSettings["TmnCode"],
				HashSecret = ConfigurationManager.AppSettings["HashSecret"]
			};
		}

		public void Validate()
		{
			if (string.IsNullOrWhiteSpace(PaymentUrl) ||
				string.IsNullOrWhiteSpace(ReturnUrl) ||
				string.IsNullOrWhiteSpace(TmnCode) ||
				string.IsNullOrWhiteSpace(HashSecret))
			{
				throw new ConfigurationErrorsException("Thiếu cấu hình VNPay trong Web.config.");
			}
		}
	}

	public class VnPayPaymentRequest
	{
		public string OrderId { get; set; }
		public decimal Amount { get; set; }
		public string OrderInfo { get; set; }
		public string OrderType { get; set; } = "other";
		public string Locale { get; set; } = "vn";
		public string BankCode { get; set; }
		public string IpAddress { get; set; }
		public DateTime CreatedDate { get; set; } = DateTime.Now;
	}

	public class VnPayPaymentResponse
	{
		public bool IsValidSignature { get; set; }
		public string ResponseCode { get; set; }
		public string OrderId { get; set; }
		public string TransactionNo { get; set; }
		public string Amount { get; set; }
		public string PayDate { get; set; }
		public string Message { get; set; }
		public bool IsSuccess => IsValidSignature && ResponseCode == "00";
	}

	public class VnPayService
	{
		private readonly VnPaySettings _settings;

		public VnPayService()
			: this(VnPaySettings.FromConfig())
		{
		}

		public VnPayService(VnPaySettings settings)
		{
			_settings = settings ?? throw new ArgumentNullException(nameof(settings));
		}

		public string CreatePaymentUrl(VnPayPaymentRequest request)
		{
			if (request == null) throw new ArgumentNullException(nameof(request));
			if (string.IsNullOrWhiteSpace(request.OrderId))
				throw new ArgumentException("Thiếu mã tham chiếu đơn hàng.", nameof(request.OrderId));

			_settings.Validate();
			var pay = new PayLib();

			pay.AddRequestData("vnp_Version", "2.1.0");
			pay.AddRequestData("vnp_Command", "pay");
			pay.AddRequestData("vnp_TmnCode", _settings.TmnCode);
			pay.AddRequestData("vnp_Amount", Convert.ToInt64(request.Amount * 100).ToString());
			pay.AddRequestData("vnp_BankCode", request.BankCode ?? string.Empty);
			pay.AddRequestData("vnp_CreateDate", request.CreatedDate.ToString("yyyyMMddHHmmss"));
			pay.AddRequestData("vnp_CurrCode", "VND");
			pay.AddRequestData("vnp_IpAddr", request.IpAddress ?? Util.GetIpAddress());
			pay.AddRequestData("vnp_Locale", request.Locale ?? "vn");
			pay.AddRequestData("vnp_OrderInfo", request.OrderInfo);
			pay.AddRequestData("vnp_OrderType", request.OrderType ?? "other");
			pay.AddRequestData("vnp_ReturnUrl", _settings.ReturnUrl);
			pay.AddRequestData("vnp_TxnRef", request.OrderId);

			return pay.CreateRequestUrl(_settings.PaymentUrl, _settings.HashSecret);
		}

		public VnPayPaymentResponse ParsePaymentResponse(NameValueCollection queryParameters)
		{
			if (queryParameters == null || queryParameters.Count == 0)
			{
				return new VnPayPaymentResponse
				{
					IsValidSignature = false,
					Message = "Không có dữ liệu phản hồi từ VNPay."
				};
			}

			var pay = new PayLib();

			foreach (string key in queryParameters)
			{
				if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
				{
					pay.AddResponseData(key, queryParameters[key]);
				}
			}

			var secureHash = queryParameters["vnp_SecureHash"];
			bool isValidSignature = !string.IsNullOrEmpty(secureHash) &&
									pay.ValidateSignature(secureHash, _settings.HashSecret);

			return new VnPayPaymentResponse
			{
				IsValidSignature = isValidSignature,
				ResponseCode = pay.GetResponseData("vnp_ResponseCode"),
				OrderId = pay.GetResponseData("vnp_TxnRef"),
				TransactionNo = pay.GetResponseData("vnp_TransactionNo"),
				Amount = pay.GetResponseData("vnp_Amount"),
				PayDate = pay.GetResponseData("vnp_PayDate"),
				Message = isValidSignature ? string.Empty : "Chữ ký không hợp lệ."
			};
		}
	}
}

