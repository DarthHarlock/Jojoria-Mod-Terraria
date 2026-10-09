using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Rarities; // Importamos el espacio de nombres de tus rarezas personalizadas

namespace Jojo.Content.Items.Cosmeticos
{
    public class GreenSkin : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(0, 5, 0, 0);

            Item.rare = ModContent.RarityType<AnubisSkinVerde>();

            Item.accessory = false;
        }

        public override void AddRecipes()
        {
            // Creamos la receta para crear 1 de este objeto (GreenSkin)
            Recipe recipe = CreateRecipe();

            // 5 Esmeraldas
            recipe.AddIngredient(ItemID.Emerald, 5);

            // La "planta" verde (Champiñón verde / Green Mushroom, la que se usa para el tinte verde base)
            // (Si prefieres el alga lima, puedes usar ItemID.LimeKelp en su lugar)
            recipe.AddIngredient(ItemID.GreenMushroom, 1);

            // La máquina de crafteo de tintes (Tina de tintes / Dye Vat)
            recipe.AddTile(TileID.DyeVat);

            // Registramos la receta en el juego
            recipe.Register();
        }
    }
}