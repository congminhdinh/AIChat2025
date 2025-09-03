using System.Text.RegularExpressions;

namespace Infrastructure.Utils
{
    public class StringHelper
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
    }
}
