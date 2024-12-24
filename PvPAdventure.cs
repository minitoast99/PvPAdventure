using System;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using PvPAdventure.System;
using PvPAdventure.System.Client.Interface;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;

namespace PvPAdventure;

public class PvPAdventure : Mod
{
    private const bool AllowLoadingWhilstDisconnected = true;

    public override void Load()
    {
        // This mod should only ever be loaded when connecting to a server, it should never be loaded beforehand.
        // We don't use Netplay.Disconnect here, as that's not initialized to true (but rather to default value, aka false), so instead
        // we'll check the connection status of our own socket.
        if (Main.netMode != NetmodeID.Server)
        {
            if (!AllowLoadingWhilstDisconnected && !Netplay.Connection.Socket.IsConnected())
                throw new Exception("This mod should only be loaded whilst connected to a server.");
        }

        if (Main.netMode == NetmodeID.Server)
        {
            ModContent.GetInstance<DiscordIdentification>().PlayerJoin += (_, args) =>
            {
                // FIXME: We should allow or deny players based on proper criteria.
                //        For now, let's allow everyone.
                args.Allowed = true;
            };
        }
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var id = reader.ReadByte();

        // FIXME: no magic numbers
        switch (id)
        {
            case 0:
            {
                var bountyTransaction = BountyManager.Transaction.Deserialize(reader);

                if (Main.netMode == NetmodeID.MultiplayerClient)
                    break;

                var bountyManager = ModContent.GetInstance<BountyManager>();

                if (bountyTransaction.Id != ModContent.GetInstance<BountyManager>().TransactionId)
                {
                    // Transaction ID doesn't match, likely out of sync. Sync now.
                    NetMessage.SendData(MessageID.WorldData, whoAmI);
                    break;
                }

                if (bountyTransaction.Team != Main.player[whoAmI].team)
                    break;

                var teamBounties = bountyManager.Bounties[(Team)bountyTransaction.Team];

                if (bountyTransaction.PageIndex >= teamBounties.Count)
                    break;

                var page = bountyManager.Bounties[(Team)bountyTransaction.Team][
                    bountyTransaction.PageIndex];

                if (bountyTransaction.BountyIndex >= page.Bounties.Count)
                    break;

                try
                {
                    var bounty = page.Bounties[bountyTransaction.BountyIndex];

                    foreach (var item in bounty)
                    {
                        var index = Item.NewItem(new BountyManager.ClaimEntitySource(), Main.player[whoAmI].position,
                            Vector2.Zero, item, true, true);
                        Main.timeItemSlotCannotBeReusedFor[index] = 54000;

                        NetMessage.SendData(MessageID.InstancedItem, whoAmI, -1, null, index);

                        Main.item[index].active = false;
                    }
                }
                finally
                {
                    bountyManager.Bounties[(Team)bountyTransaction.Team].Remove(page);
                    bountyManager.IncrementTransactionId();
                    NetMessage.SendData(MessageID.WorldData);
                }

                break;
            }
            case 1:
            {
                var statistics = AdventurePlayer.Statistics.Deserialize(reader);
                var player = Main.player[Main.netMode == NetmodeID.Server ? whoAmI : statistics.Player];

                statistics.Apply(player.GetModPlayer<AdventurePlayer>());

                // FIXME: bruh thats a little dumb maybe
                if (Main.netMode != NetmodeID.Server)
                    ModContent.GetInstance<Scoreboard>().UiScoreboard.Invalidate();

                break;
            }
            case 2:
            {
                var worldMapSyncLighting = WorldMapSyncManager.Lighting.Deserialize(reader);

                // On the server, we just forward this to everyone else.
                if (Main.netMode == NetmodeID.Server)
                {
                    var packet = GetPacket();
                    // FIXME: no magic
                    packet.Write((byte)2);
                    worldMapSyncLighting.Serialize(packet);
                    packet.Send(ignoreClient: whoAmI);
                }
                // On the client, we use it to update the lighting of the world map tiles.
                else if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    try
                    {
                        ModContent.GetInstance<WorldMapSyncManager>().ignore = true;
                        foreach (var (point, light) in worldMapSyncLighting.TileLight)
                        {
                            Main.Map.UpdateLighting(point.X, point.Y, light);

                            if (MapHelper.numUpdateTile < MapHelper.maxUpdateTile - 1)
                            {
                                MapHelper.updateTileX[MapHelper.numUpdateTile] = point.X;
                                MapHelper.updateTileY[MapHelper.numUpdateTile] = point.Y;
                                MapHelper.numUpdateTile++;
                            }
                            else
                            {
                                Main.refreshMap = true;
                            }
                        }
                    }
                    finally
                    {
                        ModContent.GetInstance<WorldMapSyncManager>().ignore = false;
                    }
                }

                break;
            }
        }
    }

    internal void DisableOurselfAndReload()
    {
        var modLoaderType = typeof(ModLoader);
        modLoaderType.GetMethod("DisableMod", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [Name]);
        modLoaderType.GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null);
    }
}