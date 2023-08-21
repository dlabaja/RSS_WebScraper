using System.Net;

namespace RSS;

public class Server
{
    static Server()
    {
        string baseUrl = "http://localhost:8080/";
        using var httpListener = new HttpListener();
        httpListener.Prefixes.Add(baseUrl);
        httpListener.Start();

        Console.WriteLine("Server is listening at " + baseUrl);

        while (true)
        {
            HttpListenerContext context = httpListener.GetContext();
            ThreadPool.QueueUserWorkItem(ProcessRequest, new object[] { context, Utils.saveLocation });
        }

    }

    private static void ProcessRequest(object? state)
    {
        var stateArray = (object[])state!;
        HttpListenerContext context = (HttpListenerContext)stateArray[0];
        string filesPath = (string)stateArray[1];

        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        var filePath = request.Url?.LocalPath.Substring(1); // Odstranění počátečního lomítka

        string fullFilePath = Path.Combine(filesPath, filePath);

        if (File.Exists(fullFilePath))
        {
            try
            {
                byte[] buffer = File.ReadAllBytes(fullFilePath);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.StatusDescription = "Internal Server Error";
                Console.WriteLine("Error: " + ex.Message);
            }
        }
        else
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            response.StatusDescription = "File not found";
        }

        response.Close();
    }
}
