namespace RSS.Scrapers;

public class Website
{
    private string appFolder;
    protected string usernameFolder;
    private string imgFolder;
    protected string relativeImgFolder;

    protected string siteName;
    protected string link;
    protected string username;

    protected void LoadSiteData()
    {
        appFolder = Path.Combine(Directory.GetCurrentDirectory(), siteName);
        usernameFolder = Path.Combine(appFolder, username);
        imgFolder = Path.Combine(usernameFolder, "media");
        relativeImgFolder = Path.Combine(siteName, username, "media");;

        Directory.CreateDirectory(imgFolder);
    }
}
