using System.Xml.Serialization;

namespace RSS;

[XmlRoot("rss")]
public class RSS
{
    [XmlElement("channel")]
    public Channel Channel { get; set; }
}

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

    [XmlElement("description")]
    public string Description { get; set; }

    [XmlElement("author")]
    public string Author { get; set; }

    [XmlElement("guid")]
    public string GUID { get; set; }

    [XmlElement("pubDate")]
    public string PubDate { get; set; }
}
