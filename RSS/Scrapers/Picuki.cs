using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace RSS.Scrapers;

public static class Picuki
{
    public static void ScrapePicuki(string username)
    {
        Console.WriteLine($"Scraping {username}");
        var doc = Utils.GetHTMLDocument($"https://www.picuki.com/profile/{username}").DocumentNode;
        var rss = new RSS();

        var channel = new Channel{
            Title = doc.SelectSingleNode("//h1[@class='profile-name-top']").InnerText,
            Link = $"https://www.picuki.com/profile/{username}",
            Items = new List<Item>(),
            Description = doc.SelectSingleNode("//div[@class='profile-description']").InnerText.Trim(),
        };

        channel.Image = new Image{
            Url = doc.SelectSingleNode("//img[@class='profile-avatar-image']").GetAttributeValue("src", ""),
            Title = channel.Title,
            Link = channel.Link
        };

        foreach (var (postUrl, i) in doc.SelectNodes("//div[@class='photo']/a").Select(x => x.GetAttributeValue("href", "")).WithIndex())
        {
            Console.WriteLine($"Scraping post {i + 1}/12");
            var post = Utils.GetHTMLDocument(postUrl).DocumentNode;

            var item = new Item();
            item.Title = doc.SelectNodes("//div[@class='photo-description']")[i].InnerText.Trim();
            item.Link = channel.Link;
            item.Author = username;

            var d = new DescriptionBuilder()
                .AddSpan($"Location: {post.SelectSingleNode("//div[@class='location']/text()[normalize-space()]").InnerText.Trim() ?? "None"}") // location
                .AddSpan(post.SelectSingleNode("//span[@class='icon-thumbs-up-alt']").InnerText) // likes
                .AddSpan($"{post.SelectSingleNode("//span[@id='commentsCount']").InnerText} comments"); // commentsCount;
            try
            {
                d.AddImage(post.SelectSingleNode("//img[@id='image-photo']").GetAttributeValue("src", ""));
                var id = Regex.Match(postUrl, @"\/media\/(\d+)").Groups[1].ToString();
                
                Utils.DownloadImage(post.SelectSingleNode("//img[@id='image-photo']").GetAttributeValue("src", ""), $"{Utils.saveLocation}/picuki/{username}/images", id);
            }
            catch
            {
                // albums
            }

            d.AddComments(ScrapeComments(post));

            item.Description = d.ToString();

            channel.Items.Add(item);
        }

        rss.Channel = channel;
        Utils.SerializeXML<RSS>("picuki", username, rss);
    }

    private static (List<string> usernames, List<string> messages) ScrapeComments(HtmlNode post)
    {
        var usernames = new List<string>();
        var messages = new List<string>();

        for (int i = 0; i < post.SelectNodes("//div[@class='comment']").Count; i++)
        {
            usernames.Add(post.SelectNodes("//div[@class='comment-user-nickname']/a")[i].InnerText);
            messages.Add(post.SelectNodes("//div[@class='comment-text']")[i].InnerText);
        }

        return new ValueTuple<List<string>, List<string>>(usernames, messages);
    }
}

[XmlRoot("rss")]
public class RSS
{
    [XmlElement("channel")]
    public Channel Channel { get; set; }
}

public class Channel
{
    [XmlElement("title")]
    public string Title { get; set; }

    [XmlElement("description")]
    public string Description { get; set; }

    [XmlElement("link")]
    public string Link { get; set; }

    [XmlElement("image")]
    public Image Image { get; set; }

    [XmlElement("item")]
    public List<Item> Items { get; set; }
}

public class Image
{
    [XmlElement("url")]
    public string Url { get; set; }

    [XmlElement("title")]
    public string Title { get; set; }

    [XmlElement("link")]
    public string Link { get; set; }
}

public class Item
{
    [XmlElement("title")]
    public string Title { get; set; }

    [XmlElement("link")]
    public string Link { get; set; }

    [XmlElement("description")]
    public string Description { get; set; }

    [XmlElement("author")]
    public string Author { get; set; }

    /*[XmlElement("pubDate")]
    public string PubDate { get; set; }*/
}
