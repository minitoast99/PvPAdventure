using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.System.Server;

// Netplay.UpdateConnectedClients calls RemoteClient.Reset before calling to NetMessage.SyncDisconnectedPlayer, which in turn calls into
// Player.Hooks.PlayerDisconnect
// The problem is that resetting the client will re-instantiate the Player instance, destroying all the data before I've had a chance to do
// anything with it regarding the disconnection. We swap the order of the calls to RemoteClient.Reset and NetMessage.SyncDisconnectedPlayer.
[Autoload(Side = ModSide.Server)]
public class FixUpdateConnectedClients : ModSystem
{
    public override void Load()
    {
        IL_Netplay.UpdateConnectedClients += OnUpdateConnectedClientsIL;
    }

    private void OnUpdateConnectedClientsIL(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the first RemoteClient.Reset call
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt<RemoteClient>("Reset"));

        // Go back 4 instructions to the beginning of what we're looking for.
        cursor.Index -= 4;

        // Remove the existing 6 instructions
        cursor.RemoveRange(6)
            .Emit(OpCodes.Ldloc_1)
            .EmitDelegate((byte index) =>
            {
                var client = Netplay.Clients[index];

                // Set the state to 0, to allow SyncDisconnectedPlayer to treat this player as if they are disconnected
                client.State = 0;
                NetMessage.SyncDisconnectedPlayer(index);
                // Still call reset as normal.
                client.Reset();
            });
    }
}