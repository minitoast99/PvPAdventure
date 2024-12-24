using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

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

        var adventureConfig = ModContent.GetInstance<AdventureConfig>();

        // FIXME: Can't construct item definition this early...
        var statisticModification =
            adventureConfig.ItemStatisticModifications.SingleOrDefault(kv => kv.Key.Type == item.type);
        // FIXME: Dumb!!!
        if (statisticModification.Value != null)
        {
            // FIXME: Optional values!
            item.damage = statisticModification.Value.Damage;
            item.knockBack = statisticModification.Value.Knockback;
            item.defense = statisticModification.Value.Defense;
        }
    }
}