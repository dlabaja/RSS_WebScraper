using System.Net;

namespace RSS;

public class Server
{
    private static HttpListener listener;

    public Server()
    {
        listener = new HttpListener();
        listener.Prefixes.Add(Config.Url.EndsWith("/") ? Config.Url : Config.Url + "/");
        listener.Start();
        Console.WriteLine($"Listening for connections on {Config.Url}");
        
        var listenTask = HandleIncomingConnections();
        listenTask.GetAwaiter().GetResult();
        
        listener.Close();
    }

    async private static Task HandleIncomingConnections()
    {
        while (true)
        {
            var ctx = await listener.GetContextAsync();
            
            var req = ctx.Request;
            var resp = ctx.Response;
            
            Console.WriteLine(req.Url?.ToString());

            var path = $"{Directory.GetCurrentDirectory()}{req.Url?.LocalPath}";
            if (File.GetAttributes(path) != FileAttributes.Directory)
            {
                var data = await File.ReadAllBytesAsync(path);
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
            }

            resp.Close();
        }
    }
}
