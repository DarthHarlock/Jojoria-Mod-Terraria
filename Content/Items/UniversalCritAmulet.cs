using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Items
{
    public class UniversalCritAmulet : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;

            Item.accessory = true;
            Item.rare = ItemRarityID.Red;

            Item.value = Item.buyPrice(0, 10, 0, 0);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // +100% CRIT GLOBAL (TODO: melee, ranged, magic, summon, whips, etc.)
            player.GetCritChance(DamageClass.Generic) += 100;
        }
    }
}