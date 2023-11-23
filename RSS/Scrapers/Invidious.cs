using RSS.Builders;
using System.Text.RegularExpressions;

namespace RSS.Scrapers;

public class Invidious : Website
{
    public void Scrape()
    {
        var doc = GetHTMLDocument($"{Config.InvidiousInstance}/feed/subscriptions", Cookies.Invidious!).DocumentNode;

        var count = doc.SelectNodes("//div[@class='pure-u-1 pure-u-md-1-4']")?.Count;
        if (count == null) return;

        foreach (var (postUrl, i) in doc.SelectNodes("//div[@class='thumbnail']/a").Select(x => Config.InvidiousInstance + x.GetAttributeValue("href", "")).WithIndex())
        {
            var id = Regex.Match(postUrl, @"\/watch\?v=([^*]+)").Groups[1].ToString();
            if (scrappedIds.Contains(id))
            {
                Console.WriteLine($"{sitename}: Post {i + 1}/{count} already scraped");
                continue;
            }

            if (Config.InvidiousFilterShorts && doc.SelectNodes("//div[@class='bottom-right-overlay']")[i].ChildNodes.Count == 1)
            {
                Console.WriteLine($"{sitename}: Post {i + 1}/{count} is a short, skipping");
                continue;
            }

            Console.WriteLine($"{sitename}: Scraping post {i + 1}/{count}");

            try
            {
                var item = new Item{
                    Title = doc.SelectNodes("//div[@class='video-card-row']/a/p")[i].InnerText.Trim(),
                    Link = postUrl,
                    Author = doc.SelectNodes("//p[@class='channel-name']")[i].InnerText.Trim(),
                    PubDate = TimeBuilder.DateTimeToPubDateFormat(DateTime.Now - TimeSpan.FromMinutes(i)),
                    GUID = id,
                    Description = new DescriptionBuilder(Media)
                        .AddParagraph($"<a href=\"{postUrl}\">New video from channel <b>{doc.SelectNodes("//p[@class='channel-name']")[i].InnerText.Trim()}</b></a>").ToString()
                };
                Rss.Channel.Items.Add(item);
            }
            catch {}
        }
    }

    public Invidious(string sitename, string title, string description, string link, string faviconUrl) : base(sitename, title, description, link, faviconUrl)
    {

    }
}
