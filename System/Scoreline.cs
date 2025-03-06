using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Client)]
public class Scoreline : ModSystem
{
    private readonly Interface _interface = new();

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (!layers.Contains(_interface))
        {
            var layerIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Inventory");

            if (layerIndex != -1)
                layers.Insert(layerIndex + 1, _interface);
        }
    }

    public sealed class Interface() : GameInterfaceLayer("PvPAdventure: Scoreline", InterfaceScaleType.None)
    {
        protected override bool DrawSelf()
        {
            const int timerWidth = 100;
            const int timerHeight = 40;

            const int pointWidth = 50;
            const int pointHeight = 30;

            Utils.DrawInvBG(Main.spriteBatch,
                new((Main.screenWidth / 2) - (timerWidth / 2), 0, timerWidth, timerHeight),
                Main.teamColor[(int)Team.None] * 0.7f);

            if (ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing)
            {
                var timeRemaining = ModContent.GetInstance<GameManager>().TimeRemaining;
                var timer = TimeSpan.FromSeconds(timeRemaining / 60.0);

                if (ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing)
                {
                    var text = timer.ToString(@"h\:mm\:ss");
                    var metrics = ChatManager.GetStringSize(FontAssets.MouseText.Value, text, Vector2.One);
                    ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch,
                        FontAssets.MouseText.Value,
                        text,
                        new((int)((Main.screenWidth / 2.0f) - (metrics.X / 2.0f)), 10.0f),
                        Color.White,
                        0.0f,
                        Vector2.Zero,
                        Vector2.One);
                }
            }

            var offset = 100;
            foreach (var team in Enum.GetValues<Team>())
            {
                if (team == Team.None)
                    continue;

                Utils.DrawInvBG(Main.spriteBatch,
                    new((Main.screenWidth / 2) - (timerWidth / 2) + offset, 0, pointWidth, pointHeight),
                    Main.teamColor[(int)team] * 0.7f);

                var text = ModContent.GetInstance<PointsManager>().Points[team].ToString();
                var metrics = ChatManager.GetStringSize(FontAssets.MouseText.Value, text, Vector2.One);
                ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch,
                    FontAssets.MouseText.Value,
                    ModContent.GetInstance<PointsManager>().Points[team].ToString(),
                    new((int)((Main.screenWidth / 2.0f) + offset - (pointWidth / 2.0f) - (metrics.X / 2)), 6.0f),
                    Color.White,
                    0.0f,
                    Vector2.Zero,
                    Vector2.One);

                if (offset > 0)
                {
                    offset = -offset + 50;
                }
                else
                {
                    offset = -offset;
                    offset += 100;
                }
            }

            return base.DrawSelf();
        }
    }
}