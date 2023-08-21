using System.Text;

namespace RSS;

public class DescriptionBuilder
{
    private StringBuilder Description = new StringBuilder();

    public DescriptionBuilder()
    {
        Description.Append("<![CDATA[");
    }

    public DescriptionBuilder AddSpan(string text)
    {
        Description.Append($"<span>{text}</span>");
        return this;
    }

    public DescriptionBuilder AddParagraph(string text)
    {
        // todo replace \n with <br>?
        Description.Append($"<p>{text}</p>");
        return this;
    }

    public DescriptionBuilder AddImage(string url)
    {
        //  <img src="img_girl.jpg" alt="Girl in a jacket" width="500" height="600"> 
        Description.Append($"<img src='{url}'>");
        return this;
    }

    public override string ToString()
    {
        Description.Append("]]>");
        return Description.ToString();
    }
}
