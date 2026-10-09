using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Rarities;

namespace Jojo.Content.Items.Cosmeticos
{
    public class StarPlatinum_Green : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ModContent.RarityType<MoradoStarPlatinum>();
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(Terraria.ID.ItemID.SilverDye, 1)
                .AddIngredient(Terraria.ID.ItemID.FallenStar, 15)
                .AddRecipeGroup("Jojo:CobaltOrPalladium", 10)
                .AddTile(Terraria.ID.TileID.DyeVat)
                .Register();
        }
    }
}