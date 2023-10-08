using System.Xml.Serialization;

namespace RSS.Scrapers;

public class ProxiTok : Website
{
    public async Task Scrape(string username)
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
        var xmlRss = (RSS)serializer.Deserialize(reader)!;

        foreach (var item in xmlRss.Channel.Items.Where(x => !rss.Channel.Items.Select(y => y.GUID).Contains(x.GUID)))
        {
            item.Author = username;
            rss.Channel.Items.Add(item);
            Console.WriteLine($"{sitename}: Scraping {username}");
        }
    }

    public ProxiTok(string sitename, string title, string description, string link, string faviconUrl) : base(sitename, title, description, link, faviconUrl)
    {
        
    }
}
