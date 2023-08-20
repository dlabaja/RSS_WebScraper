using System.Xml.Serialization;

namespace RSS.Scrapers;

public class Picuki
{
    public async static void ScrapePicuki(string username)
    {
        var doc = Utils.GetHTMLDocument($"https://www.picuki.com/profile/{username}");
        var posts = doc.DocumentNode.SelectNodes("//div[contains(@class, 'box-photo')]");
        
        
    }
}

[XmlRoot("channel")]
public class Channel
{
    [XmlElement("title")]
    public string Title { get; set; }

    [XmlElement("description")]
    public string Description { get; set; }

    [XmlElement("link")]
    public string Link { get; set; }

    [XmlElement("image")]
    public Image Image { get; set; }

    [XmlElement("item")]
    public List<Item> Items { get; set; }
}

public class Image
{
    [XmlElement("url")]
    public string Url { get; set; }

    [XmlElement("title")]
    public string Title { get; set; }

    [XmlElement("link")]
    public string Link { get; set; }
}

public class Item
{
    [XmlElement("title")]
    public string Title { get; set; }

    [XmlElement("link")]
    public string Link { get; set; }

    [XmlElement("image")]
    public Image Image { get; set; }

    [XmlElement("description")]
    public string Description { get; set; }

    [XmlElement("pubDate")]
    public string PubDate { get; set; }
}
