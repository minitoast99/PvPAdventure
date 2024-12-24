using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure;

public class AdventureProjectile : GlobalProjectile
{
    public override bool? CanCutTiles(Projectile projectile)
    {
        if (projectile.owner == Main.myPlayer && Player.tileTargetX >= 3200)
            return false;

        return null;
    }
}