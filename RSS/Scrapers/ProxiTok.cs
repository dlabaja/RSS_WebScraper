using RSS.Builders;
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
            Media.Add(username, "{PROXITOK_URL}" + doc.SelectSingleNode("//figure[@class='image is-inline-block is-128x128']/img").GetAttributeValue("src", ""));
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
            Console.WriteLine($"ProxiTok instance down or invalid username '{username}'");
            return;
        }

        var serializer = new XmlSerializer(typeof(RSS));

        RSS xmlRss;
        using StringReader reader = new StringReader(xml);
        try
        {
            xmlRss = (RSS)serializer.Deserialize(reader)!;
        }
        catch (Exception e)
        {
            Console.WriteLine($"ProxiTok XML ERROR for user {username}, this is probably fault on site's site\n{e.Message}\n{e.Source}\n{e.InnerException}");
            return;
        }

        foreach (var item in xmlRss.Channel.Items.Where(x => !Rss.Channel.Items.Select(y => y.GUID).Contains(x.GUID)))
        {
            if (Regex.IsMatch(item.Description, "<source src=\"(.*)\" type"))
            {
                var url = "{PROXITOK_URL}" + Regex.Match(item.Description, "<source src=\"(.*)\" type").Groups[1].Value;
                Media.Add(item.GUID, url);
                item.Description = new DescriptionBuilder(Media).AddVideo(item.GUID, url, relativeMediaFolder).ToString();
            }
            else
            {
                var matches = Regex.Matches(item.Description, "<img src=\"([^\"]+)\">");
                var d = new DescriptionBuilder(Media);
                for (int i = 0; i < matches.Count; i++)
                {
                    var url = "{PROXITOK_URL}" + matches[i].Groups[1].Value;
                    var id = $"{item.GUID}_{i}";
                    Media.Add(id, url);
                    d.AddImage(id, url, relativeMediaFolder);
                }

                item.Description = d.ToString();
            }

            item.Author = username;
            item.Link = $"{Config.ProxiTokInstance}/@{username}";
            Rss.Channel.Items.Add(item);
            Console.WriteLine($"{sitename}: Scraping {username}");
        }

        SerializeXML();
    }

    public ProxiTok(string sitename, string title, string description, string link, string faviconUrl) : base(sitename, title, description, link, faviconUrl)
    {

    }
}
