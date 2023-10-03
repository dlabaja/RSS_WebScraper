using System.Xml.Serialization;
using static RSS.Scrapers.Website;

namespace RSS.Scrapers;

public static class ProxiTok
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
                Title = "ProxiTok",
                Link = Config.ProxiTokInstance,
                Items = new List<Item>(),
                Description = "All tiktoks in one place",
                Image = new Image{
                    Url = AddFavicon(Media, $"{Config.ProxiTokInstance}/favicon-32x32.png", relativeMediaFolder),
                    Title = "ProxiTok",
                    Link = Config.ProxiTokInstance
                }
            }
        };
    }

    public async static Task Scrape(string username)
    {
        var doc = GetHTMLDocument($"{Config.ProxiTokInstance}/@{username}").DocumentNode;

        try
        {
            Media.Add(username, doc.SelectSingleNode("//figure[@class='image is-inline-block is-128x128']/img").GetAttributeValue("src", ""));
        }
        catch { return; }

        string xml;
        try
        {
            using HttpClient httpClient = new HttpClient();
            xml = await httpClient.GetStringAsync($"{Config.ProxiTokInstance}/@{username}/rss");
        }
        catch
        {
            throw new RSSException("ProxiTok instance down or invalid username");
        }

        var serializer = new XmlSerializer(typeof(RSS));

        using StringReader reader = new StringReader(xml);
        var rss = (RSS)serializer.Deserialize(reader)!;

        foreach (var item in rss.Channel.Items.Where(x => !Rss.Channel.Items.Select(y => y.GUID).Contains(x.GUID)))
        {
            item.Author = username;
            Rss.Channel.Items.Add(item);
        }
    }

    static ProxiTok()
    {
        sitename = "proxitok";
        Media = new Media(sitename);

        SiteFolder = Path.Combine(Directory.GetCurrentDirectory(), sitename);
        relativeMediaFolder = Path.Combine(sitename, "media");

        Directory.CreateDirectory(Path.Combine(SiteFolder, "media"));
        Rss = GetRSS();
    }
}
