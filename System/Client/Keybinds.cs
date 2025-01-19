using Microsoft.Xna.Framework.Input;
using Terraria.ModLoader;

namespace PvPAdventure.System.Client;

[Autoload(Side = ModSide.Client)]
public class Keybinds : ModSystem
{
    public ModKeybind Scoreboard { get; private set; }
    public ModKeybind BountyShop { get; private set; }
    public ModKeybind TeamChat { get; private set; }

    public override void Load()
    {
        Scoreboard = KeybindLoader.RegisterKeybind(Mod, "Scoreboard", Keys.OemTilde);
        BountyShop = KeybindLoader.RegisterKeybind(Mod, "BountyShop", Keys.P);
        TeamChat = KeybindLoader.RegisterKeybind(Mod, "TeamChat", Keys.U);
    }
}