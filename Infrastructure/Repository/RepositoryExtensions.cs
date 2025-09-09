using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Repository
{
    public static class RepositoryExtensions
    {
        public static void AddRepositoryExtensions(this WebApplicationBuilder builder)
        {
            builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
            builder.Services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
        }
    }
}
