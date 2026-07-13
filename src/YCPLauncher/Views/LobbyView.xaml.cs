using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YCPLauncher.Services;

namespace YCPLauncher.Views;

public partial class LobbyView : System.Windows.Controls.UserControl
{
    public LobbyViewModel ViewModel { get; }

    public LobbyView()
    {
        InitializeComponent();
        var token = AuthService.LoadToken() ?? "";
        ViewModel = new LobbyViewModel(token);
        DataContext = ViewModel;

        ViewModel.JoinFailedValidation += () =>
        {
            Dispatcher.Invoke(() =>
            {
                if (!ConfigService.AreAnimationsEnabled)
                    return;

                var sb = (System.Windows.Media.Animation.Storyboard)Resources["ShakeAnimation"];
                sb.Begin(RoomCodeBorder);
            });
        };
        Unloaded += (_, _) => ViewModel.StopPolling();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.InitializeLobby();
    }
}

public class LobbyViewModel : INotifyPropertyChanged
{
    // ── Slots ────────────────────────────────────────────────────────────────
    public ObservableCollection<PlayerSlot> CtSlots { get; set; } = new();
    public ObservableCollection<PlayerSlot> TSlots { get; set; } = new();

    private PlayerSlot _hostSlot = new PlayerSlot { Team = "Host", Index = 0 };
    public PlayerSlot HostSlot
    {
        get => _hostSlot;
        set { _hostSlot = value; OnPropertyChanged(); }
    }

    private PlayerSlot _coachCTSlot = new PlayerSlot { Team = "CT", Index = 5, IsCoach = true };
    public PlayerSlot CoachCTSlot
    {
        get => _coachCTSlot;
        set { _coachCTSlot = value; OnPropertyChanged(); }
    }

    private PlayerSlot _coachTSlot = new PlayerSlot { Team = "T", Index = 5, IsCoach = true };
    public PlayerSlot CoachTSlot
    {
        get => _coachTSlot;
        set { _coachTSlot = value; OnPropertyChanged(); }
    }

    // ── Status Text ──────────────────────────────────────────────────────────
    private string _lobbyStatus = "匹配大厅就绪";
    public string LobbyStatus
    {
        get => _lobbyStatus;
        set { _lobbyStatus = value; OnPropertyChanged(); }
    }

    private string _lobbySubStatus = "请选择一个位置加入";
    public string LobbySubStatus
    {
        get => _lobbySubStatus;
        set { _lobbySubStatus = value; OnPropertyChanged(); }
    }

    // ── Button visibility ────────────────────────────────────────────────────
    private bool _canReady = false;
    public bool CanReady
    {
        get => _canReady;
        set { _canReady = value; OnPropertyChanged(); }
    }

    private bool _canStartGame = false;
    public bool CanStartGame
    {
        get => _canStartGame;
        set { _canStartGame = value; OnPropertyChanged(); }
    }

    public string CtCountText => $"({CtSlots.Count(s => s.IsOccupied)}/5)";
    public string TCountText  => $"({TSlots.Count(s => s.IsOccupied)}/5)";

    // ── Entry mode (create vs join) ───────────────────────────────────────────
    private bool _isInEntryMode = true;
    public bool IsInEntryMode
    {
        get => _isInEntryMode;
        set { _isInEntryMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsInRoomMode)); }
    }
    public bool IsInRoomMode => !_isInEntryMode;

    private string _roomCodeInput = "";
    public string RoomCodeInput
    {
        get => _roomCodeInput;
        set { _roomCodeInput = value?.Trim().ToUpper() ?? ""; OnPropertyChanged(); }
    }

    // Room code shown at top when in room
    private string _displayRoomCode = "";
    public string DisplayRoomCode
    {
        get => _displayRoomCode;
        set { _displayRoomCode = value; OnPropertyChanged(); }
    }

    private bool _isCreatingRoom = false;
    public bool IsCreatingRoom
    {
        get => _isCreatingRoom;
        set { _isCreatingRoom = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanJoinOrCreate)); }
    }

    private bool _isJoiningRoom = false;
    public bool IsJoiningRoom
    {
        get => _isJoiningRoom;
        set { _isJoiningRoom = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanJoinOrCreate)); }
    }

    public bool CanJoinOrCreate => !_isCreatingRoom && !_isJoiningRoom;

    private string _copyStatusText = "复制";
    public string CopyStatusText
    {
        get => _copyStatusText;
        set { _copyStatusText = value; OnPropertyChanged(); }
    }

    private bool _isRoomCodeInvalid = false;
    public bool IsRoomCodeInvalid
    {
        get => _isRoomCodeInvalid;
        set { _isRoomCodeInvalid = value; OnPropertyChanged(); }
    }

    private int _playerCount = 0;
    public int PlayerCount
    {
        get => _playerCount;
        set { _playerCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlayerCountText)); }
    }
    public string PlayerCountText => $"当前房间总人数: {PlayerCount} 人";

    private bool _isRoomCreator;
    public bool IsRoomCreator
    {
        get => _isRoomCreator;
        set { _isRoomCreator = value; OnPropertyChanged(); OnPropertyChanged(nameof(LeaveButtonText)); }
    }
    public string LeaveButtonText => IsRoomCreator ? "解散房间" : "退出房间";

    public event Action? JoinFailedValidation;

    // ── Commands ─────────────────────────────────────────────────────────────
    public ICommand JoinSlotCommand      { get; }
    public ICommand LeaveSlotCommand     { get; }
    public ICommand ReadyCommand         { get; }
    public ICommand StartGameCommand     { get; }
    public ICommand CreateNewRoomCommand { get; }
    public ICommand JoinByCodeCommand    { get; }
    public ICommand CopyRoomCodeCommand  { get; }
    public ICommand LeaveRoomCommand     { get; }

    // ── State ────────────────────────────────────────────────────────────────
    private PlayerSlot? _mySlot;
    public bool HasJoined => _mySlot != null;

    private string? _roomCode;
    private bool _isReady = false;
    private bool _hasHost = false;

    private CancellationTokenSource? _pollCts;
    private readonly MatchService _matchSvc;

    public LobbyViewModel(string token)
    {
        _matchSvc = new MatchService(token);

        JoinSlotCommand      = new RelayCommand<PlayerSlot>(slot => _ = JoinSlotAsync(slot));
        LeaveSlotCommand     = new RelayCommand(() => _ = LeaveSlotAsync());
        ReadyCommand         = new RelayCommand(() => _ = SetReadyAsync());
        StartGameCommand     = new RelayCommand(() => _ = StartGameAsync());
        CreateNewRoomCommand = new RelayCommand(() => _ = CreateRoomAsync());
        JoinByCodeCommand    = new RelayCommand(() => _ = JoinExistingRoomAsync());
        LeaveRoomCommand     = new RelayCommand(() => _ = LeaveRoomAsync());
        CopyRoomCodeCommand  = new RelayCommand(async () =>
        {
            if (!string.IsNullOrWhiteSpace(DisplayRoomCode))
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        System.Windows.Clipboard.SetText(DisplayRoomCode);
                        break;
                    }
                    catch (System.Runtime.InteropServices.COMException)
                    {
                        await Task.Delay(10);
                    }
                }
                CopyStatusText = "已复制!";
                await Task.Delay(2000);
                CopyStatusText = "复制";
            }
        });
    }

    // ── Init ─────────────────────────────────────────────────────────────────
    public void InitializeLobby()
    {
        StopPolling();
        _mySlot = null;
        _roomCode = null;
        _isReady = false;
        _hasHost = false;
        IsRoomCreator = false;
        CanReady = false;
        CanStartGame = false;
        IsCreatingRoom = false;
        IsJoiningRoom = false;
        IsRoomCodeInvalid = false;
        CopyStatusText = "复制";
        IsInEntryMode = true;
        DisplayRoomCode = "";
        RoomCodeInput = "";
        LobbyStatus = "选择模式";
        LobbySubStatus = "创建新房间，或输入朋友的房间码加入";

        CtSlots.Clear();
        TSlots.Clear();
        for (int i = 0; i < 5; i++)
        {
            CtSlots.Add(new PlayerSlot { Team = "CT", Index = i });
            TSlots.Add(new PlayerSlot { Team = "T", Index = i });
        }
        HostSlot     = new PlayerSlot { Team = "Host", Index = 0 };
        CoachCTSlot  = new PlayerSlot { Team = "CT",   Index = 5, IsCoach = true };
        CoachTSlot   = new PlayerSlot { Team = "T",    Index = 5, IsCoach = true };
        UpdateCounts();
        // Do NOT auto-create here — user picks create or join
    }

    // ── Create room ──────────────────────────────────────────────────────────
    private async Task CreateRoomAsync()
    {
        IsCreatingRoom = true;
        // Leave the previous room (stored locally) so the server doesn't block us
        var lastCode = AuthService.LoadLastRoomCode();
        if (!string.IsNullOrWhiteSpace(lastCode))
        {
            LobbyStatus = "正在清理旧会话...";
            LobbySubStatus = "请稍候";
            try { await _matchSvc.LeaveAsync(lastCode); } catch { }
            AuthService.ClearLastRoomCode();
        }

        LobbyStatus = "正在创建房间...";
        LobbySubStatus = "请稍候";

        try
        {
            var resp = await _matchSvc.CreateAsync();
            var roomCode = resp?.GetRoomCode();
            if (!string.IsNullOrWhiteSpace(roomCode))
            {
                _roomCode = roomCode;
                DisplayRoomCode = roomCode;
                AuthService.SaveLastRoomCode(roomCode);
                IsInEntryMode = false;
                IsRoomCreator = true; // Set creator flag
                LobbyStatus = "房间已就绪";
                LobbySubStatus = "把右上角的房间码发给朋友，并点击空位加入";
                StartPolling();
            }
            else
            {
                LobbyStatus = "创建房间失败";
                LobbySubStatus = resp?.Raw ?? resp?.Message ?? "请检查网络连接";
            }
        }
        catch (Exception ex)
        {
            LobbyStatus = "创建房间失败";
            LobbySubStatus = ex.Message;
        }
        finally
        {
            IsCreatingRoom = false;
        }
    }

    // ── Join existing room by code ───────────────────────────────────────────
    private async Task JoinExistingRoomAsync()
    {
        IsRoomCodeInvalid = false;
        var code = RoomCodeInput.Trim().ToUpper();
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
        {
            IsRoomCodeInvalid = true;
            JoinFailedValidation?.Invoke();
            LobbyStatus = "请输入6位房间码";
            LobbySubStatus = "向创建房间的朋友索取六位房间码";
            return;
        }

        IsJoiningRoom = true;
        LobbyStatus = $"正在加入房间 #{code}...";
        LobbySubStatus = "请稍候";

        try
        {
            // Verify room exists by fetching status
            var status = await _matchSvc.StatusAsync(code);
            if (status == null)
            {
                LobbyStatus = "房间不存在";
                LobbySubStatus = "请确认房间码是否正确";
                return;
            }
            _roomCode = code;
            DisplayRoomCode = code;
            IsInEntryMode = false;
            LobbyStatus = "房间已就绪";
            LobbySubStatus = "点击空位加入队伍";
            // Update slot UI from current status
            UpdateSlotsFromStatus(status);
            StartPolling();
        }
        catch (Exception ex)
        {
            IsRoomCodeInvalid = true;
            JoinFailedValidation?.Invoke();
            LobbyStatus = "加入房间失败";
            LobbySubStatus = ex.Message;
        }
        finally
        {
            IsJoiningRoom = false;
        }
    }

    // ── Leave room ───────────────────────────────────────────────────────────
    private async Task LeaveRoomAsync()
    {
        if (_roomCode != null)
        {
            try { await _matchSvc.LeaveAsync(_roomCode); } catch { }
            AuthService.ClearLastRoomCode();
        }
        InitializeLobby();
    }

    // ── Join slot ────────────────────────────────────────────────────────────
    private async Task JoinSlotAsync(PlayerSlot slot)
    {
        if (slot.IsOccupied) return;
        if (_roomCode == null) return;

        // Capture old slot BEFORE overwriting _mySlot
        var prevSlot = _mySlot;
        bool wasJoined = prevSlot != null;

        // Optimistically update UI
        if (prevSlot != null)
        {
            prevSlot.IsOccupied = false;
            prevSlot.IsMe = false;
            prevSlot.IsReady = false;
            prevSlot.Name = "";
        }

        var player = AuthService.LoadPlayer();
        // Use IsNullOrWhiteSpace so empty string also falls back
        string myName = string.IsNullOrWhiteSpace(player?.DisplayName)
            ? (AuthService.LoadLastUsername() ?? "Player")
            : player!.DisplayName;

        slot.Name = myName;
        slot.DisplayTag = "(你)";
        slot.IsOccupied = true;
        slot.IsMe = true;
        _mySlot = slot;
        _isReady = false;
        CanReady = true;
        OnPropertyChanged(nameof(HasJoined));
        UpdateCounts();

        try
        {
            // Directly join the new position — the backend handles team switching.
            // Do NOT leave first: leaving while alone destroys the room.
            var resp = await _matchSvc.JoinAsync(_roomCode, slot.Team);
            if (resp?.IsSuccessful() != true)
            {
                // Revert UI
                slot.IsOccupied = false;
                slot.IsMe = false;
                slot.Name = "";
                slot.DisplayTag = "";
                _mySlot = prevSlot;   // restore previous slot reference
                if (prevSlot != null)
                {
                    prevSlot.IsOccupied = true;
                    prevSlot.IsMe = true;
                    prevSlot.Name = myName;
                    prevSlot.DisplayTag = "(你)";
                }
                CanReady = wasJoined;
                OnPropertyChanged(nameof(HasJoined));
                LobbyStatus = "加入失败";
                LobbySubStatus = resp?.DisplayMessage ?? "未知错误，或者该位置暂不可用";
                UpdateCounts();
            }
            else
            {
                string teamDisplay = slot.Team switch { "CT" => "CT 阵营", "T" => "T 阵营", "Host" => "主席位", _ => slot.Team };
                LobbyStatus = $"已加入 {teamDisplay}";
                LobbySubStatus = "等待其他玩家加入，准备好后点击「已准备」";
            }
        }
        catch (Exception ex)
        {
            // Revert UI on exception
            slot.IsOccupied = false;
            slot.IsMe = false;
            slot.Name = "";
            slot.DisplayTag = "";
            _mySlot = prevSlot;
            if (prevSlot != null)
            {
                prevSlot.IsOccupied = true;
                prevSlot.IsMe = true;
                prevSlot.Name = myName;
                prevSlot.DisplayTag = "(你)";
            }
            CanReady = wasJoined;
            OnPropertyChanged(nameof(HasJoined));

            LobbyStatus = "网络错误";
            LobbySubStatus = ex.Message;
            UpdateCounts();
        }
    }

    // ── Leave slot ───────────────────────────────────────────────────────────
    private async Task LeaveSlotAsync()
    {
        if (_mySlot == null || _roomCode == null) return;

        _mySlot.IsOccupied = false;
        _mySlot.IsMe = false;
        _mySlot.IsReady = false;
        _mySlot.Name = "";
        _mySlot = null;
        _isReady = false;
        CanReady = false;
        CanStartGame = false;
        OnPropertyChanged(nameof(HasJoined));
        UpdateCounts();
        LobbyStatus = $"房间  #{_roomCode}";
        LobbySubStatus = "请选择一个位置加入";

        try
        {
            await _matchSvc.LeaveAsync(_roomCode);
            AuthService.ClearLastRoomCode();
        } catch { }
    }

    // ── Ready ────────────────────────────────────────────────────────────────
    private async Task SetReadyAsync()
    {
        if (_mySlot == null || _roomCode == null) return;

        _mySlot.IsReady = true;
        _isReady = true;
        CanReady = false;

        try
        {
            var resp = await _matchSvc.ReadyAsync(_roomCode);
            if (resp?.IsSuccessful() != true)
            {
                // Ready call rejected by server
                _mySlot.IsReady = false;
                _isReady = false;
                CanReady = true;
                LobbyStatus = "准备失败";
                LobbySubStatus = resp?.Message ?? "请重试";
                return;
            }
            _hasHost = resp?.HasHost ?? false;

            if (resp?.AllReady == true)
            {
                if (_hasHost)
                {
                    LobbyStatus = "全员准备就绪";
                    LobbySubStatus = "等待主席点击开始游戏";
                    if (_mySlot.Team == "Host")
                        CanStartGame = true;
                }
                else
                {
                    LobbyStatus = "全员准备就绪";
                    LobbySubStatus = "无主席位，即将自动开始...";
                    CanStartGame = true;
                    await StartGameAsync();
                }
            }
            else
            {
                LobbyStatus = "已准备";
                LobbySubStatus = "等待其他玩家准备...";
            }
        }
        catch (Exception ex)
        {
            LobbyStatus = "准备失败";
            LobbySubStatus = ex.Message;
        }
    }

    // ── Start game ───────────────────────────────────────────────────────────
    private async Task StartGameAsync()
    {
        if (_mySlot == null || _roomCode == null) return;

        LobbyStatus = "正在分配服务器...";
        LobbySubStatus = "请稍候";
        CanStartGame = false;

        try
        {
            var resp = await _matchSvc.StartAsync(_roomCode);
            if (resp?.ServerIp != null)
            {
                string ip = resp.ServerIp;
                int port = resp.ServerPort;
                LaunchGame(ip, port);
            }
            else
            {
                LobbyStatus = "启动失败";
                LobbySubStatus = resp?.Message ?? "服务器分配失败，请重试";
                CanStartGame = _mySlot.Team == "Host" || !_hasHost;
            }
        }
        catch (Exception ex)
        {
            LobbyStatus = "启动失败";
            LobbySubStatus = ex.Message;
            CanStartGame = _mySlot.Team == "Host" || !_hasHost;
        }
    }

    private void LaunchGame(string ip, int port)
    {
        if (_mySlot == null) return;

        LobbyStatus = "正在启动 CS2...";
        LobbySubStatus = "游戏即将自动加入服务器，请稍候";

        string teamValue = "1";
        if (_mySlot.Team == "T") teamValue = "2";
        if (_mySlot.Team == "CT") teamValue = "3";

        var player = AuthService.LoadPlayer();
        string myName = GameLauncherService.SanitizeSetInfoValue(
            player?.DisplayName ?? "Player");

        if (!GameLauncherService.LaunchDirect(ip, port, "YCP 匹配服务器",
                $"+setinfo ycp_team {teamValue} +setinfo ycp_name \"{myName}\""))
        {
            LobbyStatus = "启动失败";
            LobbySubStatus = "服务器地址无效，或 Steam 无法启动。";
        }
    }

    // ── Polling ──────────────────────────────────────────────────────────────
    private void UpdateSlotsFromStatus(MatchStatusResponse status)
    {
        PlayerCount = Math.Max(status.PlayerCount, status.Players?.Length ?? 0);
        if (status.Players != null) SyncSlots(status.Players);
        _hasHost = status.HasHost || HostSlot.IsOccupied;
        UpdateCounts();
    }

    private void StartPolling()
    {
        var next = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _pollCts, next);
        previous?.Cancel();
        previous?.Dispose();
        _ = PollLoopAsync(next.Token);
    }

    public void StopPolling()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _pollCts = null;
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(3000, ct);
                if (_roomCode == null) break;

                try
                {
                    var status = await _matchSvc.StatusAsync(_roomCode);
                    ct.ThrowIfCancellationRequested();
                    if (status == null) continue;
                    UpdateSlotsFromStatus(status);

                    // If match started from server side, launch immediately
                    if (status.Status == "playing" && status.Server != null && _mySlot != null)
                    {
                        ct.ThrowIfCancellationRequested();
                        _pollCts?.Cancel();
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            LaunchGame(status.Server.Ip, status.Server.Port));
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("不存在") || ex.Message.Contains("已结束") || ex.Message.Contains("解散"))
                    {
                        LobbyStatus = "房间已解散";
                        LobbySubStatus = "房间不存在或已被关闭";
                        _roomCode = null;
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private void SyncSlots(MatchPlayerInfo[] players)
    {
        var remotePlayers = new List<MatchPlayerInfo>(players.Length);
        bool skippedMe = false;
        foreach (var p in players)
        {
            if (!skippedMe && _mySlot != null && p.Name == _mySlot.Name && p.Team == _mySlot.Team)
            {
                skippedMe = true;
                continue;
            }

            remotePlayers.Add(p);
        }

        var host = remotePlayers.FirstOrDefault(p => p.Team == "Host");
        if (!HostSlot.IsMe)
            ApplySlotState(HostSlot, host);

        ApplyTeamState(CtSlots, remotePlayers.Where(p => p.Team == "CT"));
        ApplyTeamState(TSlots, remotePlayers.Where(p => p.Team == "T"));
        _hasHost = HostSlot.IsOccupied;

        // Check all_ready from polled state
        var occupiedPlayers = CtSlots.Concat(TSlots)
            .Where(s => s.IsOccupied)
            .ToArray();
        bool allReady = occupiedPlayers.Length > 0 &&
                        occupiedPlayers.All(s => s.IsReady);

        if (allReady && _isReady)
        {
            if (_hasHost)
            {
                LobbyStatus = "全员准备就绪";
                LobbySubStatus = "等待主席点击开始游戏";
                if (_mySlot?.Team == "Host") CanStartGame = true;
            }
            else
            {
                LobbyStatus = "全员准备就绪";
                LobbySubStatus = "无主席位，即将自动开始...";
                if (!CanStartGame) { CanStartGame = true; _ = StartGameAsync(); }
            }
        }

        UpdateCounts();
    }

    private static void ApplyTeamState(
        IEnumerable<PlayerSlot> slots,
        IEnumerable<MatchPlayerInfo> players)
    {
        var availableSlots = slots.Where(slot => !slot.IsMe).ToArray();
        var teamPlayers = players.ToArray();

        for (int index = 0; index < availableSlots.Length; index++)
        {
            ApplySlotState(
                availableSlots[index],
                index < teamPlayers.Length ? teamPlayers[index] : null);
        }
    }

    private static void ApplySlotState(PlayerSlot slot, MatchPlayerInfo? player)
    {
        slot.Name = player?.Name ?? "";
        slot.IsReady = player?.IsReady ?? false;
        slot.IsOccupied = player != null;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private void UpdateCounts()
    {
        int localPlayers = CtSlots.Count(s => s.IsOccupied) + TSlots.Count(s => s.IsOccupied);
        if (HostSlot.IsOccupied) localPlayers++;

        // Ensure at least the current user is counted as looking at the lobby
        int minPlayers = Math.Max(1, localPlayers);

        if (PlayerCount < minPlayers) PlayerCount = minPlayers;

        OnPropertyChanged(nameof(CtCountText));
        OnPropertyChanged(nameof(TCountText));
        OnPropertyChanged(nameof(PlayerCountText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

// ── PlayerSlot ───────────────────────────────────────────────────────────────
public class PlayerSlot : INotifyPropertyChanged
{
    public string Team { get; set; } = "";
    public int Index { get; set; }

    private bool _isOccupied;
    public bool IsOccupied
    {
        get => _isOccupied;
        set
        {
            if (_isOccupied == value) return;
            _isOccupied = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEmpty));
        }
    }
    public bool IsEmpty => !IsOccupied;
    public bool IsCoach { get; set; }  // visual coach designation

    private string _name = "";
    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    private string _displayTag = "";
    public string DisplayTag
    {
        get => _displayTag;
        set
        {
            if (_displayTag == value) return;
            _displayTag = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    // Full display: "PlayerName (你)" or just "PlayerName"
    public string DisplayName => string.IsNullOrWhiteSpace(_displayTag) ? _name : $"{_name} {_displayTag}";

    private bool _isMe;
    public bool IsMe
    {
        get => _isMe;
        set
        {
            if (_isMe == value) return;
            _isMe = value;
            OnPropertyChanged();
        }
    }

    private bool _isReady;
    public bool IsReady
    {
        get => _isReady;
        set
        {
            if (_isReady == value) return;
            _isReady = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

// ── RelayCommand<T> ──────────────────────────────────────────────────────────
public class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;
    private readonly Func<T, bool>? _canExecute;

    public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute == null || (parameter is T t && _canExecute(t));
    public void Execute(object? parameter) { if (parameter is T t) _execute(t); }
    public event EventHandler? CanExecuteChanged { add { } remove { } }
}

// ── RelayCommand (no param) ──────────────────────────────────────────────────
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();
    public void Execute(object? parameter) => _execute();
    public event EventHandler? CanExecuteChanged { add { } remove { } }
}
