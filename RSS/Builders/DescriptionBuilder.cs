using HtmlAgilityPack;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace RSS.Builders;

public class DescriptionBuilder
{
    protected StringBuilder Description = new StringBuilder();
    protected Media media;

    public DescriptionBuilder(Media media)
    {
        Description.Append("<![CDATA[");
        this.media = media;
    }

    public DescriptionBuilder AddSpan(string text, bool addBreak = true)
    {
        try
        {
            Description.Append($"<span>{text}</span>");
            if (addBreak) Description.Append("<br>");
        }
        catch {}

        return this;
    }

    public DescriptionBuilder AddSpanOrEmpty(string text, bool addIf, bool addBreak = true)
    {
        if (!addIf) return this;
        try
        {
            Description.Append($"<span>{text}</span>");
            if (addBreak) Description.Append("<br>");
        }
        catch {}

        return this;
    }

    public DescriptionBuilder AddBreak()
    {
        Description.Append("<br>");
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

    public DescriptionBuilder AddImage(string id, string url, string relativeMediaFolder)
    {
        media.Add(id, url);
        Description.Append($"<img src=\"{Config.Url}/{relativeMediaFolder}/{id}\"><br>");

        return this;
    }

    public DescriptionBuilder AddImages(IEnumerable<string> urls, string relativeMediaFolder)
    {
        foreach (var url in urls)
        {
            var id = Regex.IsMatch(url, @"\/media%2F(.*?)\.") ? Regex.Match(url, @"\/media%2F(.*?)\.").Groups[1].Value : url[^40..];
            media.Add(id, url);
            Description.Append($"<img src=\"{Config.Url}/{relativeMediaFolder}/{id}\"><br>");
        }

        return this;
    }

    public DescriptionBuilder AddVideos(IEnumerable<string> urls, string relativeMediaFolder)
    {
        foreach (var url in urls)
        {
            var id = Regex.IsMatch(url, @"[%\/]([A-Za-z0-9\-_]+)\.m") ? Regex.Match(url, @"[%\/]([A-Za-z0-9\-_]+)\.m").Groups[1].Value : url[^40..];
            media.Add(id, url);
            Description.Append($"<video controls><source src='{Config.Url}/{relativeMediaFolder}/{id}'></video><br>");
        }

        return this;
    }
    
    public DescriptionBuilder AddImages(string id, IEnumerable<string> urls, string relativeMediaFolder)
    {
        foreach (var (url, i) in urls.WithIndex())
        {
            id = $"{id}_{i}"; 
            media.Add(id, url);
            Description.Append($"<img src=\"{Config.Url}/{relativeMediaFolder}/{id}\"><br>");
        }

        return this;
    }

    public DescriptionBuilder AddVideos(string id, IEnumerable<string> urls, string relativeMediaFolder)
    {
        foreach (var (url, i) in urls.WithIndex())
        {
            id = $"{id}_{i}"; 
            media.Add(id, url);
            Description.Append($"<video controls><source src='{Config.Url}/{relativeMediaFolder}/{id}'></video><br>");
        }

        return this;
    }

    public DescriptionBuilder AddVideo(string id, string url, string relativeMediaFolder)
    {
        media.Add(id, url);
        Description.Append($"<video controls><source src='{Config.Url}/{relativeMediaFolder}/{id}'></video><br>");

        return this;
    }

    public DescriptionBuilder AddComments((List<string> usernames, List<string> messages) comments)
    {
        try
        {
            for (int i = 0; i < comments.usernames.Count; i++)
            {
                Description.Append($"<p><b>{comments.usernames[i].Trim()}</b><br>{comments.messages[i].Trim()}</p>");
            }
        }
        catch {}

        return this;
    }

    public DescriptionBuilder AddQuoteTweet(string? quoteHtml, string relativeMediaFolder)
    {
        if (quoteHtml == null)
            return this;

        var d = new HtmlDocument();
        d.LoadHtml(quoteHtml);
        var doc = d.DocumentNode;
        
        Description.Append("~<br><i>");
        Description.Append($"{doc.SelectSingleNode("//a[@class='fullname']").InnerText} | {TimeBuilder.ParseNitterTime(doc.SelectSingleNode("//span[@class='tweet-date']/a").GetAttributeValue("title", ""))}<br>");
        AddSpanOrEmpty(string.Join(", ", doc.SelectNodes("//div[@class='replying-to']")?.Select(x => x.InnerText) ?? Enumerable.Empty<string>()),
            doc.SelectNodes("//div[@class='replying-to']") != null);
        Description.Append(doc.SelectSingleNode("//div[@class='quote-text']").InnerText + "<br>");
        if (doc.SelectSingleNode("//a[@class='still-image']/img") != null)
        {
            var url = doc.SelectSingleNode("//a[@class='still-image']/img").GetAttributeValue("src", "");
            AddImage(url[^20..], Config.NitterInstance + url, relativeMediaFolder);
        }
        else if (doc.SelectSingleNode("//div[@class='attachment video-container']/video") != null)
        {
            var url = doc.SelectSingleNode("//div[@class='attachment video-container']/video").GetAttributeValue("data-url", "");
            AddVideo(url[^20..], Config.NitterInstance + url, relativeMediaFolder);
        }
        Description.Append("</i><br>");
        return this;
    }
    
    private static string RemoveTags(string input) => Regex.Replace(input, @"<[^>]*>", "");

    public override string ToString()
    {
        return Description.ToString();
    }
}
