using System.Text.Json;

namespace Infrastructure.Utils;

public static class ListExtensions
{
    const char Separator = ',';
    
    public static string ToDelimitedString(this IEnumerable<string> list)
    {
        if (list == null || !list.Any())
            return string.Empty;
        return string.Join(Separator, list);
    }
    
    public static string ToDelimitedString(this IEnumerable<int> list)
    {
        if (list == null || !list.Any())
            return string.Empty;
        return string.Join(Separator, list);
    }
    
    public static List<string> FromDelimitedString(this string delimitedString)
    {
        if (string.IsNullOrEmpty(delimitedString))
            return new List<string>();
        return delimitedString.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .ToList();
    }

    public static List<int> FromDelimitedStringToInt(this string? delimitedString)
    {
        if (string.IsNullOrEmpty(delimitedString))
            return new List<int>();
        return delimitedString.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => int.TryParse(s.Trim(), out var result) ? result : 0)
                   .Where(i => i != 0)
                   .ToList();
    }
}

