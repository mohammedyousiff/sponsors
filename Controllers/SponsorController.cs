using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Text.Json; // بۆ چارەسەرکردنی کێشەی JsonElement زیادکراوە
using Supabase;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SponsorSaaS.Api.Controllers
{
    // --- مۆدێلەکان (Models) ---

    public class SponsorRequest
    {
        public string UserId { get; set; }
        public string AdName { get; set; }
        public decimal DailyBudget { get; set; }
        public int DurationDays { get; set; }
        public string TiktokUrl { get; set; }
        public string PostCode { get; set; }
        public string Objective { get; set; }
        public string TargetAge { get; set; }
        public string TargetGender { get; set; }
        public string TargetLocation { get; set; }
        public string StartDate { get; set; } 
        public string StartTime { get; set; } 
    }

    public class UpdateProgressRequest
    {
        public string OrderId { get; set; }
        public string UserId { get; set; }
        public decimal DeductionAmount { get; set; }
        public int NewViews { get; set; }
        public string NewStatus { get; set; }
        public string RejectReason { get; set; }
    }

    [Table("profiles")]
    public class ProfileModel : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; }
        [Column("balance")] public decimal Balance { get; set; }
    }

    [Table("orders")]
    public class OrderModel : BaseModel
    {
        [PrimaryKey("id", false)] public long Id { get; set; }
        [Column("user_id")] public string UserId { get; set; }
        [Column("ad_name")] public string AdName { get; set; }
        [Column("spent_amount")] public decimal SpentAmount { get; set; }
        [Column("views")] public int Views { get; set; }
        [Column("total_price")] public decimal TotalPrice { get; set; }
        [Column("status")] public string Status { get; set; }
        [Column("objective")] public string Objective { get; set; }
        [Column("target_age")] public string TargetAge { get; set; }
        [Column("daily_budget")] public decimal DailyBudget { get; set; }
        [Column("duration_days")] public int DurationDays { get; set; }
        [Column("tiktok_video_url")] public string TiktokUrl { get; set; }
        [Column("tiktok_post_code")] public string PostCode { get; set; }
        [Column("target_location")] public string TargetLocation { get; set; }
        [Column("start_date")] public string StartDate { get; set; }
        [Column("start_time")] public string StartTime { get; set; }
        [Column("video_id")] public string VideoId { get; set; }
    }

    [Table("notifications")]
    public class NotificationModel : BaseModel
    {
        [Column("user_id")] public string UserId { get; set; }
        [Column("title")] public string Title { get; set; }
        [Column("message")] public string Message { get; set; }
        [Column("is_read")] public bool IsRead { get; set; }
    }

    [Table("admin_settings")]
    public class AdminSettingsModel : BaseModel
    {
        [PrimaryKey("id", false)] public int Id { get; set; }
        [Column("access_token")] public string AccessToken { get; set; }
        [Column("refresh_token")] public string RefreshToken { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    }

    // --- کۆنتڕۆڵەر (Controller) ---

    [ApiController]
    [Route("api/[controller]")]
    public class SponsorController : ControllerBase
    {
        private readonly Supabase.Client _supabase;
        private readonly IHttpClientFactory _httpClientFactory;

        public SponsorController(Supabase.Client supabase, IHttpClientFactory httpClientFactory)
        {
            _supabase = supabase;
            _httpClientFactory = httpClientFactory;
        }

        private string ExtractVideoId(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            var match = Regex.Match(url, @"/video/(\d+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        // ١. فەنکشنی نوێکردنەوەی ئۆتۆماتیکی کلیل (Refresh Token Logic)
        private async Task<string> GetValidAccessToken()
        {
            var response = await _supabase.From<AdminSettingsModel>().Where(x => x.Id == 1).Get();
            var settings = response.Models.FirstOrDefault();

            if (settings == null) return null;

            // ئەگەر کلیلەکە زیاتر لە ٢٠ سەعاتی بەسەردا ڕۆشتبوو، نوێی دەکەینەوە
            if ((DateTime.UtcNow - settings.UpdatedAt).TotalHours > 20)
            {
                var client = _httpClientFactory.CreateClient();
                var refreshData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_key", "aw30bjij28hamzh6"),
                    new KeyValuePair<string, string>("client_secret", "pGC0XSpiHT5xakcRZo1TlqYnECFjGm07"),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", settings.RefreshToken)
                });

                var tiktokResponse = await client.PostAsync("https://open.tiktokapis.com/v2/auth/token/refresh/", refreshData);
                if (tiktokResponse.IsSuccessStatusCode)
                {
                    var json = await tiktokResponse.Content.ReadFromJsonAsync<JsonElement>();
                    settings.AccessToken = json.GetProperty("access_token").GetString();
                    settings.RefreshToken = json.GetProperty("refresh_token").GetString();
                    settings.UpdatedAt = DateTime.UtcNow;
                    
                    await _supabase.From<AdminSettingsModel>().Update(settings);
                }
            }
            return settings.AccessToken;
        }

        [HttpGet("test")]
        public IActionResult Test() => Ok(new { message = "باکئێند و سیستەمی سپۆنسەر ئامادەیە!" });

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitSponsor([FromBody] SponsorRequest request)
        {
            try
            {
                var videoId = ExtractVideoId(request.TiktokUrl);
                if (string.IsNullOrEmpty(videoId)) return BadRequest(new { message = "لینکی تیکتۆک ناتەواوە." });

                decimal totalPrice = request.DailyBudget * request.DurationDays;
                var profResp = await _supabase.From<ProfileModel>().Where(x => x.Id == request.UserId).Get();
                var profile = profResp.Models.FirstOrDefault();

                if (profile == null || profile.Balance < totalPrice) return BadRequest(new { message = "باڵانست بەش ناکات." });

                var newOrder = new OrderModel
                {
                    UserId = request.UserId,
                    AdName = request.AdName,
                    Objective = request.Objective,
                    TargetAge = request.TargetAge,
                    DailyBudget = request.DailyBudget,
                    DurationDays = request.DurationDays,
                    TotalPrice = totalPrice,
                    TiktokUrl = request.TiktokUrl,
                    VideoId = videoId,
                    PostCode = request.PostCode,
                    Status = "pending",
                    TargetLocation = request.TargetLocation,
                    StartDate = request.StartDate,
                    StartTime = request.StartTime
                };
                await _supabase.From<OrderModel>().Insert(newOrder);
                return Ok(new { message = "نێردرا بۆ ئەدمین ✅" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ٢. فەنکشنی نوێکردنەوەی ڤییووەکان (Sync Views)
        [HttpGet("sync-views/{orderId}")]
        public async Task<IActionResult> SyncViews(long orderId)
        {
            try
            {
                var orderResp = await _supabase.From<OrderModel>().Where(x => x.Id == orderId).Get();
                var order = orderResp.Models.FirstOrDefault();
                if (order == null || string.IsNullOrEmpty(order.VideoId)) return BadRequest("ئۆردەرەکە کێشەی هەیە.");

                string token = await GetValidAccessToken();
                if (string.IsNullOrEmpty(token)) return StatusCode(500, "کێشە لە کلیلی ئەدمین هەیە.");

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var body = new { filters = new { video_ids = new[] { order.VideoId } } };
                var response = await client.PostAsJsonAsync("https://open.tiktokapis.com/v2/video/query/?fields=view_count", body);
                
                if (response.IsSuccessStatusCode)
                {
                    var root = await response.Content.ReadFromJsonAsync<JsonElement>();
                    
                    // لێرەدا بە وردی دەچینە ناو داتاکانی تیکتۆک بۆ دۆزینەوەی ڤییووەکان بەبێ بەکارهێنانی dynamic
                    if (root.TryGetProperty("data", out var data) && 
                        data.TryGetProperty("videos", out var videos) && 
                        videos.GetArrayLength() > 0)
                    {
                        int currentViews = videos[0].GetProperty("view_count").GetInt32();
                        order.Views = currentViews;
                        await _supabase.From<OrderModel>().Update(order);
                        return Ok(new { views = currentViews, message = "پیرۆزە! ڤییووەکان نوێکرانەوە." });
                    }
                    return BadRequest("ڤیدیۆکە لە تیکتۆک نەدۆزرایەوە.");
                }
                return BadRequest("تیکتۆک وەڵامی نەدایەوە.");
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("update-progress")]
        public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressRequest request)
        {
            try
            {
                var orderIdLong = long.Parse(request.OrderId); // گۆڕین بۆ ژمارە چونکە Id لە داتابەیس ژمارەیە
                var orderResp = await _supabase.From<OrderModel>().Where(x => x.Id == orderIdLong).Get();
                var order = orderResp.Models.FirstOrDefault();
                if (order == null) return BadRequest(new { message = "ئۆردەر نەدۆزرایەوە." });

                if (request.NewStatus != "rejected" && request.DeductionAmount > 0)
                {
                    var profResp = await _supabase.From<ProfileModel>().Where(x => x.Id == request.UserId).Get();
                    var profile = profResp.Models.FirstOrDefault();
                    if (profile != null)
                    {
                        profile.Balance -= request.DeductionAmount;
                        await _supabase.From<ProfileModel>().Update(profile);
                    }
                    order.SpentAmount += request.DeductionAmount;
                    order.Views += request.NewViews;
                }

                order.Status = request.NewStatus;
                await _supabase.From<OrderModel>().Update(order);
                return Ok(new { message = "پڕۆسەکە نوێکرایەوە ✅" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
