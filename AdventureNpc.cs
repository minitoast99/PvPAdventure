using System.Linq;
using PvPAdventure.System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure;

public class AdventureNpc : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public DamageInfo LastDamageFromPlayer { get; set; }

    public class DamageInfo(byte who)
    {
        public byte Who { get; } = who;
    }

    public override void Load()
    {
        if (Main.dedServ)
            On_NPC.PlayerInteraction += OnNPCPlayerInteraction;
    }

    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        if (npc.isLikeATownNPC)
            npc.dontTakeDamage = true;
    }

    private static void OnNPCPlayerInteraction(On_NPC.orig_PlayerInteraction orig, NPC self, int player)
    {
        orig(self, player);

        // If this is part of the Eater of Worlds, then mark ALL segments as last damaged by this player.
        if (IsPartOfEaterOfWorlds((short)self.type))
        {
            foreach (var npc in Main.ActiveNPCs)
            {
                if (!IsPartOfEaterOfWorlds((short)npc.type))
                    continue;

                npc.GetGlobalNPC<AdventureNpc>().LastDamageFromPlayer = new DamageInfo((byte)player);
            }
        }
        else
        {
            self.GetGlobalNPC<AdventureNpc>().LastDamageFromPlayer = new DamageInfo((byte)player);
        }
    }

    public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
    {
        var config = ModContent.GetInstance<AdventureConfig>();

        if (npc.boss &&
            config.BossInvulnerableProjectiles.Any(projectileDefinition =>
                projectileDefinition.Type == projectile.type))
            return false;

        return null;
    }

    public override void OnKill(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        var lastDamageInfo = npc.GetGlobalNPC<AdventureNpc>().LastDamageFromPlayer;
        if (lastDamageInfo == null)
            return;

        var lastDamager = Main.player[lastDamageInfo.Who];
        if (lastDamager == null || !lastDamager.active)
            return;

        ModContent.GetInstance<PointsManager>().AwardNpcKillToTeam((Team)lastDamager.team, npc);
    }

    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        if (ModContent.GetInstance<GameManager>()?.CurrentPhase == GameManager.Phase.Waiting)
            maxSpawns = 0;
    }

    public static bool IsPartOfEaterOfWorlds(short type) =>
        type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail;
}