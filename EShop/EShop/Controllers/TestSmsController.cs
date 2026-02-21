using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace EShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestSmsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TestSmsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> TestSms(string phone, string code)
        {
            var apiKey = _configuration["SmsSettings:ApiKey"] ?? "YTA4NGJiNzItNmY4ZC00MDcyLWEzYzgtYWEyM2MxMmRiNzUyMjA1MWFmZTM2OGYxYjE5NTNhY2ZjNTZjMDRmZTc5NDM=";
            var sender = _configuration["SmsSettings:Sender"] ?? "+98PRO";

            // Format phone
            phone = phone.Trim().Replace(" ", "").Replace("-", "");
            if (phone.StartsWith("+98")) phone = phone.Substring(3);
            else if (phone.StartsWith("98")) phone = phone.Substring(2);
            else if (phone.StartsWith("0")) phone = phone.Substring(1);

            var url = "https://edge.ippanel.com/v1/api/send";

            var jsonBody = $@"{{
                ""message"": ""کد تأیید: {code}"",
                ""from_number"": ""{sender}"",
                ""sending_type"": ""normal"",
                ""params"": {{
                    ""recipients"": [""{phone}""]
                }}
            }}";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);

            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            return Ok(new { 
                success = response.IsSuccessStatusCode, 
                statusCode = response.StatusCode,
                response = responseBody,
                phone = phone,
                jsonSent = jsonBody
            });
        }
    }
}
