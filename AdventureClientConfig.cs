using Terraria.ModLoader.Config;

namespace PvPAdventure;

public class AdventureClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    public PlayerOutlineConfig PlayerOutline { get; set; } = new();

    public class PlayerOutlineConfig
    {
        public bool Self { get; set; } = true;
        public bool Team { get; set; } = true;
    }
}