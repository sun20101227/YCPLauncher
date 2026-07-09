using System.Configuration;
using System.Data;
using System.Windows;

namespace YCPInstaller;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        bool silentMode = false;
        foreach (var arg in e.Args)
        {
            if (arg.ToLower() == "/update" || arg.ToLower() == "/silent")
            {
                silentMode = true;
                break;
            }
        }

        if (silentMode)
        {
            // Execute silent installation
            PerformSilentInstall();
        }
        else
        {
            // Check .NET 8 Desktop Runtime
            if (!IsDotNet8Installed())
            {
                var result = System.Windows.MessageBox.Show(
                    "YCP 电竞启动器 需要 .NET 8 Desktop Runtime 才能运行。\n\n" +
                    "点击「是」前往微软官网下载（约 55MB），\n" +
                    "下载安装完成后请重新运行本安装程序。",
                    "缺少运行环境",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe",
                        UseShellExecute = true
                    });
                }
                Shutdown();
                return;
            }

            InstallerSplashWindow splash = new InstallerSplashWindow();
            splash.Show();
        }

    }

    private static bool IsDotNet8Installed()
    {
        try
        {
            // Check registry for .NET 8 Desktop Runtime
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App");
            if (key != null)
            {
                foreach (var valueName in key.GetValueNames())
                {
                    if (valueName.StartsWith("8."))
                        return true;
                }
            }
        }
        catch { }

        // Fallback: try running dotnet
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("dotnet", "--list-runtimes")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var p = System.Diagnostics.Process.Start(psi);
            string output = p?.StandardOutput.ReadToEnd() ?? "";
            p?.WaitForExit();
            return output.Contains("Microsoft.WindowsDesktop.App 8.");
        }
        catch { }

        return false;
    }

    private void PerformSilentInstall()
    {
        // A minimal logic of what MainWindow does during install
        string installDir = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86), "YCPLauncher");
        try
        {
            if (!System.IO.Directory.Exists(installDir))
                System.IO.Directory.CreateDirectory(installDir);

            using (System.IO.Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("YCPInstaller.payload.zip"))
            {
                if (stream == null) throw new System.Exception("找不到资源“payload.zip”。");
                using (System.IO.Compression.ZipArchive archive = new System.IO.Compression.ZipArchive(stream))
                {
                foreach (System.IO.Compression.ZipArchiveEntry entry in archive.Entries)
                {
                    string fullPath = System.IO.Path.Combine(installDir, entry.FullName);
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        System.IO.Directory.CreateDirectory(fullPath);
                    }
                    else
                    {
                        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));
                        using (var es = entry.Open())
                        using (var fs = System.IO.File.Create(fullPath))
                        {
                            es.CopyTo(fs);
                        }
                    }
                }
            }
            }

            // Registry & Shortcuts 
            string exePath = System.IO.Path.Combine(installDir, "YCPLauncher.exe");
            
            // We just launch the updated app and exit
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = installDir,
                UseShellExecute = true
            });
            
            this.Shutdown();
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show("后台更新失败: " + ex.Message, "更新错误", MessageBoxButton.OK, MessageBoxImage.Error);
            this.Shutdown();
        }
    }
}

