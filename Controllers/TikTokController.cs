using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SponsorSaaS.Api.Controllers 
{
    [Route("api/tiktok")]
    [ApiController]
    public class TikTokController : ControllerBase
    {
        // ١. ئەمە ئەو بەشە نوێیەیە کە تیکتۆک هەڵدەخەڵەتێنێت و دۆمەینەکە ڤێریفای دەکات
        [HttpGet("/tiktokTAdOS0ZizW6VCDaZ2nIZeTO92oODuPll.txt")]
        public IActionResult VerifyDomain()
        {
            return Content("tiktok-developers-site-verification=TAdOS0ZizW6VCDaZ2nIZeTO92oODuPll", "text/plain");
        }

        // ٢. ئەمەش فەنکشنەکەی پێشووە بۆ گرتنی تۆکەنەکە
        [HttpGet("callback")]
        public async Task<IActionResult> Callback(string code)
        {
            if (string.IsNullOrEmpty(code)) return BadRequest("کۆدی تیکتۆک نەگەڕایەوە");

            var clientKey = "aw30bjij28hamzh6"; 
            var clientSecret = "pGC0XSpiHT5xakcRZo1TlqYnECFjGm07"; 
            var redirectUri = "https://sponsors-76gg.onrender.com/api/tiktok/callback"; 

            using var httpClient = new HttpClient();
            var data = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_key", clientKey),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("redirect_uri", redirectUri)
            });

            var response = await httpClient.PostAsync("https://open.tiktokapis.com/v2/oauth/token/", data);
            var result = await response.Content.ReadAsStringAsync();

            var htmlResponse = "<h2 style='color:green; text-align:center;'>پیرۆزە حەمە گیان!</h2>" +
                               "<p style='text-align:center;'>تکایە ئەم کۆدەی خوارەوە هەمووی کۆپی بکە و بۆمی بنێرە:</p>" +
                               $"<textarea style='width:100%; height:300px; direction:ltr;'>{result}</textarea>";

            return Content(htmlResponse, "text/html; charset=utf-8");
        }
    }
}
