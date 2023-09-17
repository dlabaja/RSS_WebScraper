using System.Text.Json;

namespace RSS;

public static class Config
{
    public static string Url { get; private set; }
    public static string CurlImpersonateScriptLocation { get; private set; }
    public static string FFmpegLocation { get; private set; }
    public static string NitterInstance { get; private set; }
    public static Dictionary<string, List<string>> SitesAndUsernames { get; private set; }

    public static void LoadConfig()
    {
        string jsonText = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "data", "config.json"));

        using JsonDocument document = JsonDocument.Parse(jsonText);
        JsonElement root = document.RootElement;

        try
        {
            Url = root.GetProperty("url").GetString() ?? throw new Exception("Invalid URL");
            if (Url.EndsWith("/"))
            {
                Url.Remove(Url.Length - 1);
            }

            CurlImpersonateScriptLocation = root.GetProperty("curl_impersonate_script_location").GetString()
                                            ?? throw new Exception("Invalid curl_impersonate_script_location");
            FFmpegLocation = root.GetProperty("ffmpeg_location").GetString() ?? throw new Exception("Invalid ffmpeg_location");
            NitterInstance = (root.GetProperty("nitter_instance").GetString()!.EndsWith("/") ? root.GetProperty("nitter_instance").GetString()?[..^1] : root.GetProperty("nitter_instance").GetString()) ?? throw new Exception("Invalid nitter_instance");
            SitesAndUsernames = new Dictionary<string, List<string>>() ?? throw new Exception("Invalid sites_and_usernames");
            var sitesAndUsernamesElement = root.GetProperty("sites_and_usernames");
            foreach (var siteElement in sitesAndUsernamesElement.EnumerateObject())
            {
                var siteName = siteElement.Name;
                var usernames = siteElement.Value.EnumerateArray().Select(usernameElement => usernameElement.GetString()).ToList();
                SitesAndUsernames.Add(siteName, usernames);
            }
        }
        catch
        {
            throw new Exception($"Invalid config.json file, check if\n" +
                                $"1). It's in the {Directory.GetCurrentDirectory()}/config.json location\n" +
                                $"2). The json is valid and contains all required fields");
        }
    }
}
