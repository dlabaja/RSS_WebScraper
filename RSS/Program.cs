using RSS.Scrapers;
using System.Reflection;

namespace RSS;

public static class Program
{
    private static void ScrapeByName(string siteName, string username)
    {
        var siteNameToFunc = new Dictionary<string, Action>{
            {"picuki", () => new Picuki(username).Scrape()},
            {"nitter", () => new Picuki(username).Scrape()}
        };

        try
        {
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
        new Thread(o => new Server());

        foreach (var item in Config.SitesAndUsernames)
        {
            foreach (var value in item.Value)
            {
                ScrapeByName(item.Key, value);
            }
        }
    }
}
