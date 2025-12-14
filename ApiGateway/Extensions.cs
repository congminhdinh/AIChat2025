using Infrastructure.Web;
using System.Text.Json.Nodes;

namespace ApiGateway
{
    public static class Extensions
    {
        const string API_BASE_PATH = $"/{EndpointConstants.WEB_API_BASE_ENDPOINT}";
        const string OPEN_API_BASE_PATH = $"openapi/{EndpointConstants.WEB_API_BASE_ENDPOINT}.json";
        public static void UseCustomSwagger(this WebApplication app, Dictionary<string, string> appServices)
        {
            var swaggerEndpointFormat = "swagger/v1/{0}.json";
            var proxyBasePath = "";
            if (!app.Environment.IsDevelopment())
            {
                proxyBasePath = $"{EndpointConstants.WEB_API_BASE_ENDPOINT}/";
                swaggerEndpointFormat = $"{EndpointConstants.WEB_API_BASE_ENDPOINT}/swagger/v1/{{0}}.json";
            }
            app.UseSwagger();
            app.UseSwaggerUI(options => // UseSwaggerUI is called only in Development.
            {
                options.DocumentTitle = "Api Reverse Proxy";
                options.ConfigObject.AdditionalItems.Add("persistAuthorization", "true");
                foreach (var appService in appServices)
                {
                    options.SwaggerEndpoint($"/{string.Format(swaggerEndpointFormat, appService.Key)}", appService.Key);
                }
            });
            foreach (var appServiceItem in appServices)
            {
                var appService = appServiceItem.Key;
                var appServiceUrl = appServiceItem.Value;
                if (string.IsNullOrEmpty(appServiceUrl))
                {
                    appServiceUrl = $"http://{appService}";
                }
                _ = app.MapGet(string.Format(swaggerEndpointFormat, appService), async (HttpContext context, HttpClient httpClient) =>
                {
                    var content = await httpClient.GetStringAsync($"{appServiceUrl}/{OPEN_API_BASE_PATH}");
                    var jsonNode = JsonNode.Parse(content);
                    if (jsonNode == null)
                    {
                        throw new Exception($"Failed to parse the {appService} OpenAPI document.");
                    }

                    if (jsonNode is JsonObject jsonObj)
                    {
                        // Cập nhật phần "servers"
                        var serversArray = new JsonArray();
                        var serverObj = new JsonObject
                        {
                            //["url"] = $"{context.Request.Scheme}://{context.Request.Host}"
                            ["url"] = $"https://{context.Request.Host}"
                        };
                        serversArray.Add(serverObj);
                        jsonObj["servers"] = serversArray;
                        // Nếu muốn thay đổi basePath cho các endpoint trong "paths"
                        if (jsonObj["paths"] is JsonObject pathsObj)
                        {
                            var newPathsObj = new JsonObject();
                            foreach (var kvp in pathsObj)
                            {
                                // Tạo key mới với tiền tố basePath
                                string newKey = kvp.Key.Replace($"{API_BASE_PATH}/", $"/{proxyBasePath}{appService}/");
                                newPathsObj[newKey] = kvp.Value?.DeepClone();
                            }
                            jsonObj["paths"] = newPathsObj;
                        }

                        var node = new List<JsonNode>();

                        var securityArray = JsonNode.Parse("[ {  \"bearerAuth\": [ ] } ]").AsArray();
                        jsonObj["security"] = securityArray;
                    }
                    return Results.Text(jsonNode.ToJsonString(), "application/json");
                });
            }
        }
    }
}
