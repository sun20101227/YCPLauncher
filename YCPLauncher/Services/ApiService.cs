using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using YCPLauncher.Models;

namespace YCPLauncher.Services;

public class ApiService
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };
    private readonly string _baseUrl;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService()
    {
        var cfg = ConfigService.GetConfig();
        _baseUrl = cfg.ApiBaseUrl.TrimEnd('/');

        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(cfg.ApiTimeout)
        };
    }

    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        var payload = JsonSerializer.Serialize(new { username, password });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync($"{_baseUrl}/api/auth.php", content);
        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<LoginResponse>(json, JsonOpts) ?? new LoginResponse { Error = "解析响应失败" };
    }

    public async Task<ServersResponse> GetServersAsync(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/servers.php");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<ServersResponse>(json, JsonOpts) ?? new ServersResponse { Error = "解析响应失败" };
    }

    public async Task<PlayerInfo?> ValidateTokenAsync(string token)
    {
        try
        {
            var result = await GetServersAsync(token);
            // If we can reach the servers endpoint without error, token is valid
            // The server may return player info in future; for now we just check connectivity
            if (result.Error == null)
                return null; // Token valid, but player info comes from stored login
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<DashboardResponse?> GetDashboardAsync(string token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/v1/dashboard.php");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DashboardResponse>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<UpdateResponse?> CheckForUpdateAsync(string currentVersion)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/sun20101227/YCPLauncher/releases/latest");
            request.Headers.Add("User-Agent", "YCPLauncher");
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            string tagName = root.GetProperty("tag_name").GetString() ?? "";
            string body = root.GetProperty("body").GetString() ?? "";
            string latestVersion = tagName.TrimStart('v', 'V');
            
            // 简单比较版本号
            string cleanCurrent = currentVersion.Replace(" Beta", "").TrimStart('v', 'V');
            bool updateAvailable = false;
            if (Version.TryParse(latestVersion, out var latest) && Version.TryParse(cleanCurrent, out var current))
            {
                updateAvailable = latest > current;
            }
            else
            {
                updateAvailable = latestVersion != cleanCurrent;
            }

            var assets = root.GetProperty("assets");
            string downloadUrl = "";
            long fileSize = 0;
            if (assets.GetArrayLength() > 0)
            {
                var asset = assets[0];
                downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                fileSize = asset.GetProperty("size").GetInt64();
            }

            return new UpdateResponse
            {
                Code = 200,
                UpdateAvailable = updateAvailable,
                LatestVersion = latestVersion,
                DownloadUrl = downloadUrl,
                FileSize = fileSize,
                ReleaseNotes = body
            };
        }
        catch
        {
            return null;
        }
    }
}
