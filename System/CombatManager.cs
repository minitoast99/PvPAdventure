using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Both)]
public class CombatManager : ModSystem
{
    private const bool PreventPersonalCombatModifications = true;

    public override void Load()
    {
        // Do not draw the PvP or team icons -- the server has full control over your PvP and team.
        // TODO: In the future, the server should send a packet relaying if the player can toggle hostile and which teams they may join.
        //       For now, let's just totally disable it.
        if (PreventPersonalCombatModifications && !Main.dedServ)
            On_Main.DrawPVPIcons += _ => { };

        // Re-network player hurt packets when dealing with PvP (part of our ModPlayer.ModifyHurt PvP fixes).
        // Remove player i-frames to allow ours to function.
        On_Player.Hurt_HurtInfo_bool += OnPlayerHurt;
        // Remove random damage variation.
        On_Main.DamageVar_float_int_float += OnMainDamageVar;
        // Stub this method, as our previous Player.Hurt hook does the job of this (part of our ModPlayer.ModifyHurt PvP
        // fixes).
        On_NetMessage.SendPlayerHurt_int_PlayerDeathReason_int_int_bool_bool_int_int_int +=
            (_, _, _, _, _, _, _, _, _, _) => { };
    }

    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        return PreventPersonalCombatModifications && Main.dedServ &&
               (messageType is MessageID.TogglePVP or MessageID.PlayerTeam);
    }

    private void OnPlayerHurt(On_Player.orig_Hurt_HurtInfo_bool orig, Player self, Player.HurtInfo info, bool quiet)
    {
        // This logic is originally from Player.Hurt
        var pvp = info.PvP;
        var isPvpDamageDealtByMyself = pvp && info.DamageSource.SourcePlayerIndex == Main.myPlayer;

        // We additionally now check if this is PvP damage dealt by myself -- in which case I will send the player hurt
        // packet.
        if (Main.netMode == NetmodeID.MultiplayerClient &&
            ((self.whoAmI == Main.myPlayer && !pvp) || isPvpDamageDealtByMyself) && !quiet)
        {
            if (!isPvpDamageDealtByMyself)
            {
                if (info.Knockback != 0 && info.HitDirection != 0 && (!self.mount.Active || !self.mount.Cart))
                    NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, self.whoAmI);

                NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, self.whoAmI);
            }

            NetMessage.SendPlayerHurt(self.whoAmI, info);
        }

        // Don't try to send any packets -- I just did it for you.
        quiet = true;

        orig(self, info, quiet);

        // FIXME: An IL patch might be slightly better.
        //        Doing it this way isn't great, because anything introduced in-between the i-frames being set isn't correct
        //        Meaning side effects are possible.
        //        We assume here that anyone who cares is going to care after this method comes back, not during it.
        //        IL Patching means it never has a moment to be wrong.
        if (ModContent.GetInstance<AdventureConfig>().Combat.MeleeInvincibilityFrames > 0 && info.PvP)
        {
            self.immune = false;
            self.immuneTime = 0;
        }
    }

    private int OnMainDamageVar(On_Main.orig_DamageVar_float_int_float orig, float dmg, int percent, float luck)
    {
        return (int)Math.Round(dmg);
    }
}