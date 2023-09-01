using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace RSS.Builders;

public class DescriptionBuilder
{
    private StringBuilder Description = new StringBuilder();
    private Media media;

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
        Description.Append($"<img src=\"{Config.Url}/{relativeMediaFolder}/{media.GetUniqueName(id, url)}\"><br>");

        return this;
    }

    public DescriptionBuilder AddImages(string id, IEnumerable<string> urls, string relativeMediaFolder)
    {
        foreach (var url in urls)
        {
            media.Add(id, url);
            Description.Append($"<img src=\"{Config.Url}/{relativeMediaFolder}/{media.GetUniqueName(id, url)}\"><br>");
        }

        return this;
    }

    public DescriptionBuilder AddVideos(string id, IEnumerable<string> urls, string relativeMediaFolder)
    {
        foreach (var url in urls)
        {
            media.Add(id, url);
            Description.Append($"<video controls><source src='{Config.Url}/{relativeMediaFolder}/{media.GetUniqueName(id, url)}'></video><br>");
        }

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


    private static string RemoveTags(string input) => Regex.Replace(input, @"<[^>]*>", "");

    public override string ToString()
    {
        return Description.ToString();
    }
}
