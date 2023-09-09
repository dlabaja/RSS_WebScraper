using HtmlAgilityPack;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace RSS.Scrapers;

public class Website
{
    private string appFolder;
    protected string usernameFolder;
    private string imgFolder;
    protected string relativeImgFolder;

    protected string siteName;
    protected string link;
    protected string username;

    protected void LoadSiteData()
    {
        appFolder = Path.Combine(Directory.GetCurrentDirectory(), siteName);
        usernameFolder = Path.Combine(appFolder, username);
        imgFolder = Path.Combine(usernameFolder, "media");
        relativeImgFolder = Path.Combine(siteName, username, "media");;

        Directory.CreateDirectory(imgFolder);
    }

    protected static void SerializeXML<T>( string usernameFolder, object o)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        var filePath = $"{usernameFolder}/rss.xml";

        if (!Directory.Exists($"{usernameFolder}"))
        {
            Directory.CreateDirectory($"{usernameFolder}");
        }

        using var writer = new StreamWriter(filePath);
        serializer.Serialize(writer, o);

        var doc = new XmlDocument();
        doc.Load(filePath);
        
        var versionAttribute = doc.CreateAttribute("version");
        versionAttribute.Value = "2.0";
        doc.SelectSingleNode("//rss")?.Attributes?.Append(versionAttribute);
        
        var permaLinkAttribute = doc.CreateAttribute("isPermaLink");
        permaLinkAttribute.Value = "false";
        doc.SelectSingleNode("//item/guid")?.Attributes?.Append(permaLinkAttribute);
        
        doc.Save(filePath);

        Console.WriteLine($"RSS file serialized as {filePath}");
    }

    protected static RSS DeserializeXML(string path)
    {
        var xmlData = File.ReadAllText(path);
        var serializer = new XmlSerializer(typeof(RSS));

        using StringReader reader = new StringReader(xmlData);
        return (RSS)serializer.Deserialize(reader)!;
    }

    protected static HtmlDocument GetHTMLDocument(string url, string cookieFilePath = "")
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
}
