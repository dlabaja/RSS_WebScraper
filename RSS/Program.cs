using Notify;
using RSS.Scrapers;
using System.Globalization;
using System.Timers;
using StreamWriter = System.IO.StreamWriter;
using Timer = System.Timers.Timer;

namespace RSS;

public static class Program
{
    private static void ScrapeBySitename(string sitename)
    {
        var siteNameToFunc = new Dictionary<string, Action>{
            {
                "picuki", () =>
                {
                    foreach (var username in Config.SitesAndUsernames[sitename])
                    {
                        Picuki.Scrape(username);
                        if (!Config.SitesAndUsernames["picuki_stories_blacklist"].Contains(username))
                        {
                            PicukiStories.Scrape(username);
                        }

                        Picuki.Media.SaveJson();
                        PicukiStories.Media.SaveJson();

                        Website.SerializeXML<RSS>(Picuki.Rss, Picuki.SiteFolder, sitename);
                        Website.SerializeXML<RSS>(PicukiStories.Rss, PicukiStories.SiteFolder, sitename);
                    }

                    Picuki.Media.DownloadAllMedia();
                    PicukiStories.Media.DownloadAllMedia();
                }
            },{
                "nitter", () =>
                {
                    foreach (var username in Config.SitesAndUsernames[sitename])
                    {
                        Nitter.Scrape(username, !Config.SitesAndUsernames["nitter_replies_blacklist"].Contains(username));
                        Nitter.Media.SaveJson();
                        Website.SerializeXML<RSS>(Nitter.Rss, Nitter.SiteFolder, sitename);
                    }

                    Nitter.Media.DownloadAllMedia();
                }
            },{
                "proxitok", async () =>
                {
                    foreach (var username in Config.SitesAndUsernames[sitename])
                    {
                        await ProxiTok.Scrape(username);
                        ProxiTok.Media.SaveJson();
                        Website.SerializeXML<RSS>(ProxiTok.Rss, ProxiTok.SiteFolder, sitename);
                    }

                    ProxiTok.Media.DownloadAllMedia();
                }
            }
        };

        if (!siteNameToFunc.ContainsKey(sitename)) return;
        siteNameToFunc[sitename]();
    }

    private static void Main()
    {
        Config.LoadConfig();
        new Thread(_ => new Server()).Start();

        Rescrape();
        var timer = new Timer();
        timer.Interval = 1000 * 60 * Config.ScrapeTimer;
        timer.Elapsed += delegate { Rescrape(); };
        timer.AutoReset = true;
        timer.Start();
    }

    private static void Rescrape()
    {
        Config.LoadConfig();
        foreach (var item in Config.SitesAndUsernames)
        {
            new Thread(_ =>
            {
                ScrapeBySitename(item.Key);
            }).Start();
        }

        WriteUrls();
        Console.WriteLine(DateTime.Now.ToString("HH:mm:ss dd/MM", CultureInfo.InvariantCulture));
    }

    private static void WriteUrls()
    {
        var sites = new List<string>{
            "nitter",
            "picuki",
            "picuki_stories",
            "proxitok"
        };

        using StreamWriter url_writer = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), "data", "rss_urls.txt"));
        foreach (var site in sites)
        {
            // http://localhost:8000/nitter/rss.xml
            url_writer.WriteLine($"{Config.Url}/{site}/rss.xml");
        }
    }
}
