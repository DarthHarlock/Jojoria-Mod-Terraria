using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Rarities; // Importamos el espacio de nombres de tus rarezas personalizadas

namespace Jojo.Content.Items.Cosmeticos
{
    public class BlueSkin : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(0, 5, 0, 0);

            Item.rare = ModContent.RarityType<AnubisSkinAzul>();

            Item.accessory = false;
        }

        public override void AddRecipes()
        {
            // Creamos la receta para crear 1 de este objeto (BlueSkin)
            Recipe recipe = CreateRecipe();

            // 5 Zafiros
            recipe.AddIngredient(ItemID.Sapphire, 5);

            // La "planta" azul (Arándanos / Blue Berries, la que se usa para el tinte azul base)
            // (Si prefieres la Flor Azul Cielo, cambia ItemID.BlueBerries por ItemID.SkyBlueFlower)
            recipe.AddIngredient(ItemID.BlueBerries, 1);

            // La máquina de crafteo de tintes (Tina de tintes / Dye Vat)
            recipe.AddTile(TileID.DyeVat);

            // Registramos la receta en el juego
            recipe.Register();
        }
    }
}