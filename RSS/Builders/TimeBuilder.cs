using System.Globalization;
using System.Text.RegularExpressions;

namespace RSS.Builders;

public static class TimeBuilder
{
    public static string? ParsePicukiTime(string time)
    {
        var regex = Regex.Match(time, @"(\d+)\s+(minute|hour|day|week|month|year)s?\s+ago");
        if (!regex.Success)
            return null;

        var baseCount = int.Parse(regex.Groups[1].ToString());
        var modifiers = new Dictionary<string, int>{
            {"minute", 1},
            {"hour", 60},
            {"day", 60 * 24},
            {"week", 60 * 24 * 7},
            {"month", 60 * 24 * 7 * 4},
            {"year", 60 * 24 * 7 * 4 * 12}
        };
        var modifier = modifiers[regex.Groups[2].ToString().Replace("s", "")];
        var minutesToRemove = baseCount * modifier;

        return DateTimeToPubDateFormat(DateTime.Now - TimeSpan.FromMinutes(minutesToRemove));
    }

    public static string DateTimeToPubDateFormat(DateTime dateTime)
    {
        return dateTime.ToString("ddd, dd MMM yyyy HH:mm:ss K", CultureInfo.InvariantCulture);
    }

    public static string ParseNitterTime(string time)
    {
        time = time.Replace("·", "");
        time = time.Replace("UTC", "");

        var dateTime = DateTime.Parse(time);
        return dateTime.ToString("ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture);
    }
}
