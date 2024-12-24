using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Both)]
public class PvPManager : ModSystem
{
    private const bool Enabled = false;

    public override void Load()
    {
        // Do not draw the PvP or team icons -- the server has full control over your PvP and team.
        // TODO: In the future, the server should send a packet relaying if the player can toggle hostile and which teams they may join.
        //       For now, let's just totally disable it.
        if (Enabled && Main.netMode != NetmodeID.Server)
            On_Main.DrawPVPIcons += _ => { };
    }

    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        return Enabled && Main.netMode == NetmodeID.Server &&
               (messageType is MessageID.TogglePVP or MessageID.PlayerTeam);
    }
}