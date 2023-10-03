using Notify;

namespace RSS;

public static class EnumExtension {
    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)       
        => self.Select((item, index) => (item, index));
}

public sealed class RSSException : Exception
{
    public RSSException(string msg) : base(msg)
    {
        new Notification("RSS WebScraper", msg).Show();
        using StreamWriter writer = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), "data", "crash_report.txt"));
        writer.WriteLine(msg);
        writer.WriteLine(InnerException);
        writer.WriteLine(StackTrace);
    }
}
