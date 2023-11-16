using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RSS;

public class Media
{
    private string pathToJson;
    private string sitename;
    private ConcurrentDictionary<string, string> mediaDict = new ConcurrentDictionary<string, string>();

    public Media(string siteName)
    {
        pathToJson = Path.Combine(Directory.GetCurrentDirectory(), siteName, "media.json");
        sitename = siteName;
        if (!File.Exists(pathToJson))
            return;
        try
        {
            mediaDict = JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(File.ReadAllText(pathToJson)) ?? new ConcurrentDictionary<string, string>();
        }
        catch (JsonException)
        {
            File.Delete(pathToJson);
        }
    }

    public void SaveJson()
    {
        var jsonString = JsonSerializer.Serialize(mediaDict,
            new JsonSerializerOptions{
                WriteIndented = true
            });

        File.WriteAllText(pathToJson, jsonString);
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
        
        var mediaFolder = Path.Combine(Path.GetDirectoryName(pathToJson)!, "media");
        RemoveEmptyFiles(mediaFolder);
        
        var xml_content = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), sitename, "rss.xml"));
        new Thread(o =>
        {
            var _mediaDict = mediaDict;
            foreach (var (id, _url) in mediaDict)
            {
                if (!xml_content.Contains($"{Config.Url}/{sitename}/media/{id}")) // removes old media to save space
                {
                    File.Delete(Path.Combine(mediaFolder, id));
                    _mediaDict.Remove(id, out _);
                    continue;
                }

                var url = ReplaceUrlPlaceholders(_url);
                try
                {
                    if (File.Exists(Path.Combine(mediaFolder, id))) continue;
                    
                    DownloadMedia(url, mediaFolder, id);

                    if (!pathToJson.Contains("proxitok/media.json")) continue;

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

            mediaDict = _mediaDict;
            SaveJson();
        }).Start();
    }

    private static void RemoveEmptyFiles(string mediaFolderPath)
    {
        foreach (var path in Directory.GetFiles(mediaFolderPath))
        {
            if (new FileInfo(path).Length == 0)
            {
                File.Delete(path);
            }
        }
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
                Arguments = $"-i {path} -vcodec libvpx -vf \"scale=trunc(iw/4)*2:trunc(ih/4)*2\" -f webm -threads 12 {path + "c"} -y",
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

    public static string ReplaceUrlPlaceholders(string url)
    {
        url = url.Replace("{PROXITOK_URL}", Config.ProxiTokInstance);
        url = url.Replace("{NITTER_URL}", Config.NitterInstance);
        return url;
    }

    public static void ConvertToMp4(string url, string path, string id)
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
