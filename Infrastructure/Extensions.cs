using Infrastructure.Authentication;
using Infrastructure.OS;
using Infrastructure.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class Extensions
    {
        public static void AddInfrastructure(this WebApplicationBuilder builder)
        {
            builder.AddCustomOs();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            builder.Services.AddSingleton<ICurrentUserProvider, CurrentUserProvider>();
            builder.Services.AddHttpContextAccessor();
            builder.AddCustomAuthorization();
        }
    }
}
