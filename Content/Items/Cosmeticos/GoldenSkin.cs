using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Rarities; // Espacio de nombres de tus rarezas personalizadas

namespace Jojo.Content.Items.Cosmeticos
{

    public class GoldenSkin : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(0, 5, 0, 0);

            // Reutilizamos tu rareza dorada ya existente
            Item.rare = ModContent.RarityType<RarezaDoradoJojo>();

            Item.accessory = false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddRecipeGroup("Jojo:GoldOrPlatinum", 10)
                .AddIngredient(ItemID.SilverDye, 1)
                .AddTile(TileID.DyeVat)
                .Register();
        }
    }
}