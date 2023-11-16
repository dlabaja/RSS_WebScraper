namespace RSS;

public static class Cookies
{
    public const string Nitter = "hlsPlayback=on";
    public static readonly string? Invidious;

    static Cookies()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "cookies");
        Directory.CreateDirectory(path);

        var invidious_path = Path.Combine(path, "invidious.txt");
        if (File.Exists(invidious_path))
        {
            Invidious = File.ReadLines(invidious_path).FirstOrDefault()?.Trim();
        }
        else
        {
            File.Create(invidious_path);
        }
    }
}
