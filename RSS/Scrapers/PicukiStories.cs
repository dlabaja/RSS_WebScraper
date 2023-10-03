using RSS.Builders;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static RSS.Scrapers.Website;

namespace RSS.Scrapers;

public static class PicukiStories
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
                Title = "Picuki stories",
                Link = "https://www.picuki.com",
                Items = new List<Item>(),
                Description = "All stories in one place",
                Image = new Image{
                    Url = AddFavicon(Media, "https://www.picuki.com/p.svg", relativeMediaFolder),
                    Title = "Picuki stories",
                    Link = "https://www.picuki.com"
                }
            }
        };
    }
    
    public static void Scrape(string username)
    {
        var doc = GetHTMLDocument($"https://www.picuki.com/profile/{username}").DocumentNode;
        if (doc.InnerHtml.Contains("<title>Error 403</title>")) return; // 403 error, skip profile

        var userId = Regex.Match(doc.InnerHtml, @"let\s+query\s*=\s*'(\d+)'").Groups[1].Value;
        var stories = ScrapeStories($"{Config.CurlImpersonateScriptLocation} -X POST https://www.picuki.com/app/controllers/ajax.php -d username={username} -d query={userId} -d type=story");
        foreach (var (story, i) in stories.WithIndex())
        {
            var id = Convert.ToBase64String(Encoding.UTF8.GetBytes(Regex.Match(story, "data-origin=\"([^\"]*)\"").Groups[1].Value)[^50..^20]);
            var time = Regex.Match(story, "stories_count\\\">([^<]*)").Groups[1].Value.Trim();
            
            if (TimeBuilder.ParsePicukiTime(time) == null)
            {
                break;
            }
            
            if (Rss.Channel.Items.Select(x => x.GUID).Contains(id))
            {
                Console.WriteLine($"{sitename}/{username}: Story {i + 1} ({time}) already scraped");
                continue;
            }

            Console.WriteLine($"{sitename}/{username}: Scraping story {i + 1} ({time})");

            var item = new Item{
                Title = $"{username} ({DateTime.Parse(TimeBuilder.ParsePicukiTime(time)!):dd MMM yyyy HH:mm:ss})",
                Link = Rss.Channel.Link + $"/profile/{username}",
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

            Rss.Channel.Items.Add(item);
        }
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

    static PicukiStories()
    {
        sitename = "picuki_stories";
        Media = new Media(sitename);

        SiteFolder = Path.Combine(Directory.GetCurrentDirectory(), sitename);
        relativeMediaFolder = Path.Combine(sitename, "media");

        Directory.CreateDirectory(Path.Combine(SiteFolder, "media"));
        Rss = GetRSS();
    }
}
