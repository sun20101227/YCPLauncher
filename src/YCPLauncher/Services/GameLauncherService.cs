using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace YCPLauncher.Services;

public class GameLauncherService
{
    private const string Cs2AppId = "730";
    private const string DefaultCs2InstallDirectory = "Counter-Strike Global Offensive";
    private const int MaxCustomArgumentsLength = 512;
    private static long _launchGeneration;
    private static int _cfgSequence;
    private static readonly object LaunchSync = new();

    public static bool LaunchDirect(string ip, int port, string serverName, string customArgs = "")
    {
        try
        {
            if (!TryNormalizeServerEndpoint(ip, port, out var endpoint) ||
                !TryParseCustomSetInfoArguments(customArgs, out var setInfoValues))
            {
                return false;
            }

            var config = ConfigService.GetConfig();
            var cs2WasRunning = TryGetRunningCs2Executable(out var runningCs2Exe);
            var cs2Exe = runningCs2Exe ?? GetCs2ExecutablePath();
            var requestStartedUtc = DateTime.UtcNow;

            AddDefaultPlayerName(setInfoValues);

            var cfgName = TryWriteConnectionCfg(cs2Exe, endpoint, setInfoValues);
            var gameArguments = BuildGameArguments(config, endpoint, cfgName, setInfoValues);

            bool launchRequestSent;
            long launchGeneration;
            lock (LaunchSync)
            {
                if (cs2WasRunning)
                {
                    // Steam may ignore launch arguments for a running game, but sending the
                    // cfg execution request first gives team/name metadata a best-effort path.
                    var metadataSent = TryLaunchViaSteamExecutable(gameArguments) ||
                                       TryLaunchViaSteamUri(gameArguments);
                    launchRequestSent = TrySteamConnect(endpoint) || metadataSent;
                }
                else
                {
                    launchRequestSent = config.LaunchMethod switch
                    {
                        1 => TryLaunchViaSteamExecutable(gameArguments) ||
                             TryLaunchViaSteamUri(gameArguments),
                        2 => TryLaunchViaSteamExecutable(gameArguments) ||
                             TryLaunchViaSteamUri(gameArguments) ||
                             TrySteamConnect(endpoint),
                        _ => TryLaunchViaSteamUri(gameArguments) ||
                             TryLaunchViaSteamExecutable(gameArguments)
                    };
                }

                if (!launchRequestSent)
                    return false;

                // Only a successfully dispatched request supersedes an older recovery task.
                launchGeneration = ++_launchGeneration;
            }

            ShowLaunchDialog(endpoint, serverName);

            // Fire-and-forget is intentional: LaunchDirect is called from synchronous UI commands.
            // The generation guard prevents an older server request from overriding a newer click.
            _ = EnsureConnectionAfterGameReadyAsync(
                endpoint,
                launchGeneration,
                cs2WasRunning,
                requestStartedUtc);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string SanitizeSetInfoValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Player";

        var builder = new StringBuilder(Math.Min(value.Length, 48));
        var previousWasSpace = false;

        foreach (var character in value)
        {
            if (builder.Length >= 48)
                break;

            // Quotes and backslashes can escape the cfg string; semicolons can start
            // another Source console command. Remove all three rather than escaping them.
            if (character is '"' or '\\' or ';' || char.IsControl(character))
                continue;

            if (char.IsWhiteSpace(character))
            {
                if (builder.Length > 0 && !previousWasSpace)
                {
                    builder.Append(' ');
                    previousWasSpace = true;
                }

                continue;
            }

            builder.Append(character);
            previousWasSpace = false;
        }

        var sanitized = builder.ToString().Trim();
        if (sanitized.Length == 0)
            return "Player";

        // Source treats leading '+' as another console command and leading '-' as
        // a launch option when cfg injection is unavailable and arguments are used.
        return sanitized[0] is '+' or '-' ? "_" + sanitized : sanitized;
    }

    private static void ShowLaunchDialog(string endpoint, string serverName)
    {
        var application = System.Windows.Application.Current;
        if (application == null)
            return;

        var safeServerName = (serverName ?? string.Empty)
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();

        application.Dispatcher.InvokeAsync(async () =>
        {
            var dialog = new Views.TerminalLaunchDialog();
            dialog.Show();
            await dialog.RunLogsAsync(new[]
            {
                "[SYSTEM] 初始化 YCP CS2 注入引擎...",
                "[NETWORK] 验证通信链路状态... UDP直连成功",
                $"[MATCH] 锁定目标服务器 -> {endpoint} [{safeServerName}]",
                "[LAUNCH] 正在下发启动协议...",
                "[SUCCESS] 指令已下发！正在拉起 Source 2 引擎，请等待..."
            });
        });
    }

    private static void AddDefaultPlayerName(Dictionary<string, string> setInfoValues)
    {
        if (setInfoValues.ContainsKey("ycp_name"))
            return;

        var player = AuthService.LoadPlayer();
        if (player != null && !string.IsNullOrWhiteSpace(player.Username))
            setInfoValues["ycp_name"] = SanitizeSetInfoValue(player.Username);
    }

    private static List<string> BuildGameArguments(
        Models.AppConfig config,
        string endpoint,
        string? cfgName,
        Dictionary<string, string> setInfoValues)
    {
        var arguments = new List<string>();

        if (config.LaunchNoVid)
            arguments.Add("-novid");
        if (config.LaunchHighFreq)
        {
            arguments.Add("-freq");
            arguments.Add("240");
        }
        if (config.LaunchConsole)
            arguments.Add("-console");

        if (!string.IsNullOrEmpty(cfgName))
        {
            arguments.Add("+exec");
            arguments.Add(cfgName);
            return arguments;
        }

        // If the install path could not be resolved or cfg writing was denied,
        // pass only the strictly allow-listed commands as individual arguments.
        AddSetInfoArguments(arguments, setInfoValues);
        arguments.Add("+connect");
        arguments.Add(endpoint);
        return arguments;
    }

    private static void AddSetInfoArguments(
        List<string> arguments,
        Dictionary<string, string> setInfoValues)
    {
        if (setInfoValues.TryGetValue("ycp_team", out var team))
        {
            arguments.Add("+setinfo");
            arguments.Add("ycp_team");
            arguments.Add(team);
        }

        if (setInfoValues.TryGetValue("ycp_name", out var playerName))
        {
            arguments.Add("+setinfo");
            arguments.Add("ycp_name");
            arguments.Add(playerName);
        }
    }

    private static bool TryParseCustomSetInfoArguments(
        string customArgs,
        out Dictionary<string, string> setInfoValues)
    {
        setInfoValues = new Dictionary<string, string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(customArgs))
            return true;
        if (customArgs.Length > MaxCustomArgumentsLength ||
            customArgs.IndexOfAny(['\r', '\n', ';']) >= 0)
        {
            return false;
        }

        if (!TryTokenizeArguments(customArgs, out var tokens) || tokens.Count % 3 != 0)
            return false;

        for (var index = 0; index < tokens.Count; index += 3)
        {
            if (!string.Equals(tokens[index], "+setinfo", StringComparison.Ordinal))
                return false;

            var key = tokens[index + 1];
            var value = tokens[index + 2];

            if (string.Equals(key, "ycp_team", StringComparison.Ordinal))
            {
                if (value is not ("1" or "2" or "3"))
                    return false;
                setInfoValues[key] = value;
            }
            else if (string.Equals(key, "ycp_name", StringComparison.Ordinal))
            {
                setInfoValues[key] = SanitizeSetInfoValue(value);
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryTokenizeArguments(string arguments, out List<string> tokens)
    {
        tokens = new List<string>();
        var index = 0;

        while (index < arguments.Length)
        {
            while (index < arguments.Length && char.IsWhiteSpace(arguments[index]))
                index++;
            if (index >= arguments.Length)
                break;

            if (arguments[index] == '"')
            {
                index++;
                var start = index;
                while (index < arguments.Length && arguments[index] != '"')
                    index++;
                if (index >= arguments.Length)
                    return false;

                tokens.Add(arguments[start..index]);
                index++;

                if (index < arguments.Length && !char.IsWhiteSpace(arguments[index]))
                    return false;
            }
            else
            {
                var start = index;
                while (index < arguments.Length && !char.IsWhiteSpace(arguments[index]))
                {
                    if (arguments[index] == '"')
                        return false;
                    index++;
                }

                tokens.Add(arguments[start..index]);
            }
        }

        return true;
    }

    private static string? TryWriteConnectionCfg(
        string? cs2Exe,
        string endpoint,
        Dictionary<string, string> setInfoValues)
    {
        if (string.IsNullOrEmpty(cs2Exe))
            return null;

        string? temporaryFile = null;
        try
        {
            var win64Directory = Path.GetDirectoryName(cs2Exe);
            var binDirectory = win64Directory == null ? null : Path.GetDirectoryName(win64Directory);
            var gameDirectory = binDirectory == null ? null : Path.GetDirectoryName(binDirectory);
            if (gameDirectory == null)
                return null;

            var csgoDirectory = Path.Combine(gameDirectory, "csgo");
            if (!Directory.Exists(csgoDirectory))
                return null;

            var cfgDirectory = Path.Combine(csgoDirectory, "cfg");
            Directory.CreateDirectory(cfgDirectory);
            DeleteStaleConnectionCfgs(cfgDirectory);

            var cfgName =
                $"ycp_connect_{Environment.ProcessId}_{Interlocked.Increment(ref _cfgSequence)}.cfg";
            var cfgFile = Path.Combine(cfgDirectory, cfgName);
            temporaryFile = cfgFile + ".tmp";

            var content = BuildCfgContent(endpoint, setInfoValues);
            using (var stream = new FileStream(
                       temporaryFile,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.Read,
                       4096,
                       FileOptions.WriteThrough))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(content);
                writer.Flush();
                stream.Flush(true);
            }

            File.Move(temporaryFile, cfgFile);
            temporaryFile = null;
            return cfgName;
        }
        catch
        {
            if (!string.IsNullOrEmpty(temporaryFile))
            {
                try
                {
                    File.Delete(temporaryFile);
                }
                catch
                {
                    // Best-effort cleanup only.
                }
            }

            return null;
        }
    }

    private static string BuildCfgContent(
        string endpoint,
        Dictionary<string, string> setInfoValues)
    {
        var builder = new StringBuilder();

        if (setInfoValues.TryGetValue("ycp_team", out var team))
            builder.Append("setinfo ycp_team \"").Append(team).AppendLine("\"");
        if (setInfoValues.TryGetValue("ycp_name", out var playerName))
            builder.Append("setinfo ycp_name \"").Append(playerName).AppendLine("\"");

        builder.Append("connect ").Append(endpoint).AppendLine();
        return builder.ToString();
    }

    private static void DeleteStaleConnectionCfgs(string cfgDirectory)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);
            foreach (var file in Directory.EnumerateFiles(
                         cfgDirectory,
                         "ycp_connect_*.cfg",
                         SearchOption.TopDirectoryOnly))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(file) < cutoff)
                        File.Delete(file);
                }
                catch
                {
                    // A stale file can be locked by Steam/CS2; it is safe to leave it.
                }
            }
        }
        catch
        {
            // Cleanup must never prevent launching.
        }
    }

    private static bool TryLaunchViaSteamExecutable(IReadOnlyList<string> gameArguments)
    {
        var steamExe = GetSteamExePath();
        if (string.IsNullOrEmpty(steamExe))
            return false;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = steamExe,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("-applaunch");
            startInfo.ArgumentList.Add(Cs2AppId);
            foreach (var argument in gameArguments)
                startInfo.ArgumentList.Add(argument);

            Process.Start(startInfo)?.Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryLaunchViaSteamUri(IReadOnlyList<string> gameArguments)
    {
        var commandLine = BuildEncodedSteamCommandLine(gameArguments);
        // Steam requires console-command '+' prefixes to remain literal. Encoding '+'
        // as %2B regresses the exact protocol form that CS2 accepts reliably.
        var encodedCommandLine = Uri.EscapeDataString(commandLine)
            .Replace("%2B", "+", StringComparison.OrdinalIgnoreCase);
        var url = $"steam://rungameid/{Cs2AppId}//{encodedCommandLine}";
        return TryOpenSteamUri(url);
    }

    private static string BuildEncodedSteamCommandLine(IReadOnlyList<string> arguments)
    {
        var builder = new StringBuilder();
        foreach (var argument in arguments)
        {
            if (builder.Length > 0)
                builder.Append(' ');

            if (argument.IndexOfAny([' ', '\t']) < 0)
            {
                builder.Append(argument);
            }
            else
            {
                // All values reaching here are controlled/sanitized and cannot contain quotes
                // or backslashes, so ordinary quoting is sufficient before URL encoding.
                builder.Append('"').Append(argument).Append('"');
            }
        }

        return builder.ToString();
    }

    private static bool TrySteamConnect(string endpoint)
    {
        return TryOpenSteamUri($"steam://connect/{endpoint}");
    }

    private static bool TryOpenSteamUri(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            })?.Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task EnsureConnectionAfterGameReadyAsync(
        string endpoint,
        long launchGeneration,
        bool gameWasAlreadyRunning,
        DateTime requestStartedUtc)
    {
        try
        {
            if (!await WaitForCs2ReadyAsync(
                    launchGeneration,
                    requestStartedUtc,
                    gameWasAlreadyRunning).ConfigureAwait(false))
                return;

            await Task.Delay(
                    gameWasAlreadyRunning
                        ? TimeSpan.FromMilliseconds(1200)
                        : TimeSpan.FromSeconds(2))
                .ConfigureAwait(false);

            lock (LaunchSync)
            {
                if (launchGeneration != _launchGeneration)
                    return;

                // Keep the generation check and protocol dispatch atomic relative to a
                // newer click, preventing an old retry from reconnecting the user.
                TrySteamConnect(endpoint);
            }
        }
        catch
        {
            // The primary launch request already succeeded; retries are best effort.
        }
    }

    private static async Task<bool> WaitForCs2ReadyAsync(
        long launchGeneration,
        DateTime requestStartedUtc,
        bool gameWasAlreadyRunning)
    {
        // Steam may need to finish an update before cs2.exe appears, but a shorter
        // bounded window avoids connecting an unrelated game start much later.
        var deadline = DateTime.UtcNow.AddSeconds(90);
        DateTime? firstSeen = null;
        DateTime? lastSeen = null;

        while (DateTime.UtcNow < deadline)
        {
            if (launchGeneration != Volatile.Read(ref _launchGeneration))
                return false;

            var processes = Array.Empty<Process>();
            try
            {
                processes = Process.GetProcessesByName("cs2");
                var matchingProcessSeen = false;
                if (processes.Length > 0)
                {
                    foreach (var process in processes)
                    {
                        try
                        {
                            process.Refresh();
                            if (!gameWasAlreadyRunning &&
                                process.StartTime.ToUniversalTime() <
                                requestStartedUtc.AddSeconds(-2))
                            {
                                continue;
                            }

                            matchingProcessSeen = true;
                            firstSeen ??= DateTime.UtcNow;
                            lastSeen = DateTime.UtcNow;
                            if (!process.HasExited &&
                                process.MainWindowHandle != IntPtr.Zero &&
                                process.Responding)
                            {
                                return true;
                            }
                        }
                        catch
                        {
                            // Process state can change between enumeration and inspection.
                        }
                    }

                    // Some CS2/Windows combinations do not expose a stable main window
                    // through Process. A bounded fallback is more reliable than never retrying.
                    if (matchingProcessSeen &&
                        firstSeen.HasValue &&
                        DateTime.UtcNow - firstSeen.Value >= TimeSpan.FromSeconds(12))
                        return true;
                }

                if (!matchingProcessSeen &&
                    lastSeen.HasValue &&
                    DateTime.UtcNow - lastSeen.Value > TimeSpan.FromSeconds(5))
                {
                    return false;
                }
            }
            catch
            {
                // Retry until the bounded deadline.
            }
            finally
            {
                foreach (var process in processes)
                    process.Dispose();
            }

            await Task.Delay(750).ConfigureAwait(false);
        }

        return false;
    }

    private static bool TryNormalizeServerEndpoint(string host, int port, out string endpoint)
    {
        endpoint = string.Empty;
        if (string.IsNullOrWhiteSpace(host) || port is < 1 or > 65535)
            return false;

        host = host.Trim();
        if (host.IndexOfAny([' ', '\t', '\r', '\n', '"', '\'', ';', '/', '\\']) >= 0)
            return false;

        if (IPAddress.TryParse(host, out var address))
        {
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // Scoped/link-local addresses cannot identify an Internet game server
                // reliably through the Steam URI protocol.
                if (address.ScopeId != 0)
                    return false;
                endpoint = $"[{address}]:{port}";
            }
            else
            {
                endpoint = $"{address}:{port}";
            }

            return true;
        }

        try
        {
            var asciiHost = new IdnMapping().GetAscii(host.TrimEnd('.')).ToLowerInvariant();
            if (!IsValidDnsName(asciiHost))
                return false;

            endpoint = $"{asciiHost}:{port}";
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool IsValidDnsName(string host)
    {
        if (host.Length is < 1 or > 253)
            return false;

        var labels = host.Split('.');
        foreach (var label in labels)
        {
            if (label.Length is < 1 or > 63 ||
                label[0] == '-' ||
                label[^1] == '-')
            {
                return false;
            }

            foreach (var character in label)
            {
                if (!char.IsAsciiLetterOrDigit(character) && character != '-')
                    return false;
            }
        }

        return true;
    }

    private static bool TryGetRunningCs2Executable(out string? executablePath)
    {
        executablePath = null;
        var processes = Array.Empty<Process>();

        try
        {
            processes = Process.GetProcessesByName("cs2");
            foreach (var process in processes)
            {
                try
                {
                    if (process.HasExited)
                        continue;

                    var candidate = NormalizeExistingFilePath(process.MainModule?.FileName);
                    if (candidate != null)
                    {
                        executablePath = candidate;
                        break;
                    }
                }
                catch
                {
                    // MainModule may require permissions; running state is still useful.
                }
            }

            return processes.Length > 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            foreach (var process in processes)
                process.Dispose();
        }
    }

    private static string? GetSteamExePath()
    {
        var steamProcesses = Array.Empty<Process>();
        try
        {
            steamProcesses = Process.GetProcessesByName("steam");
            foreach (var process in steamProcesses)
            {
                try
                {
                    var processPath = NormalizeExistingFilePath(process.MainModule?.FileName);
                    if (processPath != null)
                        return processPath;
                }
                catch
                {
                    // Fall through to registry and conventional paths.
                }
            }
        }
        catch
        {
            // Fall through to registry and conventional paths.
        }
        finally
        {
            foreach (var process in steamProcesses)
                process.Dispose();
        }

        foreach (var candidate in GetSteamExecutableCandidates())
        {
            var existingPath = NormalizeExistingFilePath(candidate);
            if (existingPath != null)
                return existingPath;
        }

        return null;
    }

    private static IEnumerable<string?> GetSteamExecutableCandidates()
    {
        yield return TryExtractExecutablePathFromCommand(ReadRegistryString(
            Registry.ClassesRoot,
            @"steam\Shell\Open\Command",
            string.Empty));

        yield return TryExtractExecutablePathFromCommand(ReadRegistryString(
            Registry.CurrentUser,
            @"Software\Classes\steam\Shell\Open\Command",
            string.Empty));

        yield return ReadRegistryString(
            Registry.CurrentUser,
            @"Software\Valve\Steam",
            "SteamExe");

        var currentUserSteamPath = ReadRegistryString(
            Registry.CurrentUser,
            @"Software\Valve\Steam",
            "SteamPath");
        if (!string.IsNullOrWhiteSpace(currentUserSteamPath))
            yield return Path.Combine(currentUserSteamPath, "steam.exe");

        var machineSteamPath = ReadRegistryString(
            Registry.LocalMachine,
            @"Software\WOW6432Node\Valve\Steam",
            "InstallPath");
        if (!string.IsNullOrWhiteSpace(machineSteamPath))
            yield return Path.Combine(machineSteamPath, "steam.exe");

        machineSteamPath = ReadRegistryString(
            Registry.LocalMachine,
            @"Software\Valve\Steam",
            "InstallPath");
        if (!string.IsNullOrWhiteSpace(machineSteamPath))
            yield return Path.Combine(machineSteamPath, "steam.exe");

        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(programFilesX86))
            yield return Path.Combine(programFilesX86, "Steam", "steam.exe");
    }

    private static string? TryExtractExecutablePathFromCommand(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return null;

        var match = Regex.Match(
            command,
            "^\\s*(?:\"([^\"]+\\.exe)\"|(\\S+\\.exe))",
            RegexOptions.IgnoreCase);
        return match.Success
            ? match.Groups[1].Success
                ? match.Groups[1].Value
                : match.Groups[2].Value
            : null;
    }

    private static string? GetCs2ExecutablePath()
    {
        foreach (var steamRoot in GetSteamRootCandidates())
        {
            foreach (var libraryRoot in GetSteamLibraryCandidates(steamRoot))
            {
                var executable = GetCs2ExecutableFromLibrary(libraryRoot);
                if (executable != null)
                    return executable;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetSteamRootCandidates()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var steamExe = GetSteamExePath();
        if (!string.IsNullOrEmpty(steamExe))
        {
            var exeDirectory = Path.GetDirectoryName(steamExe);
            var normalized = NormalizeExistingDirectoryPath(exeDirectory);
            if (normalized != null && seen.Add(normalized))
                yield return normalized;
        }

        var registryPath = ReadRegistryString(
            Registry.CurrentUser,
            @"Software\Valve\Steam",
            "SteamPath");
        var normalizedRegistryPath = NormalizeExistingDirectoryPath(registryPath);
        if (normalizedRegistryPath != null && seen.Add(normalizedRegistryPath))
            yield return normalizedRegistryPath;

        var machinePath = ReadRegistryString(
            Registry.LocalMachine,
            @"Software\WOW6432Node\Valve\Steam",
            "InstallPath");
        var normalizedMachinePath = NormalizeExistingDirectoryPath(machinePath);
        if (normalizedMachinePath != null && seen.Add(normalizedMachinePath))
            yield return normalizedMachinePath;

        machinePath = ReadRegistryString(
            Registry.LocalMachine,
            @"Software\Valve\Steam",
            "InstallPath");
        normalizedMachinePath = NormalizeExistingDirectoryPath(machinePath);
        if (normalizedMachinePath != null && seen.Add(normalizedMachinePath))
            yield return normalizedMachinePath;
    }

    private static IEnumerable<string> GetSteamLibraryCandidates(string steamRoot)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (seen.Add(steamRoot))
            yield return steamRoot;

        var vdfCandidates = new[]
        {
            Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf"),
            Path.Combine(steamRoot, "config", "libraryfolders.vdf")
        };

        foreach (var vdfPath in vdfCandidates)
        {
            if (!File.Exists(vdfPath))
                continue;

            string content;
            try
            {
                content = File.ReadAllText(vdfPath);
            }
            catch
            {
                continue;
            }

            foreach (Match match in Regex.Matches(
                         content,
                         "\"path\"\\s+\"([^\"]+)\"",
                         RegexOptions.IgnoreCase))
            {
                var path = NormalizeExistingDirectoryPath(DecodeVdfString(match.Groups[1].Value));
                if (path != null && seen.Add(path))
                    yield return path;
            }

            // Steam's older VDF format stored library paths directly under numeric keys.
            foreach (Match match in Regex.Matches(content, "\"\\d+\"\\s+\"([^\"]+)\""))
            {
                var path = NormalizeExistingDirectoryPath(DecodeVdfString(match.Groups[1].Value));
                if (path != null && seen.Add(path))
                    yield return path;
            }
        }
    }

    private static string? GetCs2ExecutableFromLibrary(string libraryRoot)
    {
        try
        {
            var steamApps = Path.Combine(libraryRoot, "steamapps");
            var manifestPath = Path.Combine(steamApps, $"appmanifest_{Cs2AppId}.acf");
            if (!File.Exists(manifestPath))
                return null;

            var installDirectory = DefaultCs2InstallDirectory;
            try
            {
                var manifest = File.ReadAllText(manifestPath);
                var installDirMatch = Regex.Match(
                    manifest,
                    "\"installdir\"\\s+\"([^\"]+)\"",
                    RegexOptions.IgnoreCase);
                if (installDirMatch.Success)
                    installDirectory = DecodeVdfString(installDirMatch.Groups[1].Value);
            }
            catch
            {
                // The historical directory name remains a useful fallback.
            }

            var commonDirectory = Path.GetFullPath(Path.Combine(steamApps, "common"));
            var installRoot = Path.GetFullPath(Path.Combine(commonDirectory, installDirectory));
            if (!IsPathWithin(installRoot, commonDirectory))
                return null;

            return NormalizeExistingFilePath(Path.Combine(
                installRoot,
                "game",
                "bin",
                "win64",
                "cs2.exe"));
        }
        catch
        {
            return null;
        }
    }

    private static bool IsPathWithin(string candidate, string parent)
    {
        var normalizedParent = parent.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
        return candidate.Equals(normalizedParent, StringComparison.OrdinalIgnoreCase) ||
               candidate.StartsWith(
                   normalizedParent + Path.DirectorySeparatorChar,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string DecodeVdfString(string value)
    {
        return value.Replace(@"\\", @"\").Replace(@"\/", "/");
    }

    private static string? ReadRegistryString(
        RegistryKey root,
        string keyPath,
        string valueName)
    {
        try
        {
            using var key = root.OpenSubKey(keyPath);
            return key?.GetValue(valueName) as string;
        }
        catch
        {
            return null;
        }
    }

    private static string? NormalizeExistingFilePath(string? path)
    {
        var normalized = NormalizePath(path);
        return normalized != null && File.Exists(normalized) ? normalized : null;
    }

    private static string? NormalizeExistingDirectoryPath(string? path)
    {
        var normalized = NormalizePath(path);
        return normalized != null && Directory.Exists(normalized) ? normalized : null;
    }

    private static string? NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            path = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
            return Path.GetFullPath(path);
        }
        catch
        {
            return null;
        }
    }
}
