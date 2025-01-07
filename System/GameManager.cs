using System;
using System.Linq;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Both)]
public class GameManager : ModSystem
{
    public override void Load()
    {
        // Prevent the world from entering the lunar apocalypse (killing cultist and spawning pillars)
        On_WorldGen.TriggerLunarApocalypse += _ => { };
    }

    public class TeamCommand : ModCommand
    {
        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length < 2)
                return;

            var player = Main.player
                .Where(player => player.active)
                .Where(player => player.name.Contains(args[0], StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefault();

            if (player == null)
                return;

            if (!Enum.TryParse(args[1], true, out Team team) || (int)team >= Enum.GetValues<Team>().Length)
                return;

            player.team = (int)team;
            NetMessage.SendData(MessageID.PlayerTeam, number: player.whoAmI);
        }

        public override string Command => "team";
        public override CommandType Type => CommandType.Console;
    }
}