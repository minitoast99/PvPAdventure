using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace PvPAdventure
{
    public class RainSystem : ModSystem
    {
        private bool triggeredToday;

        public override void PreUpdateWorld()
        {
            if (Main.netMode == NetmodeID.Server) return;

            if (Main.dayTime && Main.time == 0)
            {
                triggeredToday = false;
            }

            if (Main.dayTime &&
                !triggeredToday &&
                Main.time >= 17524)
            {

                if (Main.rand.NextBool(3))
                {
                    StartRain();
                }
                triggeredToday = true;
            }
        }

        private void StartRain()
        {
            Main.rainTime = Main.rand.Next(3600, 18000);
            Main.raining = true;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.WorldData);
        }
    }
}