namespace RSS.Scrapers;

public class Website
{
    protected string appFolder;
    protected string usernameFolder;
    protected string imgFolder;
    protected string relativeImgFolder;

    public string siteName;
    protected string link;
    public string username;

    protected void LoadSiteData()
    {
        appFolder = $"{Directory.GetCurrentDirectory()}/{siteName}";
        usernameFolder = $"{appFolder}/{username}";
        imgFolder = $"{usernameFolder}/images";
        relativeImgFolder = $"{siteName}/{username}/images";

        Directory.CreateDirectory(imgFolder);
    }
}
