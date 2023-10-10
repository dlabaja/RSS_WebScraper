using System.Text.RegularExpressions;
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
            if (Regex.IsMatch(item.Description, "<source src=\"(.*)\" type"))
            {
                var url = Config.ProxiTokInstance + Regex.Match(item.Description, "<source src=\"(.*)\" type").Groups[1].Value;
                Media.Add(item.GUID, url);
                item.Description = item.Description.Replace(url, $"{Config.Url}/proxitok/media/{item.GUID}");
            }
            else
            {
                var matches = Regex.Matches(item.Description, "<img src=\"([^\"]+)\">");
                for (int i = 0; i < matches.Count; i++)
                {
                    var url = Config.ProxiTokInstance + matches[i].Groups[1].Value;
                    var id = $"{item.GUID}_{i}";
                    Media.Add(id, url);
                    item.Description = item.Description.Replace(url, $"{Config.Url}/proxitok/media/{id}");
                }
            }

            item.Author = username;
            rss.Channel.Items.Add(item);
            Console.WriteLine($"{sitename}: Scraping {username}");
        }

        SerializeXML();
    }

    public ProxiTok(string sitename, string title, string description, string link, string faviconUrl) : base(sitename, title, description, link, faviconUrl)
    {

    }
}
