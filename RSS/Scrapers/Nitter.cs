using HtmlAgilityPack;
using RSS.Builders;
using System.Collections;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace RSS.Scrapers;

public class Nitter : Website
{
    public void Scrape()
    {
        var media = new Media(siteName, username);
        var doc = Utils.GetHTMLDocument($"{link}/{username}/with_replies").DocumentNode;

        var rss = new RSS{
            Channel = new Channel{
                Title = doc.SelectSingleNode("//a[@class='profile-card-fullname']").InnerText,
                Link = $"{link}/{username}/with_replies",
                Items = new List<Item>(),
                Description = new DescriptionBuilder(media)
                    .AddParagraph(doc.SelectSingleNode("//div[@class='profile-bio']/p").InnerText.Trim()).ToString(),
            }
        };

        rss.Channel.Image = new Image{
            Url = new DescriptionBuilder(media)
                .AddImage($"{link}{doc.SelectSingleNode("//a[@class='profile-card-avatar']/img").GetAttributeValue("src", "")}", relativeImgFolder, "favicon").ToString(),
            Title = rss.Channel.Title,
            Link = rss.Channel.Link
        };

        if (File.Exists($"{usernameFolder}/rss.xml"))
        {
            rss = Utils.DeserializeXML($"{usernameFolder}/rss.xml");
        }

        var count = doc.SelectNodes("//div[@class='timeline-item ']").Count;

        foreach (var (postUrl, i) in doc.SelectNodes("//div[@class='timeline-item ']/a").Select(x => "https://nitter.net" + x.GetAttributeValue("href", "")).WithIndex())
        {
            var id = Regex.Match(postUrl, @"\d+").Value;
            if (rss.Channel.Items.Select(x => x.GUID).Contains(id))
            {
                Console.WriteLine($"Post {i + 1}/{count} already scraped");
                continue;
            }

            var post = Utils.GetHTMLDocument(postUrl, $"{Path.Combine(Directory.GetCurrentDirectory(), "data", "cookies", "nitter.txt")}").DocumentNode;

            Console.WriteLine($"Scraping post {i + 1}/{count}");

            var item = new Item{
                Title = new DescriptionBuilder(media).AddParagraph(doc.SelectNodes("//div[@class='tweet-content media-body']")[i].InnerText).ToString(),
                Link = postUrl,
                Author = username,
                GUID = id,
                PubDate = TimeBuilder.ParseNitterTime(post.SelectSingleNode("//p[@class='tweet-published']").InnerText),
                Description = ScrapeThreads(post, id, media)
            };

            rss.Channel.Items.Add(item);
        }

        Utils.SerializeXML<RSS>(usernameFolder, rss);
        media.DownloadAllMedia();
    }

    private string ScrapeThreads(HtmlNode post, string id, Media media)
    {
        var d = new DescriptionBuilder(media);

        var nodes = new HtmlNodeCollection(post);
        var xpathExpressions = new List<string>{
            "//div[@class='before-tweet thread-line']/div",
            "//div[@class='main-tweet']/div"
        };

        var separators = new List<int>();
        
        foreach (var selectedNodes in xpathExpressions.Select(xpathExpression => post.SelectNodes(xpathExpression) ?? Enumerable.Empty<HtmlNode>()))
        {
            foreach (var item in selectedNodes)
            {
                var html = new HtmlDocument();
                html.LoadHtml(item.InnerHtml);
                nodes.Add(html.DocumentNode);
            }
        }
        separators.Add((post.SelectNodes("//div[@class='before-tweet thread-line']/div")?.Count ?? 0) + 1);

        foreach (var threads in post.SelectNodes("//div[@class='reply thread thread-line']") ?? Enumerable.Empty<HtmlNode>())
        {
            var t = new HtmlDocument();
            t.LoadHtml(threads.InnerHtml);
    
            foreach (var item in t.DocumentNode.SelectNodes("//div[contains(@class, 'timeline-item')]") ?? Enumerable.Empty<HtmlNode>())
            {
                var html = new HtmlDocument();
                html.LoadHtml(item.InnerHtml);
                nodes.Add(html.DocumentNode);
            }

            separators.Add(nodes.Count);
        }

        separators.RemoveAt(separators.Count - 1);

        var i = 0;
        foreach (var item in nodes)
        {
            try
            {
                var comment = item.SelectSingleNode("//span[@class='icon-comment']/parent::div").InnerText.Trim();

                var retweet = item.SelectSingleNode("//span[@class='icon-retweet']/parent::div").InnerText.Trim();
                var quote = item.SelectSingleNode("//span[@class='icon-quote']/parent::div").InnerText.Trim();
                var like = item.SelectSingleNode("//span[@class='icon-heart']/parent::div").InnerText.Trim();
                var view = item.SelectSingleNode("//span[@class='icon-play']/parent::div")?.InnerText.Trim();

                d.AddSpan($"<b>{item.SelectSingleNode("//a[@class='fullname']").InnerText} ({item.SelectSingleNode("//a[@class='username']").InnerText})</b>          {item.SelectSingleNode("//span[@class='tweet-date']/a").InnerText}")
                    .AddSpanOrEmpty(string.Join(", ", item.SelectNodes("//div[@class='replying-to']")?.Select(x => x.InnerText) ?? Enumerable.Empty<string>()),
                        item.SelectNodes("//div[@class='replying-to']") != null)
                    .AddParagraph(item.SelectSingleNode("//div[@class='tweet-content media-body']").InnerText)
                    .AddImages($"{id}_{i}", item.SelectNodes("//a[@class='still-image']/img")?.Select(x => "https://nitter.net" + x.GetAttributeValue("src", "")) ?? Enumerable.Empty<string>(), relativeImgFolder)
                    .AddVideos($"{id}_{i}",item.SelectNodes("//div[@class='attachment video-container']/video")?
                                              .Select(x => Regex.Match(HttpUtility.UrlDecode(x.GetAttributeValue("data-url", "")), @"(https:\/\/video\.twimg\.com\/[^.]+\.m3u8)").Value)!
                                          ?? Enumerable.Empty<string>(),
                        relativeImgFolder)
                    .AddSpanOrEmpty($"💬 {comment}  ",
                        !string.IsNullOrEmpty(comment),
                        false)
                    .AddSpanOrEmpty($"🔁 {retweet}  ",
                        !string.IsNullOrEmpty(retweet),
                        false)
                    .AddSpanOrEmpty($"❞ {quote}  ",
                        !string.IsNullOrEmpty(quote),
                        false)
                    .AddSpanOrEmpty($"❤️ {like}  ",
                        !string.IsNullOrEmpty(like),
                        false)
                    .AddSpanOrEmpty($"▶️ {view}  ",
                        !string.IsNullOrEmpty(view),
                        false)
                    .AddBreak().AddBreak().AddBreak();
                i++;
                if (separators.Contains(i))
                {
                    d.AddParagraph("---------").AddBreak();
                }
            }
            catch
            {
                d.AddParagraph("This tweet is unavailable");
            }
        }

        return d.ToString();
    }

    public Nitter(string username)
    {
        this.username = username;
        link = "https://nitter.net";
        siteName = "nitter";

        LoadSiteData();
    }
}
