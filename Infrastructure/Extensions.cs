using Infrastructure.Authentication;
using Infrastructure.Logging;
using Infrastructure.OS;
using Infrastructure.Tenancy;
using Infrastructure.Web;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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
            builder.Services.Configure<AppSettings>(builder.Configuration);
            builder.AddCustomOs();
            builder.Services.AddHttpContextAccessor();
            builder.AddCustomLogging();
            builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            builder.Services.AddSingleton<ICurrentUserProvider, CurrentUserProvider>();
            builder.Services.AddScoped<ICurrentTenantProvider>(sp =>
                new CurrentTenantProvider(sp.GetRequiredService<ICurrentUserProvider>()));
            builder.Services.AddHttpContextAccessor();
            builder.AddCustomAuthorization();
            builder.AddCustomOpenApi();
            builder.Services.AddAntiforgery();

        }

        public static void UseInfrastructure(this WebApplication app)
        {
            app.UseCustomAuthentication();
            app.UseMiddleware<ExceptionMiddleware>();
            app.MapOpenApi();
            app.UseAntiforgery();
            app.UseStatusCodePages(async statusCodeContext =>
            {
                var message = $"Có lỗi xảy ra ({statusCodeContext.HttpContext.Response.StatusCode})";
                switch (statusCodeContext.HttpContext.Response.StatusCode)
                {
                    case 401:
                        message = "Thông tin token không đúng";
                        break;
                }
                var model = new { code = (int)statusCodeContext.HttpContext.Response.StatusCode, message };
                statusCodeContext.HttpContext.Response.StatusCode = 200;
                await statusCodeContext.HttpContext.Response.WriteAsJsonAsync(model);
            });

        }

        public static void AddMassTransitWithRabbitMq(this WebApplicationBuilder builder)
        {
            builder.Services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    var appSettings = context.GetRequiredService<IOptionsMonitor<AppSettings>>().CurrentValue;
                    var rabbitMqEndpoint = appSettings.RabbitMQEndpoint ?? "localhost:5672";
                    var username = appSettings.RabbitMQUsername ?? "guest";
                    var password = appSettings.RabbitMQPassword ?? "guest";

                    cfg.Host($"rabbitmq://{rabbitMqEndpoint}", h =>
                    {
                        h.Username(username);
                        h.Password(password);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });
        }

        public static void AddCustomSignalR(this WebApplicationBuilder builder)
        {
            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = builder.Environment.IsDevelopment();
                options.MaximumReceiveMessageSize = 102400; // 100 KB
                options.StreamBufferCapacity = 10;
            });
        }
    }
}
