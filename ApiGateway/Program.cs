using Infrastructure;
using System.Text.Json.Nodes;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppSettings();
// 1. Add YARP services and load config from appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// 2. Add HttpClient for fetching downstream swagger docs
builder.Services.AddHttpClient();

// 3. Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "API Gateway", Version = "v1" });
});

var app = builder.Build();

// 4. Configure Swagger UI to display endpoints for each service
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Discover and add an endpoint for each downstream service from the YARP config
        var proxyConfig = app.Services.GetRequiredService<IProxyConfigProvider>().GetConfig();
        var clusters = proxyConfig.Clusters.ToList();

        // Find and add the "account" service first to make it the default
        var accountCluster = clusters.FirstOrDefault(c => c.ClusterId.Equals("account-cluster", StringComparison.OrdinalIgnoreCase));
        if (accountCluster != null)
        {
            options.SwaggerEndpoint($"/swagger/service/{accountCluster.ClusterId}", "account");
        }

        // Add the rest of the services, skipping the default one
        foreach (var cluster in clusters)
        {
            if (cluster.ClusterId.Equals("account-cluster", StringComparison.OrdinalIgnoreCase))
            {
                continue; // Already added as the default
            }

            if (cluster.Metadata?.TryGetValue("SwaggerUiName", out _) ?? false)
            {
                // Derive the doc name from the ClusterId (e.g., "tenant-cluster" -> "tenant")
                var docName = cluster.ClusterId.Replace("-cluster", "", StringComparison.OrdinalIgnoreCase);
                options.SwaggerEndpoint($"/swagger/service/{cluster.ClusterId}", docName);
            }
        }
    });
}


// 5. Map the YARP proxy endpoints
app.MapReverseProxy();

// 6. Add a custom endpoint to fetch, transform, and serve the downstream swagger.json
app.MapGet("/swagger/service/{clusterId}", async (
    string clusterId,
    HttpContext context,
    IProxyConfigProvider proxyConfigProvider,
    IHttpClientFactory httpClientFactory,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("SwaggerProxy");
    var proxyConfig = proxyConfigProvider.GetConfig();
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

    // Get the destination address and the route prefix
    var destination = cluster.Destinations!.First().Value;
    var route = proxyConfig.Routes.FirstOrDefault(r => r.ClusterId == clusterId);
    if (route == null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync($"Route not found for cluster '{clusterId}'.");
        return;
    }

    var downstreamUrl = $"{destination.Address!.TrimEnd('/')}{downstreamPath}";

    try
    {
        var httpClient = httpClientFactory.CreateClient();
        logger.LogInformation("Fetching swagger doc from: {DownstreamUrl}", downstreamUrl);
        var swaggerJsonString = await httpClient.GetStringAsync(downstreamUrl);

        // Use System.Text.Json to modify the document
        var swaggerNode = JsonNode.Parse(swaggerJsonString)!;

        // Clear any existing servers, as the gateway is the single point of entry
        if (swaggerNode["servers"] is JsonArray servers)
        {
            servers.Clear();
        }

        // Because your downstream services already include the full path prefix in their swagger docs,
        // we don't need to modify the paths here. We just clone them.
        if (swaggerNode["paths"] is JsonObject paths)
        {
            var newPaths = new JsonObject();
            foreach (var path in paths)
            {
                newPaths.Add($"{path.Key}", path.Value!.DeepClone());
            }
            swaggerNode["paths"] = newPaths;
        }

        context.Response.Headers.ContentType = new(new[] { "application/json" });
        await context.Response.WriteAsync(swaggerNode.ToJsonString());
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching or transforming swagger doc for {ClusterId} from {DownstreamUrl}", clusterId, downstreamUrl);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync($"Error fetching or transforming swagger doc for {clusterId}: {ex.Message}");
    }
});


app.Run();

