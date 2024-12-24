using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Graphics.Renderers;
using Terraria.ModLoader;

namespace PvPAdventure.System.Client;

[Autoload(Side = ModSide.Client)]
public class PlayerOutlines : ModSystem
{
    private delegate void CreateOutlinesDelegate(float alpha, float scale, Color borderColor);

    private CreateOutlinesDelegate _createOutlines;

    public override void Load()
    {
        _createOutlines =
            typeof(LegacyPlayerRenderer).GetMethod("CreateOutlines", BindingFlags.NonPublic | BindingFlags.Instance)
                .CreateDelegate<CreateOutlinesDelegate>(Main.PlayerRenderer);
        On_PlayerDrawLayers.DrawPlayer_RenderAllLayers += OnPlayerDrawLayersDrawPlayer_RenderAllLayers;
    }

    private void OnPlayerDrawLayersDrawPlayer_RenderAllLayers(On_PlayerDrawLayers.orig_DrawPlayer_RenderAllLayers orig,
        ref PlayerDrawSet drawinfo)
    {
        if (drawinfo.shadow == 0.0f)
        {
            var team = (Team)drawinfo.drawPlayer.team;
            if (!drawinfo.headOnlyRender && team != Team.None)
            {
                _createOutlines(1.0f, 1.0f,
                    Main.teamColor[(int)team].MultiplyRGBA(Lighting.GetColor(drawinfo.Center.ToTileCoordinates())));
            }
        }

        orig(ref drawinfo);
    }
}