using System.Text.Json.Serialization;

namespace YCPLauncher.Models;

public class UpdateResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("update_available")]
    public bool UpdateAvailable { get; set; }

    [JsonPropertyName("latest_version")]
    public string LatestVersion { get; set; } = "";

    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; set; } = "";

    [JsonPropertyName("file_size")]
    public long FileSize { get; set; }

    [JsonPropertyName("release_notes")]
    public string ReleaseNotes { get; set; } = "";
}
