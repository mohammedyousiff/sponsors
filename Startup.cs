using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Supabase;

namespace SponsorSaaS.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // هێنانی زانیارییەکانی سوبابەیس
            var url = Configuration["Supabase:Url"];
            var key = Configuration["Supabase:Key"];
            var options = new SupabaseOptions { AutoConnectRealtime = true };

            // ناساندنی سوبابەیس بە سیستەمەکە
            services.AddSingleton(provider => new Supabase.Client(url, key, options));

            // کردنەوەی CORS بۆ ئەوەی جاڤاسکریپتەکەمان بتوانێت قسەی لەگەڵ بکات
            services.AddCors(options => {
                options.AddPolicy("AllowAll", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors("AllowAll");
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
