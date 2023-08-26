using System.Text.Json;
using System;
using System.Net.Http;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace RSS;

public class Utils
{
    public static HtmlDocument GetHTMLDocument(string url)
    {
        // create new curl-impersonate process
        var process = new Process{
            StartInfo = new ProcessStartInfo{
                FileName = "/bin/bash",
                Arguments = $"{Config.CurlImpersonateScriptLocation} {url}",
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

    public static void SerializeXML<T>( string usernameFolder, object o)
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
        
        // todo najít způsob jak tam ten atribut přidat ještě před serializací
        var versionAttribute = doc.CreateAttribute("version");
        versionAttribute.Value = "2.0";
        doc.SelectSingleNode("//rss")?.Attributes?.Append(versionAttribute);
        
        var permaLinkAttribute = doc.CreateAttribute("isPermaLink");
        permaLinkAttribute.Value = "false";
        doc.SelectSingleNode("//item/guid")?.Attributes?.Append(permaLinkAttribute);
        
        doc.Save(filePath);

        Console.WriteLine($"RSS file serialized as {filePath}");
    }

    public static RSS DeserializeXML(string path)
    {
        var xmlData = File.ReadAllText(path);
        var serializer = new XmlSerializer(typeof(RSS));

        using StringReader reader = new StringReader(xmlData);
        return (RSS)serializer.Deserialize(reader)!;
    }
}

public static class EnumExtension {
    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)       
        => self.Select((item, index) => (item, index));
}
