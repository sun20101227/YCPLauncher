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
        TxtReleaseNotes.Text = _updateInfo.ReleaseNotes;
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
        BtnUpdate.Content = "正在下载...";
        ProgressGrid.Visibility = Visibility.Visible;

        try
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"YachiyoCup_Installer_v{_updateInfo.LatestVersion}.exe");
            
            // Use GitHub mirror proxy for faster download in China
            string downloadUrl = _updateInfo.DownloadUrl;
            if (downloadUrl != null && downloadUrl.Contains("github.com"))
            {
                downloadUrl = "https://mirror.ghproxy.com/" + downloadUrl;
            }

            using var client = new HttpClient();
            using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 131072, true);

            var buffer = new byte[131072]; // 128 KB buffer for faster write
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
            
            // Release the file lock before executing
            fileStream.Close();

            // Execute the installer
            _canClose = true;
            Process.Start(new ProcessStartInfo
            {
                FileName = tempPath,
                Arguments = "/update",
                UseShellExecute = true
            });

            // Kill launcher
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
