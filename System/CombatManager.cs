using System;
using System.IO;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Both)]
public class CombatManager : ModSystem
{
    private const bool PreventPersonalCombatModifications = true;
    public const int PvPImmunityCooldownId = -100;

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

        // Override the cooldown counter/immunity cooldown ID and "proceed" flag for PvP damage.
        IL_Player.Hurt_PlayerDeathReason_int_int_refHurtInfo_bool_bool_int_bool_float_float_float += EditPlayerHurt2;
        // Don't presumptuously check Player.immune for projectile PvP damage -- player hurt will do it for you.
        // Don't apply statuses conditionally on the value of Player.immune.
        IL_Projectile.Damage += EditProjectileDamage;
        // Don't presumptuously check Player.immune for melee PvP damage -- player hurt will do it for you.
        IL_Player.ItemCheck_MeleeHitPVP += EditPlayerItemCheck_MeleeHitPVP;
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
    }

    private int OnMainDamageVar(On_Main.orig_DamageVar_float_int_float orig, float dmg, int percent, float luck)
    {
        return (int)Math.Round(dmg);
    }

    private void EditPlayerHurt2(ILContext il)
    {
        var cursor = new ILCursor(il);
        // Find where we load Player.immune...
        cursor.GotoNext(i => i.MatchLdfld<Player>("immune"))
            // ...where we store it off to a local...
            .GotoNext(i => i.MatchStloc0());

        // ...then after that...
        cursor.Index += 1;
        // ...prepare a delegate call.
        cursor.EmitLdarg0()
            .EmitLdarg(5)
            .EmitLdarga(7)
            .EmitLdloca(0)
            .EmitDelegate((Player self, bool pvp, ref int cooldownCounter, ref bool flag) =>
            {
                var adventurePlayer = self.GetModPlayer<AdventurePlayer>();

                if (pvp)
                {
                    // Overwrite the cooldown counter, so that if the hurt succeeds, no other counter gets modified.
                    cooldownCounter = PvPImmunityCooldownId;
                    // Set the flag deciding if this hurt should proceed.
                    flag = adventurePlayer.PvPImmuneTime == 0;
                }
            });
    }

    private void EditProjectileDamage(ILContext il)
    {
        var cursor = new ILCursor(il);
        // Find the first load of Player.immune...
        cursor.GotoNext(i => i.MatchLdfld<Player>("immune"));
        // ...and go back to where the player instance is loaded...
        cursor.Index -= 1;
        // ...to remove it, the load, and the branch.
        cursor.RemoveRange(3);

        // Find the next load of Player.immune...
        cursor.GotoNext(i => i.MatchLdfld<Player>("immune"));
        // ...and remove it...
        cursor.Remove();
        // ...to replace it's loaded value with the result of our delegate.
        cursor.EmitDelegate((Player self) => self.GetModPlayer<AdventurePlayer>().PvPImmuneTime > 0);
    }

    private void EditPlayerItemCheck_MeleeHitPVP(ILContext il)
    {
        var cursor = new ILCursor(il);
        // Find the first load of Player.immune...
        cursor.GotoNext(i => i.MatchLdfld<Player>("immune"));
        // ...and go back to where the player instance is loaded...
        cursor.Index -= 1;
        // ...to remove it, the load, and the branch.
        cursor.RemoveRange(3);
    }
}