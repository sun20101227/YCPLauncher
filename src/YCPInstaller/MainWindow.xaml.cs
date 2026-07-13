using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace YCPInstaller
{
    public partial class MainWindow : Window
    {
        private string _installDir;

        public MainWindow()
        {
            InitializeComponent();
            _installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "YCPLauncher");
            TxtInstallPath.Text = _installDir;
            UpdateInstallButtonState();
        }

        private void UpdateInstallButtonState()
        {
            if (File.Exists(Path.Combine(_installDir, "YCPLauncher.exe")))
            {
                BtnInstall.Content = "覆盖安装";
            }
            else
            {
                BtnInstall.Content = "立即安装";
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "请选择安装目录";
                dialog.SelectedPath = _installDir;
                dialog.ShowNewFolderButton = true;
                
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _installDir = Path.Combine(dialog.SelectedPath, "YCPLauncher");
                    if (!dialog.SelectedPath.EndsWith("YCPLauncher", StringComparison.OrdinalIgnoreCase))
                    {
                        // Ensure it creates a subfolder so it doesn't pollute the root
                    }
                    else
                    {
                        _installDir = dialog.SelectedPath;
                    }
                    TxtInstallPath.Text = _installDir;
                    UpdateInstallButtonState();
                }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            BtnInstall.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                try
                {
                    // Kill process if running
                    var procs = Process.GetProcessesByName("YCPLauncher");
                    foreach (var p in procs)
                    {
                        try { p.Kill(); p.WaitForExit(); } catch { }
                    }

                    // Ensure dir
                    if (!Directory.Exists(_installDir))
                    {
                        Directory.CreateDirectory(_installDir);
                    }

                    // Extract embedded zip
                    var assembly = Assembly.GetExecutingAssembly();
                    var resourceName = "YCPInstaller.payload.zip";
                    using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null) throw new Exception("无法找到内嵌的安装包资源。");
                        
                        string tempZip = Path.Combine(
                            Path.GetTempPath(),
                            $"YCPLauncher_payload_{Guid.NewGuid():N}.zip");
                        try
                        {
                            using (FileStream fileStream = new FileStream(tempZip, FileMode.CreateNew))
                            {
                                stream.CopyTo(fileStream);
                            }

                            ZipFile.ExtractToDirectory(tempZip, _installDir, true);
                        }
                        finally
                        {
                            if (File.Exists(tempZip))
                                File.Delete(tempZip);
                        }
                    }

                    string launcherExe = Path.Combine(_installDir, "YCPLauncher.exe");

                    try
                    {
                        // Create Desktop Shortcut
                        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                        string shortcutPath = Path.Combine(desktop, "YCP电竞启动器.lnk");
                        CreateShortcutPs(launcherExe, shortcutPath);

                        // Create Start Menu Shortcut
                        string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\YCP电竞启动器");
                        Directory.CreateDirectory(startMenu);
                        CreateShortcutPs(launcherExe, Path.Combine(startMenu, "YCP电竞启动器.lnk"));
                        
                        string uninstallerExe = Path.Combine(_installDir, "YCPUninstaller.exe");
                        if (File.Exists(uninstallerExe))
                        {
                            CreateShortcutPs(uninstallerExe, Path.Combine(startMenu, "卸载 YCP电竞启动器.lnk"));
                        }

                        // Write Uninstall Registry Keys
                        using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\YCPLauncher"))
                        {
                            key.SetValue("DisplayName", "YCP 电竞启动器", Microsoft.Win32.RegistryValueKind.String);
                            string displayVersion =
                                Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.1.7";
                            key.SetValue("DisplayVersion", displayVersion, Microsoft.Win32.RegistryValueKind.String);
                            key.SetValue("Publisher", "YachiyoCup", Microsoft.Win32.RegistryValueKind.String);
                            key.SetValue("UninstallString", uninstallerExe, Microsoft.Win32.RegistryValueKind.String);
                            key.SetValue("DisplayIcon", launcherExe, Microsoft.Win32.RegistryValueKind.String);
                            key.SetValue("InstallLocation", _installDir, Microsoft.Win32.RegistryValueKind.String);
                        }
                    }
                    catch
                    {
                        // Ignore shortcut or registry errors silently to not block installation completion
                    }

                    Dispatcher.Invoke(() =>
                    {
                        ProgressPanel.Visibility = Visibility.Collapsed;
                        DonePanel.Visibility = Visibility.Visible;
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show($"安装失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        System.Windows.Application.Current.Shutdown();
                    });
                }
            });
        }

        private void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            string targetPath = Path.Combine(_installDir, "YCPLauncher.exe");
            if (File.Exists(targetPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = targetPath,
                    WorkingDirectory = _installDir,
                    UseShellExecute = true
                });
            }
            System.Windows.Application.Current.Shutdown();
        }

        private void CreateShortcutPs(string targetPath, string shortcutPath)
        {
            static string Escape(string value) => value.Replace("'", "''");
            string workingDirectory = Path.GetDirectoryName(targetPath) ?? _installDir;
            string script =
                $"$s=(New-Object -COM WScript.Shell).CreateShortcut('{Escape(shortcutPath)}');" +
                $"$s.TargetPath='{Escape(targetPath)}';" +
                $"$s.WorkingDirectory='{Escape(workingDirectory)}';$s.Save()";
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"{script}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            })?.WaitForExit();
        }
    }
}
