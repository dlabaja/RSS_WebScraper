using System.Xml.Serialization;

namespace RSS.Scrapers;

public static class Picuki
{
    public static void ScrapePicuki(string username)
    {
        var doc = Utils.GetHTMLDocument($"https://www.picuki.com/profile/{username}").DocumentNode;

        var channel = new Channel{
            Title = doc.SelectSingleNode("//h1[@class='profile-name-top']").InnerText,
            Link = $"https://www.picuki.com/profile/{username}",
            Items = new List<Item>(),
            Description = doc.SelectSingleNode("//div[@class='profile-description']").InnerText.Trim(),
        };

        channel.Image = new Image{
            Url = doc.SelectSingleNode("//img[@class='profile-avatar-image']").GetAttributeValue("src", ""),
            Title = channel.Title,
            Link = channel.Link
        };

        for (int i = 0; i < doc.SelectNodes("//div[@class='box-photo']").Count; i++)
        {
            var item = new Item{
                Title = doc.SelectNodes("//div[@class='photo-description']")[i].InnerText.Trim(),
                Link = channel.Link,
                Description = doc.SelectNodes("//span[@class='icon-globe-alt']//a")[i].InnerText,
                PubDate = doc.SelectNodes("//div[@class='time']//span")[i].InnerText,
                /*Image = new Image{
                    Url = doc.SelectNodes("//div[@class='photo']/a/img")[i].GetAttributeValue("src", ""),
                    Link = channel.Link,
                    Title = channel.Title
                }*/
            };

            channel.Items.Add(item);
            
            Utils.SerializeXML<Channel>("picuki", username, channel);
        }
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
    
    [XmlElement("atom:link")]
    public AtomLink AtomLink { get; set; }

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

public class AtomLink
{
    [XmlElement("href")]
    public string Href { get; set; }

    [XmlElement("rel")]
    public string Rel { get; } = "self";
    
    [XmlElement("type")]
    public string Type { get; } = "application/rss+xml";
}

public class MediaContent
{
    [XmlAttribute("medium")]
    public string Medium { get; set; }

    [XmlAttribute("url")]
    public string Url { get; set; }

    [XmlAttribute("width")]
    public int Width { get; set; }

    [XmlAttribute("height")]
    public int Height { get; set; }
}

public class Item
{
    [XmlElement("title")]
    public string Title { get; set; }

    [XmlElement("link")]
    public string Link { get; set; }

    [XmlElement("media:content")]
    public MediaContent MediaContent { get; set; }

    [XmlElement("description")]
    public string Description { get; set; }

    [XmlElement("pubDate")]
    public string PubDate { get; set; }
}
