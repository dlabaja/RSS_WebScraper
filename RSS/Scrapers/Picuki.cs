using RSS.Builders;
using System.Text.RegularExpressions;

namespace RSS.Scrapers;

public class Picuki : Website
{
    public void Scrape(string username)
    {
        var doc = GetHTMLDocument($"https://www.picuki.com/profile/{username}").DocumentNode;
        if (doc.InnerHtml.Contains("<title>Error 403</title>")) return; // 403 error, skip profile

        Media.Add(username, doc.SelectSingleNode("//img[@class='profile-avatar-image']")?.GetAttributeValue("src", "") ?? string.Empty);

        var count = doc.SelectNodes("//div[@class='photo']/a")?.Count;
        if (count == null) return;

        foreach (var (postUrl, i) in doc.SelectNodes("//div[@class='photo']/a").Select(x => x.GetAttributeValue("href", "")).WithIndex())
        {
            var id = Regex.Match(postUrl, @"\/media\/(\d+)").Groups[1].ToString();
            if (scrappedIds.Contains(id))
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

    public Picuki(string sitename, string title, string description, string link, string faviconUrl) : base(sitename, title, description, link, faviconUrl)
    {

    }
}
