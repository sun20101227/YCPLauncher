using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using System;

namespace YCPNameSync;

public class YCPNameSync : BasePlugin
{
    public override string ModuleName => "YCP Name Sync";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Antigravity AI";
    public override string ModuleDescription => "Syncs player's scoreboard name with YCP Launcher's setinfo ycp_name";

    public override void Load(bool hotReload)
    {
        // Register event for when a player fully connects
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;

        // Add a small delay to ensure player entity is fully initialized in the engine
        AddTimer(1.0f, () =>
        {
            if (!player.IsValid) return;

            // Read the hidden parameter ycp_name sent by the launcher
            string ycpName = player.GetConVarValue("ycp_name");

            if (!string.IsNullOrWhiteSpace(ycpName))
            {
                // Force overwrite the player's scoreboard name
                player.PlayerName = ycpName;
                
                // Force an update to the entity state so all clients see the new name
                Utilities.SetStateChanged(player, "CBasePlayerController", "m_iszPlayerName");

                // Send a chat message to the player
                player.PrintToChat($" \x01[\x04YCP\x01] 您的比赛名称已自动同步为: \x0C{ycpName}");
            }
            else
            {
                player.PrintToChat($" \x01[\x07WARNING\x01] 未检测到 YCP 启动器传参。如果您未通过启动器启动，请注意赛事规范！");
            }
        });

        return HookResult.Continue;
    }
}
