using System.Text.Json;

namespace RSS;

public static class Config
{
    public static string Url { get; private set; }
    public static string CurlImpersonateScriptLocation { get; private set; }
    public static Dictionary<string, List<string>> SitesAndUsernames { get; private set; }

    public static void LoadConfig()
    {
        string jsonText = File.ReadAllText($"{Directory.GetCurrentDirectory()}/config.json");

        using JsonDocument document = JsonDocument.Parse(jsonText);
        JsonElement root = document.RootElement;

        try
        {
            Url = root.GetProperty("Url").GetString() ?? throw new Exception("Invalid URL");
            if (Url.EndsWith("/"))
            {
                Url.Remove(Url.Length - 1);
            }
            CurlImpersonateScriptLocation = root.GetProperty("CurlImpersonateScriptLocation").GetString()
                                            ?? throw new Exception("Invalid CurlImpersonateScriptLocation");

            SitesAndUsernames = new Dictionary<string, List<string>>() ?? throw new Exception("Invalid SitesAndUsernames");
            var sitesAndUsernamesElement = root.GetProperty("SitesAndUsernames");
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
