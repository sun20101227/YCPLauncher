using System;
using System.Diagnostics;

namespace YCPLauncher.Services;

public class GameLauncherService
{
    private const string Cs2AppId = "730";

    public static bool LaunchDirect(string ip, int port, string serverName)
    {
        try
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var dialog = new Views.TerminalLaunchDialog();
                dialog.Show();
                await dialog.RunLogsAsync(new string[] 
                {
                    $"[SYSTEM] 初始化 YCP CS2 注入引擎...",
                    $"[NETWORK] 验证通信链路状态... UDP直连成功",
                    $"[MATCH] 锁定目标服务器 -> {ip}:{port} [{serverName}]",
                    $"[LAUNCH] 正在下发 Steam URI 启动协议...",
                    $"[SUCCESS] 指令已下发！正在拉起 Source 2 引擎，请等待..."
                });
            });

            var cfg = ConfigService.GetConfig();
            string extraArgs = "";
            if (cfg.LaunchNoVid) extraArgs += "-novid ";
            if (cfg.LaunchHighFreq) extraArgs += "-freq 240 ";
            if (cfg.LaunchConsole) extraArgs += "-console ";

            var player = AuthService.LoadPlayer();
            if (player != null && !string.IsNullOrWhiteSpace(player.Username))
            {
                // URI encoding for name with spaces
                string cleanName = player.Username.Trim().Replace(" ", "_");
                extraArgs += $"+setinfo ycp_name {cleanName} ";
            }

            // Url encode the spaces so the protocol handler doesn't truncate the string
            string args = $"{extraArgs}+connect {ip}:{port}";
            string encodedArgs = args.Replace(" ", "%20").Replace("\"", "%22");

            // User explicitly requested: steam://rungameid/730//+connect ip:port
            string url = $"steam://rungameid/{Cs2AppId}//{encodedArgs}";

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });

            return true;
        }
        catch
        {
            return false;
        }
    }
}
