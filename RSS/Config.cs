using System.Text.Json;

namespace RSS;

public static class Config
{
    public static string Url { get; private set; }
    public static string CurlImpersonateScriptLocation { get; private set; }
    public static string FFmpegLocation { get; private set; }
    public static uint ScrapeTimer { get; private set; }
    public static string NitterInstance { get; private set; }
    public static string ProxiTokInstance { get; private set; }
    public static string InvidiousInstance { get; private set; }
    public static bool InvidiousFilterShorts { get; private set; }
    public static Dictionary<string, List<string>> SitesAndUsernames { get; private set; }

    public static void LoadConfig()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "config.json");

        if (!File.Exists(path))
        {
            using StreamWriter writer = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), "config.json"));
            writer.Write("{\n    \"url\": \"http://localhost:8000\",\n    \"ffmpeg_location\": \"<path to ffmpeg bin>\",\n    \"curl_impersonate_script_location\": \"<path to curl-impersonate script (eg curl_ff109)>\",\n    \"scrape_timer\": 15,\n    \"nitter_instance\": \"https://nitter.net\", \n    \"proxitok_instance\": \"https://proxitok.pabloferreiro.es\",\n    \"invidious_instance\": \"https://invidious.poast.org\",\n    \"invidious_filter_shorts\": false,\n    \"sites_and_usernames\": {\n        \"nitter_replies_blacklist\": [],\n        \"picuki_stories_blacklist\": [],\n        \"picuki\": [],\n        \"nitter\": [],\n        \"proxitok\": []\n    }\n}");
            writer.Flush();
            throw new RSSException($"config.json created in {Directory.GetCurrentDirectory()}, please fill it and start the scraper again");
        }

        string jsonText = File.ReadAllText(path);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(jsonText);
        }
        catch
        {
            throw new RSSException($"Invalid JSON format in {path}");
        }

        JsonElement root = document.RootElement;

        try
        {
            Url = root.GetProperty("url").GetString() ?? throw new RSSException("Invalid URL");
            if (Url.EndsWith("/"))
            {
                Url.Remove(Url.Length - 1);
            }

            CurlImpersonateScriptLocation = root.GetProperty("curl_impersonate_script_location").GetString()
                                            ?? throw new RSSException("Invalid curl_impersonate_script_location");
            FFmpegLocation = root.GetProperty("ffmpeg_location").GetString() ?? throw new RSSException("Invalid ffmpeg_location");
            NitterInstance = (root.GetProperty("nitter_instance").GetString()!.EndsWith("/") ? root.GetProperty("nitter_instance").GetString()?[..^1] : root.GetProperty("nitter_instance").GetString()) ?? throw new RSSException("Invalid nitter_instance");
            ProxiTokInstance = (root.GetProperty("proxitok_instance").GetString()!.EndsWith("/") ? root.GetProperty("proxitok_instance").GetString()?[..^1] : root.GetProperty("proxitok_instance").GetString()) ?? throw new RSSException("Invalid proxitok_instance");
            InvidiousInstance = (root.GetProperty("invidious_instance").GetString()!.EndsWith("/") ? root.GetProperty("invidious_instance").GetString()?[..^1] : root.GetProperty("invidious_instance").GetString()) ?? throw new RSSException("Invalid invidious_instance");
            InvidiousFilterShorts = root.GetProperty("invidious_filter_shorts").GetBoolean();
            
            try
            {
                ScrapeTimer = root.GetProperty("scrape_timer").GetUInt32();
                if (ScrapeTimer == 0) throw new RSSException("Scrape_Timer must be greater than 0");
            }
            catch { throw new RSSException("Scrape_Timer must be greater than 0"); }

            SitesAndUsernames = new Dictionary<string, List<string>>() ?? throw new RSSException("Invalid sites_and_usernames");
            var sitesAndUsernamesElement = root.GetProperty("sites_and_usernames");
            foreach (var siteElement in sitesAndUsernamesElement.EnumerateObject())
            {
                var siteName = siteElement.Name;
                var usernames = siteElement.Value.EnumerateArray().Select(usernameElement => usernameElement.GetString()).ToList();
                SitesAndUsernames.Add(siteName, usernames);
            }
        }

        catch (Exception e)
        {
            throw new RSSException($"Invalid config.json file, check if\n" +
                                   $"1). It's in the {Directory.GetCurrentDirectory()}/config.json location\n" +
                                   $"2). The json is valid and contains all required fields (as shown in github.com/dlabaja/RSS_WebScraper)\n\n" + e.Message);
        }
    }
}
