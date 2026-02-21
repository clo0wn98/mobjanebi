using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using EShop.Data;

namespace EShop.Services
{
    public interface ISmsService
    {
        Task<bool> SendVerificationCode(string phoneNumber, string code);
        Task<bool> SendOrderConfirmation(string phoneNumber, string orderId, decimal amount);
        Task<bool> SendTrackingCode(string phoneNumber, string orderId, string trackingCode);
        Task<bool> SendToAdmin(string message);
    }

    public class IpPanelSmsService : ISmsService
    {
        private readonly string _apiKey;
        private readonly string _sender;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly HttpClient _httpClient;

        public IpPanelSmsService(IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            _apiKey = configuration["SmsSettings:ApiKey"] ?? "YTA4NGJiNzItNmY4ZC00MDcyLWEzYzgtYWEyM2MxMmRiNzUyMjA1MWFmZTM2OGYxYjE5NTNhY2ZjNTZjMDRmZTc5NDM=";
            _sender = configuration["SmsSettings:Sender"] ?? "+98PRO";
            _scopeFactory = scopeFactory;
            _httpClient = new HttpClient();
        }

        private async Task<List<string>> GetAdminPhones()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var adminSettings = await context.AdminSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == "AdminPhones");
                
                if (adminSettings == null || string.IsNullOrWhiteSpace(adminSettings.SettingValue))
                {
                    var fallback = "09931234567";
                    return new List<string> { fallback };
                }
                
                return adminSettings.SettingValue
                    .Split(',')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList();
            }
            catch
            {
                return new List<string> { "09931234567" };
            }
        }

        public async Task<bool> SendVerificationCode(string phoneNumber, string code)
        {
            return await SendSms(phoneNumber, $"کد تأیید MobJanebi: {code}");
        }

        public async Task<bool> SendOrderConfirmation(string phoneNumber, string orderId, decimal amount)
        {
            var amountStr = amount.ToString("N0");
            return await SendSms(phoneNumber, $"MobJanebi: سفارش #{orderId} با موفقیت ثبت شد. مبلغ: {amountStr} تومان");
        }

        public async Task<bool> SendTrackingCode(string phoneNumber, string orderId, string trackingCode)
        {
            return await SendSms(phoneNumber, $"MobJanebi: سفارش #{orderId} شما ارسال شد. کد پستی: {trackingCode}");
        }

        public async Task<bool> SendToAdmin(string message)
        {
            var adminPhones = await GetAdminPhones();
            bool allSent = true;
            foreach (var adminPhone in adminPhones)
            {
                var sent = await SendSms(adminPhone, $"MobJanebi Admin: {message}");
                if (!sent) allSent = false;
            }
            return allSent;
        }

        private async Task<bool> SendSms(string phoneNumber, string message)
        {
            try
            {
                var formattedPhone = FormatPhoneNumber(phoneNumber);
                
                var url = "https://edge.ippanel.com/v1/api/send";
                
                var jsonBody = $@"{{
                    ""message"": ""{message}"",
                    ""from_number"": ""{_sender}"",
                    ""sending_type"": ""normal"",
                    ""params"": {{
                        ""recipients"": [""{formattedPhone}""]
                    }}
                }}";

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", _apiKey);

                var response = await _httpClient.PostAsync(url, content);
                
                var responseBody = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"SMS Response: {responseBody}");
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SMS Error: {ex.Message}");
                return false;
            }
        }

        private string FormatPhoneNumber(string phone)
        {
            phone = phone.Trim().Replace(" ", "").Replace("-", "");
            
            if (phone.StartsWith("+98"))
                phone = phone.Substring(3);
            else if (phone.StartsWith("98"))
                phone = phone.Substring(2);
            else if (phone.StartsWith("0"))
                phone = phone.Substring(1);
            
            return phone;
        }
    }
}
