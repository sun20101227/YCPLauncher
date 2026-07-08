Add-Type -AssemblyName System.Net.Http

$installer = "C:\Users\ronfu\Desktop\cient\SetupOutput\YCPInstaller.exe"
$version = "0.0.11"
$filename = "YCPInstaller_v0.0.11.exe"
$apiKey = "ycp-admin-key-2026"
$url = "https://cs2.yachiyo8000.cn/api/v1/releases/upload_chunk.php"

$chunkSize = 1MB
$fileStream = [System.IO.File]::OpenRead($installer)
$buffer = New-Object byte[] $chunkSize
$totalChunks = [math]::Ceiling($fileStream.Length / $chunkSize)
$chunkIndex = 0

$client = New-Object System.Net.Http.HttpClient
$client.DefaultRequestHeaders.Add("X-API-Key", $apiKey)
$client.Timeout = [System.TimeSpan]::FromMinutes(30)

while ($bytesRead = $fileStream.Read($buffer, 0, $chunkSize)) {
    Write-Host "Uploading chunk $chunkIndex of $totalChunks ($bytesRead bytes)..."
    
    $content = New-Object System.Net.Http.MultipartFormDataContent
    $content.Add((New-Object System.Net.Http.StringContent($version)), "version")
    $content.Add((New-Object System.Net.Http.StringContent($filename)), "filename")
    $content.Add((New-Object System.Net.Http.StringContent($chunkIndex.ToString())), "chunk_index")
    $content.Add((New-Object System.Net.Http.StringContent($totalChunks.ToString())), "total_chunks")
    
    $notes = if ($chunkIndex -eq 0) { "YCP Launcher 0.0.10 (Beta) - 修复亚克力黑边Bug" } else { "chunk" }
    $content.Add((New-Object System.Net.Http.StringContent($notes)), "release_notes")
    
    $fileContent = New-Object System.Net.Http.ByteArrayContent($buffer, 0, $bytesRead)
    $content.Add($fileContent, "file", $filename)
    
    $response = $client.PostAsync($url, $content).Result
    $result = $response.Content.ReadAsStringAsync().Result
    Write-Host "Result: $result"
    
    $chunkIndex++
}

$fileStream.Close()
Write-Host "Upload finished."
