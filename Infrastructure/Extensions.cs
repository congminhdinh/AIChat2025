using Infrastructure.Authentication;
using Infrastructure.OS;
using Infrastructure.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure
{
    public static class Extensions
    {

        public static void ConfigureAppSettings(this IHostBuilder host, string root = "Config")
        {
            host.ConfigureAppConfiguration((ctx, builder) =>
            {
                var enviroment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                //builder.SetBasePath("Config");
                builder.AddJsonFile($"{root}/appsettings.json", false, true);
                builder.AddJsonFile($"{root}/appsettings.{enviroment}.json", true, true);
                //builder.AddJsonFile($"appsettings.{Environment.MachineName}.json", true, true);

                builder.AddEnvironmentVariables();
            });
        }
        public static void AddInfrastructure(this WebApplicationBuilder builder)
        {
            builder.AddCustomOs();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            builder.Services.AddSingleton<ICurrentUserProvider, CurrentUserProvider>();
            //builder.AddRepositoryExtensions();
            builder.Services.AddHttpContextAccessor();
            builder.AddCustomAuthorization();
            builder.AddCustomOpenApi();
            builder.Services.AddAntiforgery();
        }

        public static void UseInfrastructure(this WebApplication app)
        {
            app.UseHttpsRedirection();
            app.UseCustomAuthentication();
            app.UseMiddleware<ExceptionMiddleware>();
            app.MapOpenApi();
            app.UseAntiforgery();
            app.UseStatusCodePages(async statusCodeContext =>
            {
                var message = $"Có lỗi xảy ra ({statusCodeContext.HttpContext.Response.StatusCode})";
                // statusCodeContext.HttpContext.Response.w
                switch (statusCodeContext.HttpContext.Response.StatusCode)
                {
                    case 401:
                        message = "Thông tin token không đúng";
                        break;
                        //case 403:
                        //    statusCodeContext.HttpContext.Response.StatusCode = 400;
                        //    break;
                }
                var model = new { code = (int)statusCodeContext.HttpContext.Response.StatusCode, message };
                statusCodeContext.HttpContext.Response.StatusCode = 200;
                await statusCodeContext.HttpContext.Response.WriteAsJsonAsync(model);
            });

        }
    }
}
