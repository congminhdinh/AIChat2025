using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Utils
{
    public static class UrlBuilderExtensions
    {
        public static string BuildUrl(this string baseUrl, string endpoint, IDictionary<string, string> queryParams = null)
        {
            var uriBuilder = new UriBuilder(baseUrl);
            uriBuilder.Path = endpoint;
            if (queryParams != null && queryParams.Any())
            {
                var query = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                uriBuilder.Query = query;
            }
            return uriBuilder.ToString();
        }
    }
}
