using RSS.Builders;
using System.Text.RegularExpressions;
using static RSS.Scrapers.Website;

namespace RSS.Scrapers;

public static class Picuki
{
    public static Media Media { get; }
    public static RSS Rss { get; }
    public static string SiteFolder { get; }
    static string relativeMediaFolder;
    static string sitename;

    private static RSS GetRSS()
    {
        if (File.Exists(Path.Combine(SiteFolder, "rss.xml")))
            return DeserializeXML(sitename);

        return new RSS{
            Channel = new Channel{
                Title = "Picuki",
                Link = "https://www.picuki.com",
                Items = new List<Item>(),
                Description = "All photos in one place",
                Image = new Image{
                    Url = AddFavicon(Media, "https://www.picuki.com/p.svg", relativeMediaFolder),
                    Title = "Picuki",
                    Link = "https://www.picuki.com"
                }
            }
        };
    }

    public static void Scrape(string username)
    {
        var doc = GetHTMLDocument($"https://www.picuki.com/profile/{username}").DocumentNode;
        if (doc.InnerHtml.Contains("<title>Error 403</title>")) return; // 403 error, skip profile

        Media.Add(username, doc.SelectSingleNode("//img[@class='profile-avatar-image']")?.GetAttributeValue("src", "") ?? string.Empty);

        var count = doc.SelectNodes("//div[@class='photo']/a")?.Count;
        if (count == null) return;

        foreach (var (postUrl, i) in doc.SelectNodes("//div[@class='photo']/a").Select(x => x.GetAttributeValue("href", "")).WithIndex())
        {
            var id = Regex.Match(postUrl, @"\/media\/(\d+)").Groups[1].ToString();
            if (Rss.Channel.Items.Select(x => x.GUID).Contains(id))
            {
                Console.WriteLine($"{sitename}/{username}: Post {i + 1}/{count} already scraped");
                continue;
            }

            var post = GetHTMLDocument(postUrl).DocumentNode;

            Console.WriteLine($"{sitename}/{username}: Scraping post {i + 1}/{count}");

            try
            {
                var item = new Item{
                    Title = doc.SelectNodes("//div[@class='photo-description']")[i].InnerText.Trim(),
                    Link = postUrl,
                    Author = username,
                    PubDate = TimeBuilder.ParsePicukiTime(post.SelectSingleNode("//div[@class='single-photo-time']").InnerText),
                    GUID = id,
                    Description = new DescriptionBuilder(Media)
                        .AddSpanOrEmpty($"Location: {post.SelectSingleNode("//div[@class='location']/text()[normalize-space()]")?.InnerText.Trim()}",
                            !string.IsNullOrEmpty(post.SelectSingleNode("//div[@class='location']/text()[normalize-space()]")?.InnerText.Trim())) // location
                        .AddSpan($"💬 {post.SelectSingleNode("//span[@id='commentsCount']").InnerText}") // commentsCount;
                        .AddSpan($"❤️ {post.SelectSingleNode("//span[@class='icon-thumbs-up-alt']").InnerText.Replace("likes", "")}  ") // likes
                        .AddImages(id,
                            post.SelectNodes("//div[@class='item']/img | //div[@class='single-photo']/img")?.Select(x => x.GetAttributeValue("src", "")) ?? Enumerable.Empty<string>(),
                            relativeMediaFolder)
                        .AddVideos(id,
                            post.SelectNodes("//div[@class='item']/video/source | //div[@class='single-photo']/video")?.Select(x => x.GetAttributeValue("src", "")) ?? Enumerable.Empty<string>(),
                            relativeMediaFolder)
                        .AddComments(ScrapeComments(post)).ToString()
                };
                Rss.Channel.Items.Add(item);
            }
            catch {}
        }
    }

    private static (List<string> usernames, List<string> messages) ScrapeComments(HtmlAgilityPack.HtmlNode post)
    {
        var usernames = new List<string>();
        var messages = new List<string>();

        try
        {
            for (int i = 0; i < post.SelectNodes("//div[@class='comment']").Count; i++)
            {
                usernames.Add(post.SelectNodes("//div[@class='comment-user-nickname']/a")[i].InnerText);
                messages.Add(post.SelectNodes("//div[@class='comment-text']")[i].InnerText);
            }
        }
        catch {}

        return new ValueTuple<List<string>, List<string>>(usernames, messages);
    }

    static Picuki()
    {
        sitename = "picuki";
        Media = new Media(sitename);

        SiteFolder = Path.Combine(Directory.GetCurrentDirectory(), sitename);
        relativeMediaFolder = Path.Combine(sitename, "media");

        Directory.CreateDirectory(Path.Combine(SiteFolder, "media"));
        Rss = GetRSS();
    }
}
