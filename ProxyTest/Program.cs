using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        string[] proxies = new[]
        {
            "https://ghp.ci/",
            "https://gh-proxy.com/",
            "https://github.moeyy.xyz/",
            "https://kkgithub.com/",
            "https://mirror.ghproxy.com/"
        };
        
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("YCPLauncher/1.1.2");

        foreach (var p in proxies)
        {
            try
            {
                string url = p.Contains("kkgithub") 
                    ? "https://kkgithub.com/sun20101227/YCPLauncher/releases/download/v1.1.2/YCPInstaller.exe"
                    : p + "https://github.com/sun20101227/YCPLauncher/releases/download/v1.1.2/YCPInstaller.exe";
                
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await client.SendAsync(request);
                Console.WriteLine($"{p} - OK ({(int)response.StatusCode})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{p} - Failed: {ex.Message}");
            }
        }
    }
}
