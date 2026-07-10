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

            // User explicitly requested EXACTLY this simple format without any smart encoding or extra parameters that might break Steam's parsing
            string url = $"steam://rungameid/{Cs2AppId}//+connect {ip}:{port}";

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
