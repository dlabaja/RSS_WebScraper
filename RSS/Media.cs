using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace RSS;

public class Media
{
    private string path;
    private readonly Dictionary<string, string> mediaDict = new Dictionary<string, string>();

    public Media(string siteName, string username)
    {
        path = Path.Combine(Directory.GetCurrentDirectory(), siteName, username, "media.json");
        if (File.Exists(path))
        {
            mediaDict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? new Dictionary<string, string>();
        }
    }

    private void SaveJson()
    {
        var jsonString = JsonSerializer.Serialize(mediaDict,
            new JsonSerializerOptions{
                WriteIndented = true
            });

        File.WriteAllText(path, jsonString);
    }

    public void Add(string id, string url)
    {
        if (mediaDict.ContainsKey(id))
        {
            return;
        }

        mediaDict.Add(id, url);
    }

    public void DownloadAllMedia()
    {
        SaveJson();
        new Thread(o =>
        {
            var mediaFolder = Path.Combine(Path.GetDirectoryName(path)!, "media");
            foreach (var (id, url) in mediaDict)
            {
                try
                {
                    if (File.Exists(Path.Combine(mediaFolder, id))) continue;
                    if (url.Contains(".m3u8"))
                    {
                        ConvertToMp4(url, mediaFolder, id);
                        continue;
                    }

                    DownloadMedia(url, mediaFolder, id);
                }
                catch {}
            }
        }).Start();
    }

    private static void ConvertToMp4(string url, string path, string id)
    {
        // create new ffmpeg process
        var process = new Process{
            StartInfo = new ProcessStartInfo{
                FileName = Config.FFmpegLocation,
                Arguments = $"-i {url} -f mp4 {Path.Combine(path, id)}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                output.Append(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.WaitForExit();
    }

    private static void DownloadMedia(string url, string path, string id)
    {
        // create new curl-impersonate process
        var process = new Process{
            StartInfo = new ProcessStartInfo{
                FileName = "/bin/bash",
                Arguments = $"{Config.CurlImpersonateScriptLocation} \"{url}\" --output \"{Path.Combine(path, id)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                output.Append(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.WaitForExit();
    }
}
