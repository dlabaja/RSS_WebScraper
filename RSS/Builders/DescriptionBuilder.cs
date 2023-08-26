using HtmlAgilityPack;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace RSS.Builders;

public class DescriptionBuilder
{
    private StringBuilder Description = new StringBuilder();

    public DescriptionBuilder()
    {
        Description.Append("<![CDATA[");
    }

    public DescriptionBuilder AddSpan(string text)
    {
        try
        {
            Description.Append($"<span>{RemoveTags(text)}</span><br>");
        }
        catch {}

        return this;
    }

    public DescriptionBuilder AddSpanOrNot(string text, bool addIf)
    {
        if (!addIf) return this;
        try
        {
            Description.Append($"<span>{RemoveTags(text)}</span><br>");
        }
        catch {}

        return this;
    }

    public DescriptionBuilder AddParagraph(string text)
    {
        try
        {
            // todo replace \n with <br>?
            Description.Append($"<p>{RemoveTags(text)}</p>");
        }
        catch {}

        return this;
    }

    public DescriptionBuilder AddImage(string url, string relativeImgFolder, string id)
    {
        try
        {
            DownloadImage(url, $"{Directory.GetCurrentDirectory()}/{relativeImgFolder}", id);
            Description.Append($"<img src=\"{Config.Url}/{relativeImgFolder}/{id}\">");
        }
        catch {}

        return this;
    }

    public DescriptionBuilder AddImages(IEnumerable<string> urls, string relativeImgFolder, string id)
    {
        try
        {
            foreach (var (url, i) in urls.WithIndex())
            {
                try
                {
                    DownloadImage(url, $"{Directory.GetCurrentDirectory()}/{relativeImgFolder}", $"{id}_{i}");
                    Description.Append($"<img src=\"{Config.Url}/{relativeImgFolder}/{id}_{i}\">");
                }
                catch {}
            }
        }
        catch {}

        return this;
    }

    private static void DownloadImage(string url, string path, string id)
    {
        // create new curl-impersonate process
        var process = new Process{
            StartInfo = new ProcessStartInfo{
                FileName = "/bin/bash",
                Arguments = $"{Config.CurlImpersonateScriptLocation} \"{url}\" --output \"{path}/{id}\"",
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

    public DescriptionBuilder AddComments((List<string> usernames, List<string> messages) comments)
    {
        try
        {
            for (int i = 0; i < comments.usernames.Count; i++)
            {
                AddParagraph($"<b>{comments.usernames[i].Trim()}</b><br>{comments.messages[i].Trim()}");
            }
        }
        catch {}

        return this;
    }

    private string RemoveTags(string input) => Regex.Replace(input, @"<[^>]*>", "");

    public override string ToString()
    {
        return Description.ToString();
    }
}
