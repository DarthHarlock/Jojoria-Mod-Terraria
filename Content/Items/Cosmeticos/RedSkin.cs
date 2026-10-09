using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Rarities; // Importamos el espacio de nombres de tus rarezas personalizadas

namespace Jojo.Content.Items.Cosmeticos
{
    public class RedSkin : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(0, 5, 0, 0);

            Item.rare = ModContent.RarityType<AnubisSkinRojo>();

            Item.accessory = false;
        }

        public override void AddRecipes()
        {
            // Creamos la receta para crear 1 de este objeto (RedSkin)
            Recipe recipe = CreateRecipe();

            // 5 Rubíes
            recipe.AddIngredient(ItemID.Ruby, 5);

            // La planta naranja (Raíz de sangre naranja / Orange Bloodroot)
            recipe.AddIngredient(ItemID.OrangeBloodroot, 1);

            // La máquina de crafteo de tintes (Tina de tintes / Dye Vat)
            recipe.AddTile(TileID.DyeVat);

            // Registramos la receta en el juego
            recipe.Register();
        }
    }
}