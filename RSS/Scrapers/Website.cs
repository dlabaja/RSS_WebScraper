using HtmlAgilityPack;

namespace RSS.Scrapers;

public class Website
{
    protected string appFolder;
    protected string usernameFolder;
    protected string imgFolder;

    public string siteName;
    protected string link;
    public string username;

    protected Website(string link, string username, string siteName)
    {
        this.link = link;
        this.siteName = siteName;
        this.username = username;
        
        appFolder = $"{Directory.GetCurrentDirectory()}/{this.siteName}";
        usernameFolder = $"{appFolder}/{username}";
        imgFolder = $"{usernameFolder}/images";
        
        Directory.CreateDirectory(imgFolder);
    }
}
