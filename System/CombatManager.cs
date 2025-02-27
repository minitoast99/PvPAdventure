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

        On_Player.Hurt_HurtInfo_bool += OnPlayerHurt;
        // Remove random damage variation.
        On_Main.DamageVar_float_int_float += OnMainDamageVar;
    }

    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        return PreventPersonalCombatModifications && Main.dedServ &&
               (messageType is MessageID.TogglePVP or MessageID.PlayerTeam);
    }

    // FIXME: An IL patch might be slightly better.
    //        Doing it this way isn't great, because anything introduced in-between the i-frames being set isn't correct
    //        Meaning side effects are possible.
    //        We assume here that anyone who cares is going to care after this method comes back, not during it.
    //        IL Patching means it never has a moment to be wrong.
    private void OnPlayerHurt(On_Player.orig_Hurt_HurtInfo_bool orig, Player self, Player.HurtInfo info, bool quiet)
    {
        orig(self, info, quiet);

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