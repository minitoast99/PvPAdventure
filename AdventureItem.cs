using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure;

public class AdventureItem : GlobalItem
{
    public static readonly bool[] RecallItems =
        ItemID.Sets.Factory.CreateBoolSet(ItemID.MagicMirror, ItemID.CellPhone, ItemID.IceMirror, ItemID.Shellphone,
            ItemID.ShellphoneSpawn);

    public override void SetDefaults(Item item)
    {
        if (RecallItems[item.type])
        {
            item.useTime = 60 * 8;
            item.useAnimation = 60 * 8;
        }
    }

    public override bool CanUseItem(Item item, Player player)
    {
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
            tooltips.Add(new TooltipLine(Mod, "CombatPlayerDamageBalance", $"-{(int)((1.0f - multiplier) * 100)}% PvP damage")
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
    }
}