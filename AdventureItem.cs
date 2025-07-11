using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.Utilities;

namespace PvPAdventure;

public class AdventureItem : GlobalItem
{
    public static readonly bool[] RecallItems =
        ItemID.Sets.Factory.CreateBoolSet(ItemID.MagicMirror, ItemID.CellPhone, ItemID.IceMirror, ItemID.Shellphone,
            ItemID.ShellphoneSpawn);

    public override void SetDefaults(Item item)
    {
        var adventureConfig = ModContent.GetInstance<AdventureConfig>();

        if (item.type == ItemID.LihzahrdPowerCell)
        {

            item.rare = ItemRarityID.Yellow;
        }

        if (RecallItems[item.type])
        {
            var recallTime = adventureConfig.RecallFrames;
            item.useTime = recallTime * 2;
            item.useAnimation = recallTime * 2;
        }

        // Can't construct an ItemDefinition too early -- it'll call GetName and won't be graceful on failure.
        if (ItemID.Search.TryGetName(item.type, out var name) &&
            adventureConfig.ItemStatistics.TryGetValue(new ItemDefinition(name), out var statistics))
        {
            if (statistics.Damage != null)
                item.damage = statistics.Damage.Value;
            if (statistics.UseTime != null)
                item.useTime = statistics.UseTime.Value;
            if (statistics.UseAnimation != null)
                item.useAnimation = statistics.UseAnimation.Value;
            if (statistics.ShootSpeed != null)
                item.shootSpeed = statistics.ShootSpeed.Value;
            if (statistics.Crit != null)
                item.crit = statistics.Crit.Value;
            if (statistics.Mana != null)
                item.mana = statistics.Mana.Value;
            if (statistics.Scale != null)
                item.scale = statistics.Scale.Value;
            if (statistics.Knockback != null)
                item.knockBack = statistics.Knockback.Value;
            if (statistics.Value != null)
                item.value = statistics.Value.Value;
        }
        if (item.type == ItemID.SpectrePickaxe || item.type == ItemID.ShroomiteDiggingClaw)
            item.pick = 210;
    }

    public override void SetStaticDefaults()
    {
        void AddCircularShimmerTransform(params int[] items)
        {
            for (var i = 1; i < items.Length; i++)
                ItemID.Sets.ShimmerTransformToItem[items[i - 1]] = items[i];

            ItemID.Sets.ShimmerTransformToItem[items[^1]] = items[0];
        }

        AddCircularShimmerTransform(ItemID.CursedFlame, ItemID.Ichor);
        AddCircularShimmerTransform(
            ItemID.CrystalNinjaHelmet,
            ItemID.CrystalNinjaChestplate,
            ItemID.CrystalNinjaLeggings
        );
        AddCircularShimmerTransform(
            ItemID.GladiatorHelmet,
            ItemID.GladiatorBreastplate,
            ItemID.GladiatorLeggings
        );
        AddCircularShimmerTransform(ItemID.PaladinsHammer, ItemID.PaladinsShield);
        AddCircularShimmerTransform(ItemID.MaceWhip, ItemID.Keybrand);
        AddCircularShimmerTransform(ItemID.SniperRifle, ItemID.RifleScope);
        AddCircularShimmerTransform(ItemID.Tabi, ItemID.BlackBelt);
        AddCircularShimmerTransform(ItemID.PiggyBank, ItemID.MoneyTrough);
    }

    public override bool CanUseItem(Item item, Player player)
    {
        var isUnderground = player.position.Y > Main.worldSurface * 16;
        var isHallow = player.ZoneHallow;

        if (item.type == ItemID.EmpressButterfly)
        {
            if (isUnderground)
                return false;
        }
        else if (item.type == ItemID.QueenSlimeCrystal)
        {
            if (isUnderground)
                return false;
        }
        else if (item.type == ItemID.MechanicalEye)
        {
            if (isUnderground)
                return false;
        }
        else if (item.type == ItemID.MechanicalSkull)
        {
            if (isUnderground)
                return false;
        }
        else if (item.type == ItemID.MechanicalWorm)
        {
            if (isUnderground)
                return false;
        }

        return !ModContent.GetInstance<AdventureConfig>().PreventUse
            .Any(itemDefinition => item.type == itemDefinition.Type);
    }

    // NOTE: This will not remove already-equipped accessories from players.
    public override bool CanEquipAccessory(Item item, Player player, int slot, bool modded)
    {
        return !ModContent.GetInstance<AdventureConfig>().PreventUse
            .Any(itemDefinition => item.type == itemDefinition.Type);
    }

    public override bool? CanBeChosenAsAmmo(Item ammo, Item weapon, Player player)
    {
        if (ModContent.GetInstance<AdventureConfig>().PreventUse
            .Any(itemDefinition => ammo.type == itemDefinition.Type))
            return false;

        return null;
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        var adventureConfig = ModContent.GetInstance<AdventureConfig>();

        var itemDefinition = new ItemDefinition(item.type);
        if (adventureConfig.Combat.PlayerDamageBalance.ItemDamageMultipliers.TryGetValue(itemDefinition,
                out var multiplier))
        {
            // FIXME: The mod config is very imprecise with floating points. Do some rounding to make the UI cleaner.
            tooltips.Add(new TooltipLine(Mod, "CombatPlayerDamageBalance",
                $"-{(int)((1.0f - multiplier) * 100)}% PvP damage")
            {
                IsModifier = true,
                IsModifierBad = true
            });
        }

        if (adventureConfig.PreventUse.Contains(itemDefinition))
        {
            tooltips.Add(new TooltipLine(Mod, "Disabled", Language.GetTextValue("Mods.PvPAdventure.Item.Disabled"))
            {
                OverrideColor = Color.Red
            });
        }
        else
        {
            var isUnderground = Main.LocalPlayer.position.Y > Main.worldSurface * 16;
            var isHallow = Main.LocalPlayer.ZoneHallow;
            if (item.type == ItemID.EmpressButterfly)
            {
                if (isUnderground)
                {
                    tooltips.Add(new TooltipLine(Mod, "NoUndergroundEmpressButterfly",
                        Language.GetTextValue("Mods.PvPAdventure.Item.NoUndergroundEmpressButterfly"))
                    {
                        OverrideColor = Color.Red
                    });
                }
            }
            else if (item.type == ItemID.QueenSlimeCrystal)
            {
                if (isHallow && isUnderground)
                {
                    tooltips.Add(new TooltipLine(Mod, "NoUndergroundQueenSlimeCrystal",
                        Language.GetTextValue("Mods.PvPAdventure.Item.NoUndergroundQueenSlimeCrystal"))
                    {
                        OverrideColor = Color.Red
                    });
                }
            }
        }
    }

    public override bool? PrefixChance(Item item, int pre, UnifiedRandom rand)
    {
        // Prevent the item from spawning with a prefix, being placed into a reforge window, and loading with a prefix.
        if ((pre == -1 || pre == -3 || pre > 0) && ModContent.GetInstance<AdventureConfig>().RemovePrefixes)
            return false;

        return null;
    }

    // This is likely unnecessary if we are overriding PrefixChance, but might as well.
    public override bool CanReforge(Item item) => !ModContent.GetInstance<AdventureConfig>().RemovePrefixes;
}