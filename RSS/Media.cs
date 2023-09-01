using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace RSS;

public class Media
{
    private string path;
    private readonly Dictionary<string, List<string>> mediaDict = new Dictionary<string, List<string>>();

    public Media(string siteName, string username)
    {
        path = Path.Combine(Directory.GetCurrentDirectory(), siteName, username, "media.json");
        if (File.Exists(path))
        {
            mediaDict = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(path)) ?? new Dictionary<string, List<string>>();
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
            mediaDict[id].Add(url);
            return;
        }

        mediaDict.Add(id, new List<string>{url});
    }

    public string GetUniqueName(string id, string url)
    {
        return mediaDict.ContainsKey(id) ? $"{id}_{mediaDict[id].IndexOf(url)}" : id;
    }

    public void Add(string id, List<string> urls)
    {
        if (mediaDict.ContainsKey(id))
        {
            mediaDict[id].AddRange(urls);
            return;
        }

        mediaDict.Add(id, urls);
    }

    public void DownloadAllMedia()
    {
        SaveJson();
        new Thread(o =>
        {
            var mediaFolder = Path.Combine(Path.GetDirectoryName(path)!, "media");
            foreach (var key in mediaDict.Keys)
            {
                foreach (var (url, i) in mediaDict[key].WithIndex())
                {
                    try
                    {
                        var id = $"{key}_{i}";
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
            }

            // check if files have correct format and are not a HTML page
            foreach (var file in Directory.GetFiles(mediaFolder))
            {
                try
                {
                    if (File.ReadAllText(file).Contains("<!DOCTYPE html>"))
                        File.Delete(file);
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
