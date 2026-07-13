using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using System;

namespace YCPNameSync;

public class YCPNameSync : BasePlugin
{
    public override string ModuleName    => "YCP Name Sync";
    public override string ModuleVersion =>
        GetType().Assembly.GetName().Version?.ToString(3) ?? "1.1.7";
    public override string ModuleAuthor  => "Antigravity AI";
    public override string ModuleDescription => "Syncs player name and auto-assigns team/coach role via YCP Launcher";

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        // Small delay so the player entity is fully initialised in the engine
        AddTimer(1.0f, () =>
        {
            if (!player.IsValid) return;

            string ycpName    = player.GetConVarValue("ycp_name");
            string ycpTeamStr = player.GetConVarValue("ycp_team");

            // ── Name sync ─────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(ycpName))
            {
                player.PlayerName = ycpName;
                Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");
            }

            // ── Team / role assignment ─────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(ycpTeamStr) && int.TryParse(ycpTeamStr, out int teamId))
            {
                switch (teamId)
                {
                    // 1 = Spectator / Host / 主席 / 观战
                    case 1:
                        player.ChangeTeam(CsTeam.Spectator);
                        if (!string.IsNullOrWhiteSpace(ycpName))
                            player.PrintToChat($" \x01[\x04YCP\x01] 已分配至 \x0C观战席\x01，名称同步: \x0C{ycpName}");
                        break;

                    // 2 = Terrorist (T)
                    case 2:
                        player.ChangeTeam(CsTeam.Terrorist);
                        if (!string.IsNullOrWhiteSpace(ycpName))
                            player.PrintToChat($" \x01[\x04YCP\x01] 已加入 \x02T 阵营\x01，名称同步: \x0C{ycpName}");
                        break;

                    // 3 = Counter-Terrorist (CT)
                    case 3:
                        player.ChangeTeam(CsTeam.CounterTerrorist);
                        if (!string.IsNullOrWhiteSpace(ycpName))
                            player.PrintToChat($" \x01[\x04YCP\x01] 已加入 \x04CT 阵营\x01，名称同步: \x0C{ycpName}");
                        break;

                    // 4 = CT Coach / CT 教练
                    case 4:
                        player.ChangeTeam(CsTeam.CounterTerrorist);
                        // Execute coach command after team change settles
                        AddTimer(0.5f, () =>
                        {
                            if (player.IsValid)
                                player.ExecuteClientCommandFromServer("coach ct");
                        });
                        if (!string.IsNullOrWhiteSpace(ycpName))
                            player.PrintToChat($" \x01[\x04YCP\x01] 已分配为 \x04CT 教练\x01，名称同步: \x0C{ycpName}");
                        break;

                    // 5 = T Coach / T 教练
                    case 5:
                        player.ChangeTeam(CsTeam.Terrorist);
                        AddTimer(0.5f, () =>
                        {
                            if (player.IsValid)
                                player.ExecuteClientCommandFromServer("coach t");
                        });
                        if (!string.IsNullOrWhiteSpace(ycpName))
                            player.PrintToChat($" \x01[\x04YCP\x01] 已分配为 \x02T 教练\x01，名称同步: \x0C{ycpName}");
                        break;

                    // Fallback: legacy numeric mapping for CsTeam values directly
                    default:
                        if (teamId == (int)CsTeam.Terrorist ||
                            teamId == (int)CsTeam.CounterTerrorist ||
                            teamId == (int)CsTeam.Spectator)
                        {
                            player.ChangeTeam((CsTeam)teamId);
                        }
                        if (!string.IsNullOrWhiteSpace(ycpName))
                            player.PrintToChat($" \x01[\x04YCP\x01] 名称已同步: \x0C{ycpName}");
                        break;
                }
            }
            else if (!string.IsNullOrWhiteSpace(ycpName))
            {
                // Name was set but no team param — still sync name and notify
                player.PrintToChat($" \x01[\x04YCP\x01] 名称已同步: \x0C{ycpName}");
            }
            else
            {
                // No YCP params at all
                player.PrintToChat($" \x01[\x07WARNING\x01] 未检测到 YCP 启动器传参。如果您未通过启动器启动，请注意赛事规范！");
            }
        });

        return HookResult.Continue;
    }
}
