using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Items.Cosmeticos
{
    public class StarPlatinum_Blue : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 5);

            // Aquí aplicamos la misma rareza custom que tiene tu ítem StarPlatinumItem_Tier_1
            Item.rare = ModContent.RarityType<Rarities.MoradoStarPlatinum>();
        }
    }
}