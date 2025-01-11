using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.System.Client;

[Autoload(Side = ModSide.Client)]
public class DisableOnDisconnect : ModSystem
{
    private const bool Enabled = true;
    private bool _waitingForPlayerToChangeMenu;

    public override void Load()
    {
        if (Enabled)
        {
            Netplay.OnDisconnect += OnDisconnect;
            On_Main.Update += OnUpdate;
        }
    }

    private void OnUpdate(On_Main.orig_Update orig, Main self, GameTime gametime)
    {
        orig(self, gametime);

        // We don't have to worry about the value of _waitingForPlayerToChangeMenu after this, because we're about to die.
        if (_waitingForPlayerToChangeMenu && Main.menuMode != MenuID.MultiplayerJoining)
            ((PvPAdventure)Mod).DisableOurselfAndReload();
    }

    public override void Unload()
    {
        // We have to manually remove ourself from this event, TML won't clean it up and it'll persist + leave us in memory.
        if (Enabled)
            Netplay.OnDisconnect -= OnDisconnect;
    }

    private void OnDisconnect()
    {
        // If we are on the multiplayer joining screen still, there might be some message from the server (or ourselves!) still on the
        // screen -- let's wait for the player to close it themselves.
        if (Main.menuMode == MenuID.MultiplayerJoining)
            _waitingForPlayerToChangeMenu = true;
        else
            ((PvPAdventure)Mod).DisableOurselfAndReload();
    }
}