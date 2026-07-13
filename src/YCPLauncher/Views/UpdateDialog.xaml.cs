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
        RenderMarkdown(TxtReleaseNotes, _updateInfo.ReleaseNotes ?? "");
    }

    private static void RenderMarkdown(System.Windows.Controls.TextBlock textBlock, string md)
    {
        textBlock.Inlines.Clear();
        if (string.IsNullOrEmpty(md)) return;

        var lines = md.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        bool firstLine = true;

        foreach (var line in lines)
        {
            if (!firstLine)
            {
                textBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
            }
            firstLine = false;

            string currentLine = line.TrimEnd();
            
            if (currentLine.StartsWith("### "))
            {
                textBlock.Inlines.Add(new System.Windows.Documents.Run(currentLine.Substring(4)) { FontSize = 16, FontWeight = System.Windows.FontWeights.Bold, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 250, 250)) });
                continue;
            }
            if (currentLine.StartsWith("## "))
            {
                textBlock.Inlines.Add(new System.Windows.Documents.Run(currentLine.Substring(3)) { FontSize = 18, FontWeight = System.Windows.FontWeights.Bold, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)) });
                textBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                continue;
            }
            if (currentLine.StartsWith("# "))
            {
                textBlock.Inlines.Add(new System.Windows.Documents.Run(currentLine.Substring(2)) { FontSize = 20, FontWeight = System.Windows.FontWeights.Bold, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)) });
                textBlock.Inlines.Add(new System.Windows.Documents.LineBreak());
                continue;
            }

            if (currentLine.StartsWith("- "))
            {
                textBlock.Inlines.Add(new System.Windows.Documents.Run(" •  ") { FontWeight = System.Windows.FontWeights.Bold, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 85, 0)) });
                currentLine = currentLine.Substring(2);
            }

            // Simple bold parsing: **text**
            var parts = System.Text.RegularExpressions.Regex.Split(currentLine, @"(\*\*.*?\*\*)");
            foreach (var part in parts)
            {
                if (part.StartsWith("**") && part.EndsWith("**") && part.Length > 4)
                {
                    textBlock.Inlines.Add(new System.Windows.Documents.Run(part.Substring(2, part.Length - 4)) { FontWeight = System.Windows.FontWeights.Bold });
                }
                else if (!string.IsNullOrEmpty(part))
                {
                    // Simple inline code: `text`
                    var codeParts = System.Text.RegularExpressions.Regex.Split(part, @"(`.*?`)");
                    foreach (var cPart in codeParts)
                    {
                        if (cPart.StartsWith("`") && cPart.EndsWith("`") && cPart.Length > 2)
                        {
                            textBlock.Inlines.Add(new System.Windows.Documents.Run(cPart.Substring(1, cPart.Length - 2))
                            {
                                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 180, 100))
                            });
                        }
                        else if (!string.IsNullOrEmpty(cPart))
                        {
                            textBlock.Inlines.Add(new System.Windows.Documents.Run(cPart));
                        }
                    }
                }
            }
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_canClose)
        {
            // Mandatory update, cannot close
            e.Cancel = true;
        }
    }

    private void BtnLater_Click(object sender, RoutedEventArgs e)
    {
        if (_isDownloading) return;
        _canClose = true;
        Close();
    }

    private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (_isDownloading) return;
        
        _isDownloading = true;
        BtnUpdate.IsEnabled = false;
        BtnLater.IsEnabled = false;
        BtnUpdate.Content = "正在安全下载...";
        ProgressGrid.Visibility = Visibility.Visible;
        string partialPath = Path.Combine(
            Path.GetTempPath(),
            $"YachiyoCup_Installer_v{_updateInfo.LatestVersion}.exe.download");

        try
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"YachiyoCup_Installer_v{_updateInfo.LatestVersion}.exe");
            if (!Uri.TryCreate(_updateInfo.DownloadUrl, UriKind.Absolute, out var downloadUri) ||
                !string.Equals(downloadUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(downloadUri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("更新下载地址不是受信任的 GitHub HTTPS 地址。");
            }

            if (File.Exists(partialPath)) File.Delete(partialPath);

            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("YCPLauncher/" + App.CurrentVersion);
            using var response = await client.GetAsync(downloadUri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? _updateInfo.FileSize;
            long totalRead = 0;
            await using (var contentStream = await response.Content.ReadAsStreamAsync())
            await using (var fileStream = new FileStream(
                partialPath, FileMode.Create, FileAccess.Write, FileShare.None, 131072, true))
            {
                var buffer = new byte[131072];
                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalRead += bytesRead;

                    if (totalBytes > 0)
                    {
                        double percentage = (double)totalRead / totalBytes * 100;
                        DownloadProgressBar.Value = Math.Min(percentage, 100);
                        TxtProgress.Text = $"正在安全下载 {percentage:F1}%";
                    }
                }
                await fileStream.FlushAsync();
            }

            if (_updateInfo.FileSize > 0 && totalRead != _updateInfo.FileSize)
                throw new InvalidDataException($"更新文件大小不匹配，预期 {_updateInfo.FileSize} 字节，实际 {totalRead} 字节。");

            await using (var verifyStream = File.OpenRead(partialPath))
            {
                if (verifyStream.ReadByte() != 'M' || verifyStream.ReadByte() != 'Z')
                    throw new InvalidDataException("下载内容不是有效的 Windows 安装程序。");
            }

            File.Move(partialPath, tempPath, true);
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
            try
            {
                if (File.Exists(partialPath)) File.Delete(partialPath);
            }
            catch { }

            System.Windows.MessageBox.Show($"下载更新失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _isDownloading = false;
            BtnUpdate.IsEnabled = true;
            BtnLater.IsEnabled = true;
            BtnUpdate.Content = "重试下载并更新";
            ProgressGrid.Visibility = Visibility.Collapsed;
        }
    }
}
