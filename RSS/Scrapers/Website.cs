using HtmlAgilityPack;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace RSS.Scrapers;

public static class Website
{
    public static void SerializeXML<T>(RSS rss, string siteFolder, string sitename)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        var filePath = Path.Combine(siteFolder, "rss.xml");

        if (!Directory.Exists(siteFolder))
        {
            Directory.CreateDirectory(siteFolder);
        }

        using var writer = new StreamWriter(filePath);
        rss.Channel.Items.RemoveAll(x => !Config.SitesAndUsernames[sitename].Contains(x.Author));
        rss.Channel.Items.Sort((x, y) => DateTime.Parse(x.PubDate).CompareTo(DateTime.Parse(y.PubDate)));
        serializer.Serialize(writer, rss);

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

        Console.WriteLine($"RSS file serialized as {filePath}");
    }

    private static void AddAttribute(this XmlDocument doc, string xpath, string atName, string value)
    {
        var attribute = doc.CreateAttribute(atName);
        attribute.Value = value;
        doc.SelectSingleNode(xpath)?.Attributes?.Append(attribute);
    }

    public static RSS DeserializeXML(string sitename)
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

    public static HtmlDocument GetHTMLDocument(string url, string cookieFilePath = "")
    {
        // create new curl-impersonate process
        var process = new Process{
            StartInfo = new ProcessStartInfo{
                FileName = "/bin/bash",
                Arguments = $"{Config.CurlImpersonateScriptLocation} \"{url}\" {(!string.IsNullOrEmpty(cookieFilePath) ? $"-b \"{cookieFilePath}\"" : "")}",
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

    public static string AddFavicon(Media media, string url, string relativeMediaFolder)
    {
        media.Add("favicon", url);
        return $"{Config.Url}/{relativeMediaFolder}/favicon";
    }
}
