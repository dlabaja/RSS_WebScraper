using RSS.Scrapers;

namespace RSS;

public class Program
{
    private static void Main()
    {
        Config.LoadConfig();
        new Thread(o => new Server());

        foreach (var value in Config.SitesAndUsernames["picuki"])
        {
            new Picuki("https://www.picuki.com/profile", value, "picuki");
        }
    }
}
