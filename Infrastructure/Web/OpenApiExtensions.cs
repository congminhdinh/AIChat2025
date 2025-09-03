using Infrastructure.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Reflection;
namespace Infrastructure.Web;


public static class OpenApiExtensions
{
    public static void AddCustomOpenApi(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOpenApi(EndpointConstants.ADMIN_API_BASE_ENDPOINT, options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();

            options.ShouldInclude = (description) =>
            {
                return description.RelativePath != null && description.RelativePath.StartsWith(EndpointConstants.ADMIN_API_BASE_ENDPOINT);

            };
        });
        builder.Services.AddOpenApi(EndpointConstants.WEB_API_BASE_ENDPOINT, options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.ShouldInclude = (description) =>
            {
                return description.RelativePath != null && description.RelativePath.StartsWith(EndpointConstants.WEB_API_BASE_ENDPOINT);
            };
        });
    }

    internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
            if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
            {
                var requirements = new Dictionary<string, OpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        In = ParameterLocation.Header,
                        BearerFormat = "JWT",
                        Description = "JWT Bearer token authorization"
                    }
                };
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = requirements;

                // Áp dụng security requirement cho tất cả các operation
                foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
                {
                    operation.Value.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme } }] = Array.Empty<string>()
                    });
                }
            }
        }
    }

}
