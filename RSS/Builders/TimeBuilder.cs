using System.Globalization;
using System.Text.RegularExpressions;

namespace RSS.Builders;

public static class TimeBuilder
{
    public static string ParsePicukiTime(string time)
    {
        var regex = Regex.Match(time, @"(\d+)\s+(minute|hour|day|week|month)s?\s+ago");
        var baseCount = int.Parse(regex.Groups[1].ToString());
        var modifiers = new Dictionary<string, int>{
            {"minute", 1},
            {"hour", 60},
            {"day", 60 * 24},
            {"week", 60 * 24 * 7},
            {"month", 60 * 24 * 7 * 4}
        };
        var modifier = modifiers[regex.Groups[2].ToString().Replace("s", "")];
        var minutesToRemove = baseCount * modifier;
        var dateTime = DateTime.Now - TimeSpan.FromMinutes(minutesToRemove);
        
        // Thu, 27 Apr 2006
        return dateTime.ToString("ddd, dd MMM yyyy hh:mm:ss K", CultureInfo.InvariantCulture);
    }
}
