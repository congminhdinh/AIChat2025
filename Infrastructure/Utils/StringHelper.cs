using Microsoft.AspNetCore.StaticFiles;
using System.Text.RegularExpressions;
using System.Web;

namespace Infrastructure.Utils
{
    public static class StringHelper
    {
        public static string GenerateSlug(string text)
        {
            // Convert to lower case
            text = text.ToLowerInvariant();

            // Remove invalid characters
            text = Regex.Replace(text, @"[^a-z0-9\s-]", "");

            // Replace multiple spaces with a single space
            text = Regex.Replace(text, @"\s+", " ").Trim();

            // Replace spaces with hyphens
            text = Regex.Replace(text, @"\s", "-");

            return text;
        }
        public static string GetQueryString(this object obj)
        {
            var properties = from p in obj.GetType().GetProperties()
                             where p.GetValue(obj, null) != null
                             select p.Name + "=" + HttpUtility.UrlEncode(p.GetValue(obj, null).ToString());

            return String.Join("&", properties.ToArray());
        }
        public static string GetMimeType(this string filePath)
        {
            var provider = new FileExtensionContentTypeProvider();
            string mimeType;
            if (!provider.TryGetContentType(filePath, out mimeType))
            {
                mimeType = "application/octet-stream"; // Default MIME type
            }
            return mimeType;
        }
    }
}
