using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Enrichers.Span;

namespace Infrastructure.Logging
{
    public static class LoggingExtensions
    {
        public static void AddCustomLogging(this WebApplicationBuilder builder)
        {

            builder.Host.UseSerilog((context, loggerConfiguration) =>
            {

                loggerConfiguration
                    .Enrich.WithSpan()
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("ServiceName", AppDomain.CurrentDomain.FriendlyName)
                    .ReadFrom.Configuration(context.Configuration);
            });
            builder.Services.AddSingleton(typeof(IAppLogger<>), typeof(LoggerAdapter<>));
        }
    }
}
