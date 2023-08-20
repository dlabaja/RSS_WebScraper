using System.Text.Json;
using System;
using System.Net.Http;
using HtmlAgilityPack;
using System.Diagnostics;
using System.Text;
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

    public static void SerializeXML<T>(string siteName, string username, object o)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        if (!Directory.Exists($"{saveLocation}/{siteName}"))
        {
            Directory.CreateDirectory($"{saveLocation}/{siteName}");
        }

        using var writer = new StreamWriter($"{saveLocation}/{siteName}/{username}");
        serializer.Serialize(writer, o);
    }
}
