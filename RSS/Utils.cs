using Notify;
using System.Xml;

namespace RSS;

public static class EnumExtension
{
    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
        => self.Select((item, index) => (item, index));
}

public static class Extensions
{
    public static void AddAttribute(this XmlDocument doc, string xpath, string atName, string value)
    {
        var attribute = doc.CreateAttribute(atName);
        attribute.Value = value;
        doc.SelectSingleNode(xpath)?.Attributes?.Append(attribute);
    }
}

public sealed class RSSException : Exception
{
    public RSSException(string msg) : base(msg)
    {
        try
        {
            new Notification("RSS WebScraper", msg).Show();
        }
        catch {}

        using StreamWriter writer = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), "crash_report.txt"));
        writer.WriteLine(msg);
        writer.WriteLine(InnerException);
        writer.WriteLine(StackTrace);
    }
}
