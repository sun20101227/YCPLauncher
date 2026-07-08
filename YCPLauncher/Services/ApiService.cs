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
            // Note: Update API is on cs2.yachiyo8000.cn domain as specified by user
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://cs2.yachiyo8000.cn/api/v1/launcher/version.php?current={currentVersion}");
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UpdateResponse>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }
}
