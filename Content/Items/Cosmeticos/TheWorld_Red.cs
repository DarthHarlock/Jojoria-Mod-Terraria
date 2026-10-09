using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Rarities;

namespace Jojo.Content.Items.Cosmeticos
{
    public class TheWorld_Red : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(0, 5, 0, 0);

            Item.rare = ModContent.RarityType<MoradoStarPlatinum>();

            Item.accessory = false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Bone, 20)
                .AddIngredient(ItemID.ShadowDye, 1)
                .AddTile(TileID.DyeVat)
                .Register();
        }
    }
}