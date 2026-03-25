using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection; // ئەمە پێویستە بۆ ناسینەوەی AddHttpClient

namespace SponsorSaaS.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // لێرەدا HttpClient زیاد دەکەین بۆ ئەوەی باکئێند بتوانێت بە بێ کێشە پەیوەندی بە سێرڤەرەکانی تیکتۆکەوە بکات
                    webBuilder.ConfigureServices(services => {
                        services.AddHttpClient();
                    });

                    webBuilder.UseStartup<Startup>();
                });
    }
}
