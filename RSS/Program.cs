using RSS.Scrapers;
using StreamWriter = System.IO.StreamWriter;

namespace RSS;

public static class Program
{
    private static void ScrapeByName(string siteName, string username)
    {
        var siteNameToFunc = new Dictionary<string, Action>{
            {
                "picuki", () =>
                {
                    new Picuki(username).Scrape();
                    if (!Config.SitesAndUsernames["picuki_stories_blacklist"].Contains(username))
                    {
                        new PicukiStories(username).Scrape();
                    }
                }
            },{
                "nitter", () => new Nitter(username, !Config.SitesAndUsernames["nitter_replies_blacklist"].Contains(username)).Scrape()
            }
        };

        if (!siteNameToFunc.ContainsKey(siteName)) return;
        siteNameToFunc[siteName]();
    }

    private static void Main()
    {
        Config.LoadConfig();
        new Thread(_ => new Server()).Start();
        using StreamWriter writer = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), "data", "rss_urls.txt"));

        foreach (var item in Config.SitesAndUsernames)
        {
            new Thread(_ =>
            {
                foreach (var value in item.Value)
                {
                    ScrapeByName(item.Key, value);
                }
            }).Start();
            
            foreach (var value in item.Value)
            {
                writer.WriteLine($"{Config.Url}/{item.Key}/{value}/rss.xml");
            }
        }
    }
}
