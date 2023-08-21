using System.Xml.Serialization;

namespace RSS.Scrapers;

public static class Picuki
{
    public static void ScrapePicuki(string username)
    {
        var doc = Utils.GetHTMLDocument($"https://www.picuki.com/profile/{username}").DocumentNode;
        var rss = new RSS();
        
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
                Description = new DescriptionBuilder()
                    .AddSpan(doc.SelectNodes("//span[@class='icon-globe-alt']//a")[i].InnerText)
                    .AddImage(doc.SelectNodes("//div[@class='photo']/a/img")[i].GetAttributeValue("src", ""))
                    .ToString()
                //PubDate = doc.SelectNodes("//div[@class='time']//span")[i].InnerText,
                /*MediaContent = new MediaContent{
                    Medium = "image",
                    Url = doc.SelectNodes("//div[@class='photo']/a/img")[i].GetAttributeValue("src", ""),
                    Width = 1000,
                    Height = 1000
                }*/
            };

            channel.Items.Add(item);
            rss.Channel = channel;
            
            Utils.SerializeXML<RSS>("picuki", username, rss);
        }
    }
}

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

public class Item
{
    [XmlElement("title")]
    public string Title { get; set; }

    [XmlElement("link")]
    public string Link { get; set; }

    [XmlElement("description")]
    public string Description { get; set; }

    /*[XmlElement("pubDate")]
    public string PubDate { get; set; }*/
}
