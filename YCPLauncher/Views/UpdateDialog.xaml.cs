using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using YCPLauncher.Models;

namespace YCPLauncher.Views;

public partial class UpdateDialog : Window
{
    private readonly UpdateResponse _updateInfo;
    private bool _isDownloading = false;
    private bool _canClose = false;

    public UpdateDialog(UpdateResponse updateInfo)
    {
        InitializeComponent();
        _updateInfo = updateInfo;
        
        TxtVersion.Text = $"检测到新版本: v{_updateInfo.LatestVersion}";
        TxtReleaseNotes.Text = StripMarkdown(_updateInfo.ReleaseNotes ?? "");
    }

    private static string StripMarkdown(string md)
    {
        // Remove headings markers (###, ##, #)
        md = System.Text.RegularExpressions.Regex.Replace(md, @"^#{1,6}\s+", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        // Remove bold/italic (**text** / *text*)
        md = System.Text.RegularExpressions.Regex.Replace(md, @"\*{1,2}(.+?)\*{1,2}", "$1");
        // Remove inline code (`code`)
        md = System.Text.RegularExpressions.Regex.Replace(md, @"`(.+?)`", "$1");
        return md.Trim();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_canClose)
        {
            // Mandatory update, cannot close
            e.Cancel = true;
        }
    }

    private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (_isDownloading) return;
        
        _isDownloading = true;
        BtnUpdate.IsEnabled = false;
        BtnUpdate.Content = "正在开启多线程极速下载...";
        ProgressGrid.Visibility = Visibility.Visible;

        try
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"YachiyoCup_Installer_v{_updateInfo.LatestVersion}.exe");
            
            string originalUrl = _updateInfo.DownloadUrl;
            string[] proxyPrefixes = new[] {
                "https://gh-proxy.com/",
                "https://mirror.ghproxy.com/",
                "https://ghp.ci/"
            };

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            using var checkClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
            checkClient.DefaultRequestHeaders.UserAgent.ParseAdd("YCPLauncher/" + App.CurrentVersion);

            HttpResponseMessage? checkResponse = null;
            string? downloadUrl = null;

            var testUrls = new System.Collections.Generic.List<string>();
            if (originalUrl != null && originalUrl.Contains("github.com"))
            {
                foreach (var p in proxyPrefixes) testUrls.Add(p + originalUrl);
            }
            testUrls.Add(originalUrl ?? ""); // always add direct github as fallback

            using var cts = new System.Threading.CancellationTokenSource();
            var proxyTasks = testUrls.Select(async url =>
            {
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                var resp = await checkClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                resp.EnsureSuccessStatusCode();
                return new { Url = url, Response = resp };
            }).ToList();

            while (proxyTasks.Count > 0)
            {
                var completedTask = await Task.WhenAny(proxyTasks);
                proxyTasks.Remove(completedTask);
                try
                {
                    var result = await completedTask;
                    checkResponse = result.Response;
                    downloadUrl = result.Url;
                    cts.Cancel(); // cancel the other pending requests
                    break;
                }
                catch
                {
                    // failed, wait for the next one
                }
            }

            if (checkResponse == null || downloadUrl == null)
            {
                // all failed, default to github directly anyway to let chunk downloader try
                downloadUrl = originalUrl;
                checkResponse = await checkClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                checkResponse.EnsureSuccessStatusCode();
            }

            long totalBytes = checkResponse.Content.Headers.ContentLength ?? -1L;
            bool canReportProgress = totalBytes != -1;
            
            // Check if server supports Range requests
            bool supportsRange = checkResponse.Headers.AcceptRanges.Contains("bytes");

            if (canReportProgress && supportsRange && totalBytes > 1024 * 1024)
            {
                int numberOfThreads = 8;
                long chunkSize = totalBytes / numberOfThreads;
                long totalRead = 0;
                
                using (var fileHandle = File.OpenHandle(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, FileOptions.Asynchronous))
                {
                    RandomAccess.SetLength(fileHandle, totalBytes);
                    
                    var tasks = new System.Collections.Generic.List<Task>();
                    
                    for (int i = 0; i < numberOfThreads; i++)
                    {
                        int chunkIndex = i;
                        tasks.Add(Task.Run(async () =>
                        {
                            long start = chunkIndex * chunkSize;
                            long end = (chunkIndex == numberOfThreads - 1) ? totalBytes - 1 : start + chunkSize - 1;
                            
                            var chunkHandler = new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator };
                            using var client = new HttpClient(chunkHandler) { Timeout = TimeSpan.FromSeconds(60) };
                            client.DefaultRequestHeaders.UserAgent.ParseAdd("YCPLauncher/" + App.CurrentVersion);
                            var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);
                            
                            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                            response.EnsureSuccessStatusCode();
                            
                            using var stream = await response.Content.ReadAsStreamAsync();
                            byte[] buffer = new byte[131072]; // 128KB
                            int bytesRead;
                            long currentOffset = start;
                            
                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                            {
                                await RandomAccess.WriteAsync(fileHandle, new ReadOnlyMemory<byte>(buffer, 0, bytesRead), currentOffset);
                                currentOffset += bytesRead;
                                
                                long currentTotal = System.Threading.Interlocked.Add(ref totalRead, bytesRead);
                                
                                // Throttle UI updates to roughly every 1MB or at the very end
                                if (currentTotal % (1024 * 1024) < bytesRead || currentTotal == totalBytes)
                                {
                                    double percentage = (double)currentTotal / totalBytes * 100;
                                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                                        DownloadProgressBar.Value = percentage;
                                        TxtProgress.Text = $"多线程拉满狂飙中 {percentage:F1}%";
                                    });
                                }
                            }
                        }));
                    }
                    
                    await Task.WhenAll(tasks);
                }
            }
            else
            {
                // Fallback to single thread
                using var contentStream = await checkResponse.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 131072, true);

                var buffer = new byte[131072];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (canReportProgress)
                    {
                        double percentage = (double)totalRead / totalBytes * 100;
                        DownloadProgressBar.Value = percentage;
                        TxtProgress.Text = $"正在下载 {percentage:F1}%";
                    }
                }
            }

            _canClose = true;
            Process.Start(new ProcessStartInfo
            {
                FileName = tempPath,
                Arguments = "/update",
                UseShellExecute = true
            });

            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"下载更新失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _isDownloading = false;
            BtnUpdate.IsEnabled = true;
            BtnUpdate.Content = "重试下载并更新";
            ProgressGrid.Visibility = Visibility.Collapsed;
        }
    }
}
