using HtmlAgilityPack;
using RSS.Builders;
using System.Text.RegularExpressions;

namespace RSS.Scrapers;

public class Picuki : Website
{
    public override void Scrape()
    {
        Console.WriteLine($"----\nScraping {username}");
        var doc = Utils.GetHTMLDocument($"{link}/{username}").DocumentNode;

        var rss = new RSS{
            Channel = new Channel{
                Title = doc.SelectSingleNode("//h1[@class='profile-name-top']").InnerText,
                Link = $"{link}/{username}",
                Items = new List<Item>(),
                Description = doc.SelectSingleNode("//div[@class='profile-description']").InnerText.Trim(),
            }
        };

        Utils.DownloadImage(doc.SelectSingleNode("//img[@class='profile-avatar-image']").GetAttributeValue("src", ""), imgFolder, "favicon");
        rss.Channel.Image = new Image{
            Url = $"{Config.Url}/{this.siteName}/{username}/images/favicon.png",
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
            var post = Utils.GetHTMLDocument(postUrl).DocumentNode;

            var id = Regex.Match(postUrl, @"\/media\/(\d+)").Groups[1].ToString();
            if (rss.Channel.Items.Select(x => x.GUID).Contains(id))
            {
                Console.WriteLine($"Post {i + 1}/{count} already scraped");
                continue;
            }
            Console.WriteLine($"Scraping post {i + 1}/{count}");

            var item = new Item();
            item.Title = doc.SelectNodes("//div[@class='photo-description']")[i].InnerText.Trim();
            item.Link = rss.Channel.Link;
            item.Author = username;
            item.GUID = id;

            var d = new DescriptionBuilder();
            try
            {
                d.AddSpan($"Location: {post.SelectSingleNode("//div[@class='location']/text()[normalize-space()]").InnerText.Trim()}"); // location
            }
            catch {}
            
            d.AddSpan(post.SelectSingleNode("//span[@class='icon-thumbs-up-alt']").InnerText) // likes
                .AddSpan($"{post.SelectSingleNode("//span[@id='commentsCount']").InnerText} comments"); // commentsCount;
            try
            {
                Utils.DownloadImage(post.SelectSingleNode("//img[@id='image-photo']").GetAttributeValue("src", ""), imgFolder, id);
                d.AddImage($"{Config.Url}/{this.siteName}/{username}/images/{id}.png");
            }
            catch
            {
                // todo albums
            }

            d.AddComments(ScrapeComments(post));
            item.Description = d.ToString();
            item.PubDate = TimeBuilder.ParsePicukiTime(post.SelectSingleNode("//div[@class='single-photo-time']").InnerText);

            rss.Channel.Items.Add(item);
        }

        Utils.SerializeXML<RSS>(usernameFolder, rss);
    }

    private static (List<string> usernames, List<string> messages) ScrapeComments(HtmlNode post)
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

    public Picuki(string username) : base(username)
    {
        this.username = username;
        link = "https://www.picuki.com/profile/tmbkofficial";
        siteName = "picuki";
    }
}
