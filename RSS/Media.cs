using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RSS;

public class Media
{
    private string path;
    private readonly ConcurrentDictionary<string, string> mediaDict = new ConcurrentDictionary<string, string>();

    public Media(string siteName)
    {
        path = Path.Combine(Directory.GetCurrentDirectory(), siteName, "media.json");
        if (!File.Exists(path))
            return;
        try
        {
            mediaDict = JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(File.ReadAllText(path)) ?? new ConcurrentDictionary<string, string>();
        }
        catch (JsonException)
        {
            File.Delete(path);
        }
    }

    public void SaveJson()
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

        mediaDict.TryAdd(id, url);
    }

    public void DownloadAllMedia()
    {
        SaveJson();
        new Thread(o =>
        {
            var mediaFolder = Path.Combine(Path.GetDirectoryName(path)!, "media");
            foreach (var (id, _url) in mediaDict)
            {
                var url = ReplaceUrlPlaceholders(_url);
                try
                {
                    if (File.Exists(Path.Combine(mediaFolder, id))) continue;
                    if (url.Contains(".m3u8"))
                    {
                        ConvertToMp4(url, mediaFolder, id);
                        continue;
                    }

                    DownloadMedia(url, mediaFolder, id);

                    if (!path.Contains("proxitok/media.json")) continue;

                    if (Regex.IsMatch(id, @"^\d+_\d+$"))
                    {
                        CompressImages(Path.Combine(mediaFolder, id));
                    }
                    else if (Regex.IsMatch(id, @"^\d+$"))
                    {
                        CompressVideo(Path.Combine(mediaFolder, id));
                    }

                }
                catch {}
            }
        }).Start();
    }

    private static void CompressImages(string path)
    {
        // create new ffmpeg process
        var process = new Process{
            StartInfo = new ProcessStartInfo{
                FileName = Config.FFmpegLocation,
                Arguments = $"-i {path} -q:v 0 -f webp {path + "c"} -y",
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
        File.Replace(path + "c", path, null);
    }

    private static void CompressVideo(string path)
    {
        // create new ffmpeg process
        var process = new Process{
            StartInfo = new ProcessStartInfo{
                FileName = Config.FFmpegLocation,
                Arguments = $"-i {path} -vf \"scale=trunc(iw/4)*2:trunc(ih/4)*2\" -c:v libx265 -crf 28 -f mp4 {path + "c"} -y",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false
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
        File.Replace(path + "c", path, null);
    }

    private static string ReplaceUrlPlaceholders(string url)
    {
        url = url.Replace("{PROXITOK_URL}", Config.ProxiTokInstance);
        url = url.Replace("{NITTER_URL}", Config.NitterInstance);
        return url;
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
