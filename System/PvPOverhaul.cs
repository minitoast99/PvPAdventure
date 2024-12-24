using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Both)]
public class PvPOverhaul : ModSystem
{
    public override void Load()
    {
        On_Player.Hurt_HurtInfo_bool += OnPlayerHurt;
    }

    // FIXME: An IL patch might be slightly better.
    //        Doing it this way isn't great, because anything introduced in-between the i-frames being set isn't correct
    //        Meaning side effects are possible.
    //        We assume here that anyone who cares is going to care after this method comes back, not during it.
    //        IL Patching means it never has a moment to be wrong.
    private void OnPlayerHurt(On_Player.orig_Hurt_HurtInfo_bool orig, Player self, Player.HurtInfo info, bool quiet)
    {
        orig(self, info, quiet);

        if (info.PvP)
        {
            self.immune = false;
            self.immuneTime = 0;
        }
    }
}