using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions; // بۆ جیاکردنەوەی ئایدی ڤیدیۆکە پێویستە
using Supabase;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SponsorSaaS.Api.Controllers
{
    // ١. مۆدێلی وەرگرتنی سپۆنسەری نوێ
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

    // ٢. مۆدێلی نوێکردنەوەی پڕۆگرێس لەلایەن ئەدمین
    public class UpdateProgressRequest
    {
        public string OrderId { get; set; }
        public string UserId { get; set; }
        public decimal DeductionAmount { get; set; }
        public int NewViews { get; set; }
        public string NewStatus { get; set; }
        public string RejectReason { get; set; }
    }

    // ٣. مۆدێلی پڕۆفایل
    [Table("profiles")]
    public class ProfileModel : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; }
        [Column("balance")] public decimal Balance { get; set; }
    }

    // ٤. مۆدێلی ئۆردەر
    [Table("orders")]
    public class OrderModel : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; }
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
        [Column("video_id")] public string VideoId { get; set; } // ئایدی تیکتۆک لێرە سەیڤ دەبێت
    }

    // ٥. مۆدێلی ئاگادارکردنەوەکان
    [Table("notifications")]
    public class NotificationModel : BaseModel
    {
        [Column("user_id")] public string UserId { get; set; }
        [Column("title")] public string Title { get; set; }
        [Column("message")] public string Message { get; set; }
        [Column("is_read")] public bool IsRead { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class SponsorController : ControllerBase
    {
        private readonly Supabase.Client _supabase;

        public SponsorController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        // *** فەنکشنی یاریدەدەر بۆ جیاکردنەوەی ئایدی ڤیدیۆ لە لینکەکە ***
        private string ExtractVideoId(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            var match = Regex.Match(url, @"/video/(\d+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        [HttpGet("ping")]
        public IActionResult Ping() => Ok("سێرڤەرەکە بە تەندروستی کار دەکات.");

        [HttpGet("test")]
        public IActionResult TestApi() => Ok(new { message = "باکئێند ئامادەیە!" });

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitSponsor([FromBody] SponsorRequest request)
        {
            try
            {
                // ١. جیاکردنەوەی ئایدی ڤیدیۆکە پێش هەموو شتێک
                var videoId = ExtractVideoId(request.TiktokUrl);
                if (string.IsNullOrEmpty(videoId)) 
                    return BadRequest(new { message = "تکایە لینکێکی ڕاستی تیکتۆک بنێرە (دەبێت ئایدی ڤیدیۆکەی تێدابێت)." });

                decimal totalPrice = request.DailyBudget * request.DurationDays;
                var response = await _supabase.From<ProfileModel>().Where(x => x.Id == request.UserId).Get();
                var profile = response.Models.FirstOrDefault();

                if (profile == null) return BadRequest(new { message = "بەکارهێنەر نەدۆزرایەوە." });
                if (profile.Balance < totalPrice) return BadRequest(new { message = "باڵانست پێویست نییە!" });

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
                    VideoId = videoId, // ئایدییە جیاکراوەکە لێرە دادەنرێت
                    PostCode = request.PostCode,
                    Status = "pending",
                    TargetLocation = request.TargetLocation,
                    StartDate = request.StartDate,
                    StartTime = request.StartTime
                };
                
                await _supabase.From<OrderModel>().Insert(newOrder);

                var newNotif = new NotificationModel
                {
                    UserId = request.UserId,
                    Title = "داواکارییەکەت گەیشت ⏳",
                    Message = $"داواکاری سپۆنسەر بۆ ڕیکلامی ({request.AdName}) بە سەرکەوتوویی نێردرا، تکایە چاوەڕێی قبوڵکردن بە.",
                    IsRead = false
                };
                await _supabase.From<NotificationModel>().Insert(newNotif);

                return Ok(new { message = "داواکارییەکە تۆمارکرا ✅", video_id = videoId });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("update-progress")]
        public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressRequest request)
        {
            try
            {
                var orderResp = await _supabase.From<OrderModel>().Where(x => x.Id == request.OrderId).Get();
                var order = orderResp.Models.FirstOrDefault();
                if (order == null) return BadRequest(new { message = "ئۆردەر نەدۆزرایەوە." });

                if (request.NewStatus != "rejected" && request.DeductionAmount > 0)
                {
                    var profResp = await _supabase.From<ProfileModel>().Where(x => x.Id == request.UserId).Get();
                    var profile = profResp.Models.FirstOrDefault();
                    if (profile == null) return BadRequest(new { message = "پڕۆفایل نەدۆزرایەوە." });

                    profile.Balance -= request.DeductionAmount;
                    await _supabase.From<ProfileModel>().Update(profile);

                    order.SpentAmount += request.DeductionAmount;
                    order.Views += request.NewViews;
                }

                order.Status = request.NewStatus;
                await _supabase.From<OrderModel>().Update(order);

                string notifTitle = "";
                string notifMessage = "";

                if (request.NewStatus == "done")
                {
                    notifTitle = "سپۆنسەرەکەت تەواو بوو 🎉";
                    notifMessage = $"کامپەینی ({order.AdName}) بە سەرکەوتوویی کۆتایی هات! بڕی ${request.DeductionAmount} خەرجکرا و کۆی ڤییوو گەیشتە {order.Views:N0}.";
                }
                else if (request.NewStatus == "rejected")
                {
                    notifTitle = "ڕیکلامەکەت ڕەتکرایەوە ❌";
                    string reason = !string.IsNullOrEmpty(request.RejectReason) ? request.RejectReason : "هۆکار دیارینەکراوە";
                    notifMessage = $"بەداخەوە ڕیکلامی ({order.AdName}) ڕەتکرایەوە. هۆکار: {reason}.";
                }
                else if (request.NewStatus == "approved")
                {
                    notifTitle = (request.DeductionAmount > 0) ? "ڕاپۆرتی نوێی ڕیکلام 📈" : "ڕیکلامەکەت قبوڵکرا ✅";
                    notifMessage = (request.DeductionAmount > 0) 
                        ? $"بەرەوپێشچوونی نوێ لە ({order.AdName}): ${request.DeductionAmount} خەرجکرا." 
                        : $"پیرۆزە! ڕیکلامی ({order.AdName}) پەسەندکرا.";
                }

                if (!string.IsNullOrEmpty(notifTitle))
                {
                    var newNotif = new NotificationModel { UserId = request.UserId, Title = notifTitle, Message = notifMessage, IsRead = false };
                    await _supabase.From<NotificationModel>().Insert(newNotif);
                }

                return Ok(new { message = "داتاکان نوێکرانەوە ✅" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
