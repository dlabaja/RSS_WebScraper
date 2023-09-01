using RSS.Scrapers;
using StreamWriter = System.IO.StreamWriter;

namespace RSS;

public static class Program
{
    private static void ScrapeByName(string siteName, string username)
    {
        var siteNameToFunc = new Dictionary<string, Action>{
            {"picuki", () => new Picuki(username).Scrape()},
            {"nitter", () => new Nitter(username).Scrape()}
        };

        try
        {
            Console.WriteLine($"----\nScraping {siteName}/{username}");
            siteNameToFunc[siteName]();
        }
        catch (KeyNotFoundException _)
        {
            throw new Exception($"Invalid site name ({siteName})");
        }
    }

    private static void Main()
    {
        Config.LoadConfig();
        new Thread(o => new Server()).Start();
        using StreamWriter writer = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), "data", "rss_urls.txt"));

        foreach (var item in Config.SitesAndUsernames)
        {
            foreach (var value in item.Value)
            {
                writer.WriteLine($"{Config.Url}/{item.Key}/{value}/rss.xml");
                ScrapeByName(item.Key, value);
            }
        }
    }
}
