using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Supabase;
using Postgrest.Attributes;
using Postgrest.Models;
using Telegram.Bot;

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
    }

    // ٢. مۆدێلی نوێکردنەوەی پڕۆگرێس لەلایەن ئەدمین
    public class UpdateProgressRequest
    {
        public string OrderId { get; set; }
        public string UserId { get; set; }
        public decimal DeductionAmount { get; set; }
        public int NewViews { get; set; }
        public string NewStatus { get; set; }
    }

    // ٣. مۆدێلی پڕۆفایل
    [Table("profiles")]
    public class ProfileModel : BaseModel
    {
        [PrimaryKey("id", false)] public string Id { get; set; }
        [Column("balance")] public decimal Balance { get; set; }
        [Column("telegram_chat_id")] public string TelegramChatId { get; set; }
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
    }

    [ApiController]
    [Route("api/[controller]")]
    public class SponsorController : ControllerBase
    {
        private readonly Supabase.Client _supabase;
        // لێرەدا تۆکنە ڕاستەقینەکەی خۆت دابنێ
        private readonly string _botToken = "8442452637:AAEZsajoeyN11wGipFnWqGZmm6XreIYqdRk"; 

        public SponsorController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        [HttpGet("test")]
        public IActionResult TestApi() => Ok(new { message = "باکئێند ئامادەیە!" });

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitSponsor([FromBody] SponsorRequest request)
        {
            try
            {
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
                    PostCode = request.PostCode,
                    Status = "pending"
                };
                
                await _supabase.From<OrderModel>().Insert(newOrder);
                return Ok(new { message = "داواکارییەکە تۆمارکرا ✅" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost("update-progress")]
        public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressRequest request)
        {
            try
            {
                var bot = new TelegramBotClient(_botToken);

                // بڕینی پارە
                var profResp = await _supabase.From<ProfileModel>().Where(x => x.Id == request.UserId).Get();
                var profile = profResp.Models.FirstOrDefault();
                if (profile == null) return BadRequest(new { message = "پڕۆفایل نەدۆزرایەوە." });

                profile.Balance -= request.DeductionAmount;
                await _supabase.From<ProfileModel>().Update(profile);

                // نوێکردنەوەی ئۆردەر
                var orderResp = await _supabase.From<OrderModel>().Where(x => x.Id == request.OrderId).Get();
                var order = orderResp.Models.FirstOrDefault();
                if (order == null) return BadRequest(new { message = "ئۆردەر نەدۆزرایەوە." });

                order.SpentAmount += request.DeductionAmount;
                order.Views += request.NewViews;
                order.Status = request.NewStatus;
                await _supabase.From<OrderModel>().Update(order);

                // ناردنی تێلیگرام
                if (!string.IsNullOrEmpty(profile.TelegramChatId))
                {
                    string statusEmoji = request.NewStatus == "done" ? "✅" : "📈";
                    string msg = $"{statusEmoji} *نوێکردنەوەی سپۆنسەر*\n\n" +
                                 $"📌 ڕیکلام: {order.AdName}\n" +
                                 $"💰 خەرجکرا: ${request.DeductionAmount}\n" +
                                 $"👀 ڤییو: {request.NewViews}\n" +
                                 $"💵 باڵانسی ماوە: ${profile.Balance}";

                    await bot.SendMessage(long.Parse(profile.TelegramChatId), msg, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                }

                return Ok(new { message = "داتاکان نوێکرانەوە و تێلیگرام نێردرا ✅" });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // ئەم میتۆدە بۆ پەیوەستکردنی بەکارهێنەرە بە بۆتەکە
        [HttpPost("telegram-webhook")]
        public async Task<IActionResult> TelegramWebhook([FromBody] dynamic update)
        {
            try
            {
                var bot = new TelegramBotClient(_botToken);
                var message = update.message;
                if (message == null) return Ok();

                string chatId = message.chat.id.ToString();
                string text = message.text?.ToString() ?? "";

                if (text.StartsWith("/start"))
                {
                    var parts = text.Split(' ');
                    if (parts.Length > 1)
                    {
                        string userId = parts[1];
                        var response = await _supabase.From<ProfileModel>().Where(x => x.Id == userId).Get();
                        var profile = response.Models.FirstOrDefault();
                        if (profile != null)
                        {
                            profile.TelegramChatId = chatId;
                            await _supabase.From<ProfileModel>().Update(profile);
                            await bot.SendMessage(long.Parse(chatId), "پیرۆزە! 🎉 هەژمارەکەت پەیوەست کرا.");
                        }
                    }
                }
            }
            catch { /* تەنها بۆ ئەوەی پڕۆژەکە نەوەستێت */ }
            return Ok();
        }
    }
}