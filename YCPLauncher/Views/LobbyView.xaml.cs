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
                var sb = (System.Windows.Media.Animation.Storyboard)Resources["ShakeAnimation"];
                sb.Begin(RoomCodeBorder);
            });
        };
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
                System.Windows.Clipboard.SetText(DisplayRoomCode);
                CopyStatusText = "已复制!";
                await Task.Delay(2000);
                CopyStatusText = "复制";
            }
        });
    }

    // ── Init ─────────────────────────────────────────────────────────────────
    public void InitializeLobby()
    {
        _pollCts?.Cancel();
        _mySlot = null;
        _roomCode = null;
        _isReady = false;
        _hasHost = false;
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
                LobbySubStatus = resp?.Message ?? "位置已被占用";
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
            LobbyStatus = "网络错误";
            LobbySubStatus = ex.Message;
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
        string myName = player?.DisplayName ?? "Player";

        GameLauncherService.LaunchDirect(ip, port, "YCP 匹配服务器",
            $"+setinfo ycp_team {teamValue} +setinfo ycp_name \"{myName}\"");
    }

    // ── Polling ──────────────────────────────────────────────────────────────
    private void UpdateSlotsFromStatus(MatchStatusResponse status)
    {
        if (status.Players != null) SyncSlots(status.Players);
    }

    private void StartPolling()
    {
        _pollCts?.Cancel();
        _pollCts = new CancellationTokenSource();
        _ = PollLoopAsync(_pollCts.Token);
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(3000, ct).ContinueWith(_ => { }); // swallow cancel
            if (ct.IsCancellationRequested) break;
            if (_roomCode == null) break;

            try
            {
                var status = await _matchSvc.StatusAsync(_roomCode);
                if (status == null) continue;

                // If match started from server side, launch immediately
                if (status.Status == "playing" && status.Server != null && _mySlot != null)
                {
                    _pollCts?.Cancel();
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        LaunchGame(status.Server.Ip, status.Server.Port));
                    break;
                }

                // Sync slot names from server
                if (status.Players != null)
                    System.Windows.Application.Current.Dispatcher.Invoke(() => SyncSlots(status.Players));
            }
            catch { /* ignore transient errors */ }
        }
    }

    private void SyncSlots(MatchPlayerInfo[] players)
    {
        // Reset all non-me slots
        foreach (var s in CtSlots.Concat(TSlots))
        {
            if (!s.IsMe)
            {
                s.IsOccupied = false;
                s.Name = "";
                s.IsReady = false;
            }
        }
        if (!HostSlot.IsMe)
        {
            HostSlot.IsOccupied = false;
            HostSlot.Name = "";
        }

        int ctIdx = 0, tIdx = 0;
        foreach (var p in players)
        {
            // Skip my own slot – already managed locally
            if (_mySlot != null && p.Name == _mySlot.Name && p.Team == _mySlot.Team)
                continue;

            if (p.Team == "Host")
            {
                HostSlot.Name = p.Name;
                HostSlot.IsOccupied = true;
                _hasHost = true;
            }
            else if (p.Team == "CT" && ctIdx < CtSlots.Count)
            {
                var slot = CtSlots.FirstOrDefault(s => !s.IsMe && !s.IsOccupied);
                if (slot != null) { slot.Name = p.Name; slot.IsOccupied = true; slot.IsReady = p.IsReady; }
                ctIdx++;
            }
            else if (p.Team == "T" && tIdx < TSlots.Count)
            {
                var slot = TSlots.FirstOrDefault(s => !s.IsMe && !s.IsOccupied);
                if (slot != null) { slot.Name = p.Name; slot.IsOccupied = true; slot.IsReady = p.IsReady; }
                tIdx++;
            }
        }

        // Check all_ready from polled state
        bool allReady = CtSlots.All(s => s.IsReady) && TSlots.All(s => s.IsReady)
                        && CtSlots.Any(s => s.IsOccupied) && TSlots.Any(s => s.IsOccupied);

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

    // ── Helpers ──────────────────────────────────────────────────────────────
    private void UpdateCounts()
    {
        OnPropertyChanged(nameof(CtCountText));
        OnPropertyChanged(nameof(TCountText));
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
    public bool IsOccupied { get => _isOccupied; set { _isOccupied = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsEmpty)); } }
    public bool IsEmpty => !IsOccupied;
    public bool IsCoach { get; set; }  // visual coach designation

    private string _name = "";
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
    }

    private string _displayTag = "";
    public string DisplayTag
    {
        get => _displayTag;
        set { _displayTag = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
    }

    // Full display: "PlayerName (你)" or just "PlayerName"
    public string DisplayName => string.IsNullOrWhiteSpace(_displayTag) ? _name : $"{_name} {_displayTag}";

    private bool _isMe;
    public bool IsMe { get => _isMe; set { _isMe = value; OnPropertyChanged(); } }

    private bool _isReady;
    public bool IsReady { get => _isReady; set { _isReady = value; OnPropertyChanged(); } }

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
