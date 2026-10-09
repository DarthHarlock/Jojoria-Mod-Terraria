using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
namespace Jojo.Content.Items
{
    public class Voluntad_Justa : ModItem
    {
        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 4));

            ItemID.Sets.AnimatesAsSoul[Item.type] = true;
            ItemID.Sets.ItemNoGravity[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 28;

            Item.maxStack = 9999;
            Item.value = 37500;

            // No especificaste una rareza nueva para esta, así que dejo la misma que Voluntad_Basica.
            // Si tienes o quieres una clase de rareza propia para esta (ej. Rarities.ColorVoluntadJusta), cámbiala aquí.
            Item.rare = ModContent.RarityType<Rarities.ColorVoluntadBasica>();

            Item.accessory = false;
            Item.material = true;
        }

        public override void PostUpdate()
        {
            // Luz naranja suave
            Lighting.AddLight(Item.Center, 0.55f, 0.32f, 0.05f);
        }

        public override void AddRecipes()
        {
            // Crafteo a mano, sin necesidad de mesa: 2 Voluntad_Basica + 1 Estrella Caída = 1 Voluntad_Justa
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<Voluntad_Basica>(), 1)
                .AddIngredient(ModContent.ItemType<LingoteMeteoritoJojo>(), 1)        
                .Register();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {

        }
    }
}