using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Items.Cosmeticos
{
    public class OVASkin : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ModContent.RarityType<Rarities.OVAStarPlatinum>();
            Item.accessory = false;
        }
    }
}