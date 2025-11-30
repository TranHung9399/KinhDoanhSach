using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace banSach.Other
{
    public class MomoLib
    {
        // Request data properties
        public string PartnerCode { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string RequestId { get; set; }
        public string OrderId { get; set; }
        public string OrderInfo { get; set; }
        public string ReturnUrl { get; set; }
        public string NotifyUrl { get; set; }
        public long Amount { get; set; }
        public string RequestType { get; set; }
        public string ExtraData { get; set; }

        public MomoLib()
        {
            RequestType = "captureWallet";
            ExtraData = "";
        }

        /// <summary>
        /// Tạo chữ ký HMAC SHA256 cho MoMo
        /// </summary>
        private string CreateSignature(string rawData, string secretKey)
        {
            byte[] keyByte = Encoding.UTF8.GetBytes(secretKey);
            byte[] messageBytes = Encoding.UTF8.GetBytes(rawData);
            
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return BitConverter.ToString(hashmessage).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// Tạo request gửi đến MoMo API
        /// </summary>
        public async Task<MomoPaymentResponse> CreatePaymentAsync(string endpoint)
        {
            // Tạo rawHash theo định dạng của MoMo
            string rawHash = $"accessKey={AccessKey}&amount={Amount}&extraData={ExtraData}&ipnUrl={NotifyUrl}" +
                           $"&orderId={OrderId}&orderInfo={OrderInfo}&partnerCode={PartnerCode}" +
                           $"&redirectUrl={ReturnUrl}&requestId={RequestId}&requestType={RequestType}";

            // Tạo signature
            string signature = CreateSignature(rawHash, SecretKey);

            // Tạo request body
            var requestData = new
            {
                partnerCode = PartnerCode,
                accessKey = AccessKey,
                requestId = RequestId,
                amount = Amount,
                orderId = OrderId,
                orderInfo = OrderInfo,
                redirectUrl = ReturnUrl,
                ipnUrl = NotifyUrl,
                extraData = ExtraData,
                requestType = RequestType,
                signature = signature,
                lang = "vi"
            };

            string jsonRequest = JsonConvert.SerializeObject(requestData);

            try
            {
                using (var httpClient = new HttpClient())
                {
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(endpoint, content);
                    
                    string responseContent = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var momoResponse = JsonConvert.DeserializeObject<MomoPaymentResponse>(responseContent);
                        return momoResponse;
                    }
                    else
                    {
                        return new MomoPaymentResponse
                        {
                            ResultCode = -1,
                            Message = "Lỗi kết nối đến MoMo: " + responseContent
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new MomoPaymentResponse
                {
                    ResultCode = -1,
                    Message = "Lỗi: " + ex.Message
                };
            }
        }

        /// <summary>
        /// Xác thực chữ ký từ MoMo callback
        /// </summary>
        public bool ValidateSignature(string signature, string rawData, string secretKey)
        {
            string expectedSignature = CreateSignature(rawData, secretKey);
            return signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Xác thực callback từ MoMo
        /// </summary>
        public bool ValidateCallback(Dictionary<string, string> momoData, string secretKey)
        {
            if (!momoData.ContainsKey("signature"))
                return false;

            string signature = momoData["signature"];
            
            // Tạo rawHash từ dữ liệu callback
            string rawHash = $"accessKey={momoData.GetValueOrDefault("accessKey", "")}" +
                           $"&amount={momoData.GetValueOrDefault("amount", "")}" +
                           $"&extraData={momoData.GetValueOrDefault("extraData", "")}" +
                           $"&message={momoData.GetValueOrDefault("message", "")}" +
                           $"&orderId={momoData.GetValueOrDefault("orderId", "")}" +
                           $"&orderInfo={momoData.GetValueOrDefault("orderInfo", "")}" +
                           $"&orderType={momoData.GetValueOrDefault("orderType", "")}" +
                           $"&partnerCode={momoData.GetValueOrDefault("partnerCode", "")}" +
                           $"&payType={momoData.GetValueOrDefault("payType", "")}" +
                           $"&requestId={momoData.GetValueOrDefault("requestId", "")}" +
                           $"&responseTime={momoData.GetValueOrDefault("responseTime", "")}" +
                           $"&resultCode={momoData.GetValueOrDefault("resultCode", "")}" +
                           $"&transId={momoData.GetValueOrDefault("transId", "")}";

            return ValidateSignature(signature, rawHash, secretKey);
        }
    }

    /// <summary>
    /// Class để deserialize response từ MoMo
    /// </summary>
    public class MomoPaymentResponse
    {
        [JsonProperty("partnerCode")]
        public string PartnerCode { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("amount")]
        public long Amount { get; set; }

        [JsonProperty("responseTime")]
        public long ResponseTime { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("resultCode")]
        public int ResultCode { get; set; }

        [JsonProperty("payUrl")]
        public string PayUrl { get; set; }

        [JsonProperty("deeplink")]
        public string Deeplink { get; set; }

        [JsonProperty("qrCodeUrl")]
        public string QrCodeUrl { get; set; }
    }

    /// <summary>
    /// Extension method để lấy giá trị từ Dictionary
    /// </summary>
    public static class DictionaryExtensions
    {
        public static string GetValueOrDefault(this Dictionary<string, string> dictionary, string key, string defaultValue = "")
        {
            return dictionary.TryGetValue(key, out string value) ? value : defaultValue;
        }
    }
}
