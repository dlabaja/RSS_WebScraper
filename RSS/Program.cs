using RSS.Scrapers;
using System.Globalization;
using StreamWriter = System.IO.StreamWriter;
using Timer = System.Timers.Timer;

namespace RSS;

public class Program
{
    private readonly Picuki picuki;
    private readonly PicukiStories picukiStories;
    private readonly Nitter nitter;
    private readonly ProxiTok proxiTok;

    private Program()
    {
        Config.LoadConfig();

        picuki = new Picuki("picuki", "Picuki", "All photos in one place", "https://www.picuki.com", "https://www.picuki.com/p.svg");
        picukiStories = new PicukiStories("picuki_stories", "Picuki stories", "All stories in one place", "https://www.picuki.com", "https://www.picuki.com/p.svg");
        nitter = new Nitter("nitter", "Nitter", "All tweets in one place", Config.NitterInstance, $"{Config.NitterInstance}/logo.png?v=1");
        proxiTok = new ProxiTok("proxitok", "ProxiTok", "All tiktoks in one place", Config.ProxiTokInstance, $"{Config.ProxiTokInstance}/favicon-32x32.png");

        new Thread(_ => new Server()).Start();

        Rescrape();
        var timer = new Timer();
        timer.Interval = 1000 * 60 * Config.ScrapeTimer;
        timer.Elapsed += delegate { Rescrape(); };
        timer.AutoReset = true;
        timer.Start();
    }

    private void ScrapeBySitename(string sitename)
    {
        var siteNameToFunc = new Dictionary<string, Action>{
            {
                "picuki", () =>
                {
                    foreach (var username in Config.SitesAndUsernames[sitename])
                    {
                        picuki.Scrape(username);
                        if (!Config.SitesAndUsernames["picuki_stories_blacklist"].Contains(username))
                        {
                            picukiStories.Scrape(username);
                        }
                    }

                    picuki.Media.DownloadAllMedia();
                    picukiStories.Media.DownloadAllMedia();
                }
            },{
                "nitter", () =>
                {
                    foreach (var username in Config.SitesAndUsernames[sitename])
                    {
                        nitter.Scrape(username, !Config.SitesAndUsernames["nitter_replies_blacklist"].Contains(username));
                    }

                    nitter.Media.DownloadAllMedia();
                }
            },{
                "proxitok", async () =>
                {
                    foreach (var username in Config.SitesAndUsernames[sitename])
                    {
                        await proxiTok.Scrape(username);
                    }

                    proxiTok.Media.DownloadAllMedia();
                }
            }
        };

        if (!siteNameToFunc.ContainsKey(sitename)) return;
        siteNameToFunc[sitename]();
    }

    private static void Main() => new Program();

    private void Rescrape()
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

        using StreamWriter url_writer = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), "rss_urls.txt"));
        foreach (var site in sites)
        {
            // http://localhost:8000/nitter/rss.xml
            url_writer.WriteLine($"{Config.Url}/{site}/rss.xml");
        }
    }
}
