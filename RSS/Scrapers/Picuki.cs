using HtmlAgilityPack;
using RSS.Builders;
using System.Text.RegularExpressions;

namespace RSS.Scrapers;

public class Picuki : Website
{
    public void Scrape()
    {
        var doc = Utils.GetHTMLDocument($"{link}/{username}").DocumentNode;

        var rss = new RSS{
            Channel = new Channel{
                Title = doc.SelectSingleNode("//h1[@class='profile-name-top']").InnerText,
                Link = $"{link}/{username}",
                Items = new List<Item>(),
                Description = doc.SelectSingleNode("//div[@class='profile-description']").InnerText.Trim(),
            }
        };
        
        rss.Channel.Image = new Image{
            //Url = $"{Config.Url}/{this.siteName}/{username}/images/favicon.png",
            Url = new DescriptionBuilder()
                .AddImage(doc.SelectSingleNode("//img[@class='profile-avatar-image']").GetAttributeValue("src", ""), relativeImgFolder, "favicon").ToString(),
            Title = rss.Channel.Title,
            Link = rss.Channel.Link
        };

        if (File.Exists($"{usernameFolder}/rss.xml"))
        {
            rss = Utils.DeserializeXML($"{usernameFolder}/rss.xml");
        }

        var count = doc.SelectNodes("//div[@class='photo']/a").Count;
        foreach (var (postUrl, i) in doc.SelectNodes("//div[@class='photo']/a").Select(x => x.GetAttributeValue("href", "")).WithIndex())
        {
            var id = Regex.Match(postUrl, @"\/media\/(\d+)").Groups[1].ToString();
            if (rss.Channel.Items.Select(x => x.GUID).Contains(id))
            {
                Console.WriteLine($"Post {i + 1}/{count} already scraped");
                continue;
            }

            var post = Utils.GetHTMLDocument(postUrl).DocumentNode;

            Console.WriteLine($"Scraping post {i + 1}/{count}");
            //todo downloadImage

            var item = new Item{
                Title = doc.SelectNodes("//div[@class='photo-description']")[i].InnerText.Trim(),
                Link = rss.Channel.Link,
                Author = username,
                PubDate = TimeBuilder.ParsePicukiTime(post.SelectSingleNode("//div[@class='single-photo-time']").InnerText),
                GUID = id,
                Description = new DescriptionBuilder()
                    .AddSpan($"Location: {post.SelectSingleNode("//div[@class='location']/text()[normalize-space()]").InnerText.Trim()}") // location
                    .AddSpan(post.SelectSingleNode("//span[@class='icon-thumbs-up-alt']").InnerText) // likes
                    .AddSpan($"{post.SelectSingleNode("//span[@id='commentsCount']").InnerText} comments") // commentsCount;
                    //.AddImage($"{Config.Url}/{siteName}/{username}/images/{id}.png")
                    .AddComments(ScrapeComments(post)).ToString()
            };

            rss.Channel.Items.Add(item);
        }

        Utils.SerializeXML<RSS>(usernameFolder, rss);
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

    public Picuki(string username)
    {
        this.username = username;
        link = "https://www.picuki.com/profile";
        siteName = "picuki";

        LoadSiteData();
    }
}
