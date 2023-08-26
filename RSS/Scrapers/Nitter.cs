using Microsoft.VisualBasic;
using RSS.Builders;
using System.Data;
using System.Text.RegularExpressions;

namespace RSS.Scrapers;

public class Nitter : Website
{
    public void Scrape()
    {
        var doc = Utils.GetHTMLDocument($"{link}/{username}/with_replies").DocumentNode;

        var rss = new RSS{
            Channel = new Channel{
                Title = doc.SelectSingleNode("//a[@class='profile-card-fullname']").InnerText,
                Link = $"{link}/{username}/with_replies",
                Items = new List<Item>(),
                Description = new DescriptionBuilder()
                    .AddParagraph(doc.SelectSingleNode("//div[@class='profile-bio']/p").InnerText.Trim()).ToString(),
            }
        };

        rss.Channel.Image = new Image{
            Url = new DescriptionBuilder()
                .AddImage($"{link}{doc.SelectSingleNode("//a[@class='profile-card-avatar']/img").GetAttributeValue("src", "")}", relativeImgFolder, "favicon").ToString(),
            Title = rss.Channel.Title,
            Link = rss.Channel.Link
        };

        if (File.Exists($"{usernameFolder}/rss.xml"))
        {
            rss = Utils.DeserializeXML($"{usernameFolder}/rss.xml");
        }

        var count = doc.SelectNodes("//div[@class='timeline-item ']").Count;
        var comments = doc.SelectNodes("//span[@class='icon-comment']/parent::div");
        var retweets = doc.SelectNodes("//span[@class='icon-retweet']/parent::div");
        var quotes = doc.SelectNodes("//span[@class='icon-quote']/parent::div");
        var likes = doc.SelectNodes("//span[@class='icon-heart']/parent::div");
        
        foreach (var (postUrl, i) in doc.SelectNodes("//div[@class='timeline-item ']/a").Select(x => "https://nitter.net" + x.GetAttributeValue("href", "")).WithIndex())
        {
            var id = Regex.Match(postUrl, @"\d+").Value;
            if (rss.Channel.Items.Select(x => x.GUID).Contains(id))
            {
                Console.WriteLine($"Post {i + 1}/{count} already scraped");
                continue;
            }

            var post = Utils.GetHTMLDocument(postUrl).DocumentNode;

            Console.WriteLine($"Scraping post {i + 1}/{count}");

            var item = new Item{
                Title = new DescriptionBuilder().AddParagraph(doc.SelectNodes("//div[@class='tweet-content media-body']")[i].InnerText).ToString(),
                Link = postUrl,
                Author = username,
                GUID = id,
                PubDate = TimeBuilder.ParseNitterTime(post.SelectSingleNode("//p[@class='tweet-published']").InnerText),
                Description = new DescriptionBuilder()
                    .AddSpanOrNot($"Comments: {comments[i].InnerText.Trim()}",
                        !string.IsNullOrEmpty(comments[i].InnerText.Trim()))
                    .AddSpanOrNot($"Retweets: {retweets[i].InnerText.Trim()}",
                        !string.IsNullOrEmpty(retweets[i].InnerText.Trim()))
                    .AddSpanOrNot($"Quotes: {quotes[i].InnerText.Trim()}",
                        !string.IsNullOrEmpty(quotes[i].InnerText.Trim()))
                    .AddSpanOrNot($"Likes: {likes[i].InnerText.Trim()}",
                        !string.IsNullOrEmpty(likes[i].InnerText.Trim()))
                    .AddImages(post.SelectNodes("//div[@class='main-thread']//a[@class='still-image']/img")?.Select(x => "https://nitter.net" + x.GetAttributeValue("src", "")) ?? Enumerable.Empty<string>(), relativeImgFolder, id).ToString()
            };

            rss.Channel.Items.Add(item);
        }

        Utils.SerializeXML<RSS>(usernameFolder, rss);
    }

    public Nitter(string username)
    {
        this.username = username;
        link = "https://nitter.net";
        siteName = "nitter";

        LoadSiteData();
    }
}
