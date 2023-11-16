using HtmlAgilityPack;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace RSS.Scrapers;

public class Website
{
    public Media Media { get; }
    public RSS Rss { get; }
    private string siteFolder { get; }
    protected readonly string relativeMediaFolder;
    protected readonly string scrappedIdsPath;

    protected readonly string sitename;
    protected List<string> scrappedIds;

    protected Website(string sitename, string title, string description, string link, string faviconUrl)
    {
        this.sitename = sitename;
        Media = new Media(this.sitename);

        siteFolder = Path.Combine(Directory.GetCurrentDirectory(), sitename);
        relativeMediaFolder = Path.Combine(sitename, "media");

        if (!Directory.Exists(siteFolder))
        {
            Directory.CreateDirectory(siteFolder);
        }
        
        Directory.CreateDirectory(Path.Combine(siteFolder, "media"));

        scrappedIdsPath = Path.Combine(siteFolder, "scrapped_ids.txt");
        if (!File.Exists(scrappedIdsPath))
        {
            File.Create(scrappedIdsPath).Close();
        }

        using StreamReader sr = new StreamReader(scrappedIdsPath);
        scrappedIds = File.ReadAllLines(scrappedIdsPath).ToList();

        Rss = GetRSS(title, description, link, faviconUrl);
    }

    private RSS GetRSS(string title, string description, string link, string faviconUrl)
    {
        if (File.Exists(Path.Combine(siteFolder, "rss.xml")))
            return DeserializeXML();

        return new RSS{
            Channel = new Channel{
                Title = title,
                Link = link,
                Items = new List<Item>(),
                Description = description,
                Image = new Image{
                    Url = AddFavicon(faviconUrl),
                    Title = title,
                    Link = link
                }
            }
        };
    }

    public void SerializeXML()
    {
        Media.SaveJson();
        var filePath = Path.Combine(siteFolder, "rss.xml");

        XmlSerializer serializer = new XmlSerializer(typeof(RSS));

        if (Rss.Channel.Items.Count != 0)
        {
            try
            {
                try
                {
                    Rss.Channel.Items.RemoveAll(x => !Config.SitesAndUsernames[sitename].Contains(x.Author));
                }catch{}

                Rss.Channel.Items.Sort((x, y) => DateTime.Parse(x.PubDate).CompareTo(DateTime.Parse(y.PubDate)));

                SaveScrappedIds();

                int count = Math.Min(20, Rss.Channel.Items.Count);
                Rss.Channel.Items = Rss.Channel.Items.ToArray()[^count..].ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        using var writer = new StreamWriter(filePath);
        serializer.Serialize(writer, Rss);
        GenerateRSSHeaders(filePath);
        Console.WriteLine($"RSS file serialized as {filePath}");
    }

    private void SaveScrappedIds()
    {
        using StreamWriter sw = File.AppendText(scrappedIdsPath);
        foreach (var id in Rss.Channel.Items.Select(x => x.GUID))
            if (!scrappedIds.Contains(id))
                sw.WriteLine(id);
    }

    private void GenerateRSSHeaders(string filePath)
    {
        var doc = new XmlDocument();
        doc.Load(filePath);

        doc.AddAttribute("//rss", "version", "2.0");
        doc.AddAttribute("//rss", "xmlns:media", "http://search.yahoo.com/mrss/");
        doc.AddAttribute("//item/guid", "isPermaLink", "false");

        // adding favicons to every item
        foreach (XmlNode itemNode in doc.SelectNodes("//item")!)
        {
            string author = itemNode.SelectSingleNode("author")!.InnerText;

            XmlElement mediaContent = doc.CreateElement("media", "content", "http://search.yahoo.com/mrss/");
            mediaContent.SetAttribute("url", $"{Config.Url}/{sitename}/media/{author}");
            mediaContent.SetAttribute("medium", "image");

            XmlElement mediaTitle = doc.CreateElement("media", "title", "http://search.yahoo.com/mrss/");
            mediaTitle.SetAttribute("type", "plain");
            mediaTitle.InnerText = "Image Title";

            mediaContent.AppendChild(mediaTitle);
            itemNode.AppendChild(mediaContent);
        }

        doc.Save(filePath);
    }

    private RSS DeserializeXML()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), sitename, "rss.xml");

        var xmlData = File.ReadAllText(path);
        var serializer = new XmlSerializer(typeof(RSS));

        using StringReader reader = new StringReader(xmlData);
        try
        {
            return (RSS)serializer.Deserialize(reader)!;
        }
        catch
        {
            File.Delete(path);
            throw;
        }
    }

    protected static HtmlDocument GetHTMLDocument(string url, string cookie = "")
    {
        // create new curl-impersonate process
        var process = new Process{
            StartInfo = new ProcessStartInfo{
                FileName = "/bin/bash",
                Arguments = $"{Config.CurlImpersonateScriptLocation} \"{url}\" {(!string.IsNullOrEmpty(cookie) ? $"-b {cookie}" : "")}",
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

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(output.ToString());

        return htmlDocument;
    }

    private string AddFavicon(string url)
    {
        Media.Add("favicon", url);
        return $"{Config.Url}/{relativeMediaFolder}/favicon";
    }
}
