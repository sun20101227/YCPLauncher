using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace YCPLauncher.Services;

// ── Response Models ──────────────────────────────────────────────────────────


public class MatchCreateResponse
{
    // flat room_code
    [JsonPropertyName("room_code")]
    public string? RoomCode { get; set; }

    // data can be: {"room_code":"XXX"} OR "XXX" OR null
    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    // numeric or string status code from backend
    [JsonPropertyName("code")]
    public JsonElement? Code { get; set; }

    // stored by service, not from JSON
    public string? Raw { get; set; }

    public string? GetRoomCode()
    {
        // 1. top-level room_code
        if (!string.IsNullOrWhiteSpace(RoomCode)) return RoomCode;

        // 2. data field
        if (Data.HasValue)
        {
            var d = Data.Value;
            if (d.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                // data is a plain string → that IS the room code
                var s = d.GetString();
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
            else if (d.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                // data is an object, try common field names
                if (d.TryGetProperty("room_code", out var rc)) return rc.GetString();
                if (d.TryGetProperty("code", out var c2)) return c2.GetString();
                if (d.TryGetProperty("roomCode", out var rc2)) return rc2.GetString();
            }
        }
        return null;
    }
}

public class MatchJoinResponse
{
    [JsonPropertyName("success")]
    public bool? Success { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }
    [JsonPropertyName("code")]
    public JsonElement? Code { get; set; }

    /// <summary>True if success==true OR message indicates success OR code is 200/0.</summary>
    public bool IsSuccessful()
    {
        // Explicit backend decisions always win.
        if (Success.HasValue) return Success.Value;

        // 2. Explicit code from backend
        if (Code.HasValue)
        {
            if (Code.Value.ValueKind == JsonValueKind.Number)
            {
                int c = Code.Value.GetInt32();
                if (c == 200 || c == 0) return true;
                if (c != 200 && c != 0) return false; // Explicitly failed business logic
            }
            if (Code.Value.ValueKind == JsonValueKind.String)
            {
                string s = Code.Value.GetString() ?? "";
                if (s == "200" || s == "0") return true;
                if (s != "200" && s != "0" && s != "") return false;
            }
        }

        // Message fallback for legacy responses.
        string actualMsg = Message ?? Msg ?? "";
        if (!string.IsNullOrWhiteSpace(actualMsg))
        {
            if (actualMsg.Contains("成功", StringComparison.OrdinalIgnoreCase) ||
                actualMsg.Contains("success", StringComparison.OrdinalIgnoreCase) ||
                actualMsg.Contains("joined", StringComparison.OrdinalIgnoreCase) ||
                actualMsg.Contains("ok", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (actualMsg.Contains("失败", StringComparison.OrdinalIgnoreCase) ||
                actualMsg.Contains("已被占用", StringComparison.OrdinalIgnoreCase) ||
                actualMsg.Contains("不存在", StringComparison.OrdinalIgnoreCase) ||
                actualMsg.Contains("已满", StringComparison.OrdinalIgnoreCase) ||
                actualMsg.Contains("拒绝", StringComparison.OrdinalIgnoreCase) ||
                actualMsg.Contains("error", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Unknown response formats must fail closed.
        return false;
    }

    public string? DisplayMessage => Message ?? Msg;
}

public class MatchReadyResponse
{
    [JsonPropertyName("all_ready")]
    public bool AllReady { get; set; }
    [JsonPropertyName("has_host")]
    public bool HasHost { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>True if the ready call was accepted (message doesn’t indicate error).</summary>
    public bool IsSuccessful() =>
        Message == null ||
        !Message.Contains("失败", StringComparison.OrdinalIgnoreCase) &&
        !Message.Contains("error", StringComparison.OrdinalIgnoreCase) &&
        !Message.Contains("fail", StringComparison.OrdinalIgnoreCase);
}

public class MatchServerInfo
{
    [JsonPropertyName("ip")]
    public string Ip { get; set; } = "";
    [JsonPropertyName("port")]
    public int Port { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

public class MatchPlayerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    [JsonPropertyName("team")]
    public string Team { get; set; } = "";
    [JsonPropertyName("is_ready")]
    public bool IsReady { get; set; }
}

public class MatchStatusResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "waiting"; // "waiting" | "playing"
    [JsonPropertyName("players")]
    public MatchPlayerInfo[]? Players { get; set; }
    [JsonPropertyName("server")]
    public MatchServerInfo? Server { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("player_count")]
    public int PlayerCount { get; set; }
    [JsonPropertyName("has_host")]
    public bool HasHost { get; set; }
}

public class MatchStartResponse
{
    [JsonPropertyName("server_ip")]
    public string? ServerIp { get; set; }
    [JsonPropertyName("server_port")]
    public int ServerPort { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class MatchRoomInfo
{
    [JsonPropertyName("room_code")]
    public string RoomCode { get; set; } = "";
    [JsonPropertyName("player_count")]
    public int PlayerCount { get; set; }
    [JsonPropertyName("has_host")]
    public bool HasHost { get; set; }
}

// ── Service ──────────────────────────────────────────────────────────────────

public class MatchService
{
    private const string BaseUrl = "https://cs2.yachiyo8000.cn";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public MatchService(string token)
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<T?> PostAsync<T>(string path, object body, Action<string>? rawCapture = null)
    {
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpResponseMessage resp;
        try { resp = await _http.PostAsync($"{BaseUrl}{path}", content); }
        catch (Exception ex) { throw new Exception($"网络连接失败：{ex.Message}"); }

        var raw = await resp.Content.ReadAsStringAsync();
        rawCapture?.Invoke(raw);

        if (!resp.IsSuccessStatusCode)
        {
            var errMsg = $"HTTP {(int)resp.StatusCode}";
            try
            {
                var errJson = JsonDocument.Parse(raw);
                if (errJson.RootElement.TryGetProperty("message", out var msg))
                    errMsg = msg.GetString() ?? errMsg;
            } catch { }
            throw new Exception(errMsg);
        }

        if (!raw.TrimStart().StartsWith('{') && !raw.TrimStart().StartsWith('[')
            && !raw.TrimStart().StartsWith('"') && raw != "null")
            throw new Exception($"服务器未就绪（HTTP {(int)resp.StatusCode}）");

        try
        {
            var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("data", out var dataElement) && dataElement.ValueKind != JsonValueKind.Null)
            {
                // If it's a wrapper, deserialize the data element
                return JsonSerializer.Deserialize<T>(dataElement.GetRawText(), JsonOpts);
            }
        }
        catch { }

        return JsonSerializer.Deserialize<T>(raw, JsonOpts);
    }

    private async Task<T?> GetAsync<T>(string path)
    {
        HttpResponseMessage resp;
        try { resp = await _http.GetAsync($"{BaseUrl}{path}"); }
        catch (Exception ex) { throw new Exception($"网络连接失败：{ex.Message}"); }

        var raw = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            var errMsg = $"HTTP {(int)resp.StatusCode}";
            try
            {
                var errJson = JsonDocument.Parse(raw);
                if (errJson.RootElement.TryGetProperty("message", out var msg))
                    errMsg = msg.GetString() ?? errMsg;
            } catch { }
            throw new Exception(errMsg);
        }

        if (!raw.TrimStart().StartsWith('{') && !raw.TrimStart().StartsWith('[')
            && !raw.TrimStart().StartsWith('"') && raw != "null")
            throw new Exception($"服务器未就绪（HTTP {(int)resp.StatusCode}）");

        try
        {
            var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("data", out var dataElement) && dataElement.ValueKind != JsonValueKind.Null)
            {
                // If it's a wrapper, deserialize the data element
                return JsonSerializer.Deserialize<T>(dataElement.GetRawText(), JsonOpts);
            }
        }
        catch { }

        return JsonSerializer.Deserialize<T>(raw, JsonOpts);
    }

    /// <summary>创建房间，返回 room_code</summary>
    public async Task<MatchCreateResponse?> CreateAsync()
    {
        string? raw = null;
        var result = await PostAsync<MatchCreateResponse>("/api/v1/match/create", new { }, r => raw = r);
        if (result != null) result.Raw = raw;
        return result;
    }

    /// <summary>加入房间，team: "T" / "CT" / "Host"</summary>
    public Task<MatchJoinResponse?> JoinAsync(string roomCode, string team)
        => PostAsync<MatchJoinResponse>("/api/v1/match/join", new { room_code = roomCode, team });

    /// <summary>切换准备状态</summary>
    public Task<MatchReadyResponse?> ReadyAsync(string roomCode)
        => PostAsync<MatchReadyResponse>("/api/v1/match/ready", new { room_code = roomCode });

    /// <summary>获取房间当前状态（轮询用）</summary>
    public Task<MatchStatusResponse?> StatusAsync(string roomCode)
        => GetAsync<MatchStatusResponse>($"/api/v1/match/status?room_code={roomCode}");

    /// <summary>启动比赛（仅主席或无主席时任何人可调）</summary>
    public Task<MatchStartResponse?> StartAsync(string roomCode)
        => PostAsync<MatchStartResponse>("/api/v1/match/start", new { room_code = roomCode });

    /// <summary>离开指定房间</summary>
    public Task<object?> LeaveAsync(string roomCode)
        => PostAsync<object>("/api/v1/match/leave", new { room_code = roomCode });

    /// <summary>尝试退出当前任意房间（不知道 room_code 时使用）</summary>
    public async Task TryLeaveCurrentAsync()
    {
        try { await PostAsync<object>("/api/v1/match/leave", new { }); } catch { }
    }

    /// <summary>列出等待中的公开房间（无需 token）</summary>
    public static async Task<MatchRoomInfo[]?> ListAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        var raw = await http.GetStringAsync($"{BaseUrl}/api/v1/match/list");
        return JsonSerializer.Deserialize<MatchRoomInfo[]>(raw, JsonOpts);
    }
}
