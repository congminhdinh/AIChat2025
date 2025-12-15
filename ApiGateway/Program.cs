using Infrastructure;
using Microsoft.OpenApi.Models;
using System.Text.Json.Nodes;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppSettings();
// 1. Add YARP services and load config from appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// 2. Add HttpClient for fetching downstream swagger docs
builder.Services.AddHttpClient();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
// 3. Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "API Gateway", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.EnablePersistAuthorization();
    var proxyConfig = app.Services.GetRequiredService<IProxyConfigProvider>().GetConfig();
    var clusters = proxyConfig.Clusters.ToList();

    var accountCluster = clusters.FirstOrDefault(c => c.ClusterId.Equals("account-cluster", StringComparison.OrdinalIgnoreCase));
    if (accountCluster != null)
    {
        options.SwaggerEndpoint($"/swagger/service/{accountCluster.ClusterId}", "account");
    }
    foreach (var cluster in clusters)
    {
        if (cluster.ClusterId.Equals("account-cluster", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (cluster.Metadata?.TryGetValue("SwaggerUiName", out _) ?? false)
        {
            var docName = cluster.ClusterId.Replace("-cluster", "", StringComparison.OrdinalIgnoreCase);
            options.SwaggerEndpoint($"/swagger/service/{cluster.ClusterId}", docName);
        }
    }
});
app.UseCors("AllowAll");
app.MapReverseProxy();
app.MapGet("/swagger/service/{clusterId}", async (
    string clusterId,
    HttpContext context,
    IProxyConfigProvider proxyConfigProvider,
    IHttpClientFactory httpClientFactory,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("SwaggerProxy");
    var proxyConfig = proxyConfigProvider.GetConfig();

    // 1. Validation
    if (!proxyConfig.Clusters.Any(c => c.ClusterId == clusterId))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var cluster = proxyConfig.Clusters.First(c => c.ClusterId == clusterId);
    if (!(cluster.Metadata?.TryGetValue("SwaggerDownstreamPath", out var downstreamPath) ?? false))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync($"'SwaggerDownstreamPath' metadata not found for cluster '{clusterId}'.");
        return;
    }

    var destination = cluster.Destinations!.First().Value;
    var downstreamUrl = $"{destination.Address!.TrimEnd('/')}{downstreamPath}";
    try
    {
        var httpClient = httpClientFactory.CreateClient();
        logger.LogInformation("Fetching swagger doc from: {DownstreamUrl}", downstreamUrl);
        var swaggerJsonString = await httpClient.GetStringAsync(downstreamUrl);

        var swaggerNode = JsonNode.Parse(swaggerJsonString)!;

        if (swaggerNode is JsonObject jsonObj)
        {
            if (jsonObj["servers"] is JsonArray servers)
            {
                servers.Clear();
                servers.Add(new JsonObject { ["url"] = $"https://{context.Request.Host}" });
            }
            else
            {
                var newServers = new JsonArray();
                newServers.Add(new JsonObject { ["url"] = $"https://{context.Request.Host}" });
                jsonObj["servers"] = newServers;
            }
            if (jsonObj["paths"] is JsonObject paths)
            {
                var newPaths = new JsonObject();
                var ignoredPaths = new[] { "login", "register", "allowanonymous", "public" };

                foreach (var path in paths)
                {
                    string originalKey = path.Key;
                    var pathItem = path.Value!.DeepClone();
                    bool isIgnored = ignoredPaths.Any(ignore => originalKey.Contains(ignore, StringComparison.OrdinalIgnoreCase));
                    if (!isIgnored && pathItem is JsonObject pathItemObj)
                    {
                        foreach (var operationEntry in pathItemObj)
                        {
                            var method = operationEntry.Key.ToLower();
                            if (new[] { "get", "post", "put", "delete", "patch" }.Contains(method))
                            {
                                if (operationEntry.Value is JsonObject operation)
                                {
                                    var securityArray = new JsonArray();
                                    var requirement = new JsonObject();
                                    requirement.Add("Bearer", new JsonArray());
                                    securityArray.Add(requirement);
                                    operation["security"] = securityArray;
                                }
                            }
                        }
                    }

                    newPaths.Add(originalKey, pathItem);
                }
                jsonObj["paths"] = newPaths;
            }
            var components = jsonObj["components"] as JsonObject ?? new JsonObject();
            jsonObj["components"] = components;

            var securitySchemes = components["securitySchemes"] as JsonObject ?? new JsonObject();
            components["securitySchemes"] = securitySchemes;

            // Ensure "Bearer" scheme is defined so the button works
            if (!securitySchemes.ContainsKey("Bearer"))
            {
                securitySchemes.Add("Bearer", new JsonObject
                {
                    ["type"] = "http",
                    ["scheme"] = "bearer",
                    ["bearerFormat"] = "JWT",
                    ["description"] = "JWT Authorization header using the Bearer scheme."
                });
            }
        }

        context.Response.Headers.ContentType = new(new[] { "application/json" });
        await context.Response.WriteAsync(swaggerNode.ToJsonString());
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching or transforming swagger doc for {ClusterId}", clusterId);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync($"Error: {ex.Message}");
    }
});
app.Run();

