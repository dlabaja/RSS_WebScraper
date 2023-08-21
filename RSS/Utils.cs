using System.Text.Json;
using System;
using System.Net.Http;
using HtmlAgilityPack;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace RSS;

public class Utils
{
    public static string saveLocation = "/home/dlabaja/Documents/RSS";
    private const string curlImpersonateScriptLocation = "/home/dlabaja/.curl-impersonate/curl_ff109";

    public static HtmlDocument GetHTMLDocument(string url)
    {
        // create new curl-impersonate process
        var process = new Process{
            StartInfo = new ProcessStartInfo{
                FileName = "/bin/bash",
                Arguments = $"{curlImpersonateScriptLocation} {url}",
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
    
    public static void DownloadImage(string url, string path, string id)
    {
        // create new curl-impersonate process
        var process = new Process{
            StartInfo = new ProcessStartInfo{
                FileName = "/bin/bash",
                Arguments = $"{curlImpersonateScriptLocation} \"{url}\" --output \"{path}/{id}.png\"",
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
    }

    public static void SerializeXML<T>(string siteName, string username, object o)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        var filePath = $"{saveLocation}/{siteName}/{username}/{username}.xml";

        if (!Directory.Exists($"{saveLocation}/{siteName}/{username}"))
        {
            Directory.CreateDirectory($"{saveLocation}/{siteName}/{username}");
        }

        using var writer = new StreamWriter(filePath);
        serializer.Serialize(writer, o);

        var doc = new XmlDocument();
        doc.Load(filePath);
        
        // todo najít způsob jak tam ten atribut přidat ještě před serializací
        XmlAttribute versionAttribute = doc.CreateAttribute("version");
        versionAttribute.Value = "2.0";
        doc.SelectSingleNode("//rss")?.Attributes?.Append(versionAttribute);
        doc.Save(filePath);

        Console.WriteLine($"RSS file serialized as {filePath}");
    }
}

public static class EnumExtension {
    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)       
        => self.Select((item, index) => (item, index));
}
