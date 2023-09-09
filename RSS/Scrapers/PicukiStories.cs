using HtmlAgilityPack;
using RSS.Builders;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RSS.Scrapers;

public class PicukiStories : Website
{
    public void Scrape()
    {
        var media = new Media(siteName, username);
        var doc = GetHTMLDocument($"{link}/{username}").DocumentNode;

        var rss = new RSS{
            Channel = new Channel{
                Title = doc.SelectSingleNode("//h1[@class='profile-name-top']").InnerText,
                Link = $"{link}/{username}",
                Items = new List<Item>(),
                Description = doc.SelectSingleNode("//div[@class='profile-description']").InnerText.Trim(),
            }
        };

        rss.Channel.Image = new Image{
            Url = new DescriptionBuilder(media)
                .AddImage("favicon", doc.SelectSingleNode("//img[@class='profile-avatar-image']").GetAttributeValue("src", ""), relativeImgFolder).ToString(),
            Title = rss.Channel.Title,
            Link = rss.Channel.Link
        };

        if (File.Exists(Path.Combine(usernameFolder, "rss.xml")))
        {
            rss = DeserializeXML(Path.Combine(usernameFolder, "rss.xml"));
        }

        var userId = Regex.Match(doc.InnerHtml, @"let\s+query\s*=\s*'(\d+)'").Groups[1].Value;
        var stories = ScrapeStories($"{Config.CurlImpersonateScriptLocation} -X POST https://www.picuki.com/app/controllers/ajax.php -d username={username} -d query={userId} -d type=story");
        
        foreach (var (story, i) in stories.WithIndex())
        {
            var id = Convert.ToBase64String(Encoding.UTF8.GetBytes(Regex.Match(story, "data-origin=\"([^\"]*)\"").Groups[1].Value))[^20..];
            var time = Regex.Match(story, "stories_count\\\">([^<]*)").Groups[1].Value.Trim();
            
            if (TimeBuilder.ParsePicukiTime(time) == null)
            {
                break;
            }
            
            if (rss.Channel.Items.Select(x => x.GUID).Contains(id))
            {
                Console.WriteLine($"Story {i + 1} ({time}) already scraped");
                continue;
            }

            Console.WriteLine($"Scraping story {i + 1} ({time})");

            var item = new Item{
                Title = $"Story ({DateTime.Parse(TimeBuilder.ParsePicukiTime(time)!):dd MMM yyyy HH:mm:ss})",
                Link = rss.Channel.Link,
                Author = username,
                PubDate = TimeBuilder.ParsePicukiTime(time),
                GUID = id
            };

            var url = Regex.Match(story, "href=\"([^\"]*)\"").Groups[1].Value;
            var d = new DescriptionBuilder(media).AddSpan(" ", false);
            if (url.EndsWith(".jpeg"))
            {
                d.AddImage(id, url, relativeImgFolder);
            }
            else
            {
                d.AddVideo(id, url, relativeImgFolder);
            }

            item.Description = d.ToString();

            rss.Channel.Items.Add(item);
        }

        SerializeXML<RSS>(usernameFolder, rss);
        media.DownloadAllMedia();
    }

    private static IEnumerable<string> ScrapeStories(string arguments)
    {
        // create new curl-impersonate process
        var process = new Process{
            StartInfo = new ProcessStartInfo{
                FileName = "/bin/bash",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                output.Append(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.WaitForExit();

        using JsonDocument document = JsonDocument.Parse(output.ToString());
        try
        {
            return document.RootElement.GetProperty("stories_container").GetString()?.Split("<div class=\"item\">").Skip(1).ToArray() ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public PicukiStories(string username)
    {
        this.username = username;
        link = "https://www.picuki.com/profile";
        siteName = "picuki";

        LoadSiteData();
    }
}
