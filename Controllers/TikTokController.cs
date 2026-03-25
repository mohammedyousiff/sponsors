using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

[Route("api/tiktok")]
[ApiController]
public class TikTokController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public TikTokController(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string code)
    {
        if (string.IsNullOrEmpty(code)) return BadRequest("کۆدی تیکتۆک نەدۆزرایەوە");

        // وەرگرتنی کلیلەکان لە Environment Variables
        var clientKey = _configuration["TIKTOK_CLIENT_KEY"];
        var clientSecret = _configuration["TIKTOK_CLIENT_SECRET"];
        var redirectUri = "https://sponsors-76gg.onrender.com/api/tiktok/callback";

        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_key", clientKey),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", redirectUri)
        });

        var response = await _httpClient.PostAsync("https://open.tiktokapis.com/v2/oauth/token/", requestContent);
        var result = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            // لێرەدا دەبێت 'result' کە تۆکەنەکەی تێدایە، خەزن بکەیت لە Supabase
            // دەتوانیت بۆ ئێستا تەنها ئاگادارییەک نیشان بدەیت
            return Content("<h1>سەرکەوتوو بوو!</h1><p>ئەکاونتی تیکتۆک بەسترایەوە. دەتوانیت ئەم لاپەڕەیە دابخەیت.</p>", "text/html; charset=utf-8");
        }

        return BadRequest($"هەڵە لە وەرگرتنی تۆکەن: {result}");
    }
}
