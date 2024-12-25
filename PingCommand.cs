using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure;

public class PingCommand : ModCommand
{
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        foreach (var player in Main.ActivePlayers)
        {
            var ping = player.GetModPlayer<AdventurePlayer>().Latency;
            if (ping != null)
                caller.Reply($"{player.name}: {ping.Value.Milliseconds}ms");
        }
    }

    public override string Command => "ping";
    public override CommandType Type => CommandType.Console;
}