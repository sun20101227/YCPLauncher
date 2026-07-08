using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace YCPUninstaller
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var result = MessageBox.Show(
                "确定要卸载 YCP电竞启动器 吗？这将会删除相关的安装文件。",
                "卸载确认",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                // 1. Kill the process if running
                var procs = Process.GetProcessesByName("YCPLauncher");
                foreach (var p in procs)
                {
                    try { p.Kill(); p.WaitForExit(2000); } catch { }
                }

                // 2. Delete desktop shortcut
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = Path.Combine(desktopPath, "YCP电竞启动器.lnk");
                if (File.Exists(shortcutPath))
                {
                    File.Delete(shortcutPath);
                }

                // 2.1 Delete start menu shortcuts
                string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\YCP电竞启动器");
                if (Directory.Exists(startMenu))
                {
                    Directory.Delete(startMenu, true);
                }

                // 2.2 Delete Registry Key
                try
                {
                    Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\YCPLauncher", false);
                }
                catch { }

                // 3. Prepare self-deletion via cmd
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                string exePath = Application.ExecutablePath;
                string cmd = $"/c ping localhost -n 3 > nul & rd /s /q \"{currentDir}\"";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = cmd,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi);

                MessageBox.Show("YCP电竞启动器 已成功卸载！", "卸载完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"卸载过程中出现错误：\n{ex.Message}", "卸载错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}