using RSS.Builders;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RSS.Scrapers;

public class PicukiStories : Website
{
    public async void Scrape(string username)
    {
        var doc = GetHTMLDocument($"https://www.picuki.com/profile/{username}").DocumentNode;
        if (doc.InnerHtml.Contains("<title>Error 403</title>")) return; // 403 error, skip profile

        var userId = Regex.Match(doc.InnerHtml, @"let\s+query\s*=\s*'(\d+)'").Groups[1].Value;
        var stories = ScrapeStories($"{Config.CurlImpersonateScriptLocation} -X POST https://www.picuki.com/app/controllers/ajax.php -d username={username} -d query={userId} -d type=story");
        foreach (var (story, i) in stories.WithIndex())
        {
            var id = await CalculateImageHash(Regex.Match(story, "data-video-poster=\"([^\"]*)\"").Groups[1].Value);
            var time = Regex.Match(story, "stories_count\\\">([^<]*)").Groups[1].Value.Trim();

            if (TimeBuilder.ParsePicukiTime(time) == null)
            {
                break;
            }

            if (rss.Channel.Items.Select(x => x.GUID).Contains(id))
            {
                Console.WriteLine($"{sitename}/{username}: Story {i + 1} ({time}) already scraped");
                continue;
            }

            Console.WriteLine($"{sitename}/{username}: Scraping story {i + 1} ({time})");

            var item = new Item{
                Title = $"{username} ({DateTime.Parse(TimeBuilder.ParsePicukiTime(time)!):dd MMM yyyy HH:mm:ss})",
                Link = rss.Channel.Link + $"/profile/{username}",
                Author = username,
                PubDate = TimeBuilder.ParsePicukiTime(time),
                GUID = id
            };

            var url = Regex.Match(story, "href=\"([^\"]*)\"").Groups[1].Value;
            var d = new DescriptionBuilder(Media).AddSpan(" ", false);
            if (url.EndsWith(".jpeg"))
            {
                d.AddImage(id, url, relativeMediaFolder);
            }
            else
            {
                d.AddVideo(id, url, relativeMediaFolder);
            }

            item.Description = d.ToString();

            rss.Channel.Items.Add(item);
        }
        
        SerializeXML();
    }

    async private static Task<string> CalculateImageHash(string imageUrl)
    {
        using HttpClient client = new HttpClient();

        var response = await client.GetAsync(imageUrl);
        var imageBytes = await response.Content.ReadAsByteArrayAsync();

        using SHA256 sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(imageBytes);

        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower()[..30];
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

        try
        {
            using JsonDocument document = JsonDocument.Parse(output.ToString());
            return document.RootElement.GetProperty("stories_container").GetString()?.Split("<div class=\"item\">").Skip(1).ToArray() ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public PicukiStories(string sitename, string title, string description, string link, string faviconUrl) : base(sitename, title, description, link, faviconUrl)
    {

    }
}
