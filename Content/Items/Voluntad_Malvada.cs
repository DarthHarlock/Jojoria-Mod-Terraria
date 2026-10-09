using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
namespace Jojo.Content.Items
{
    public class Voluntad_Malvada : ModItem
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

            // ✔ TU RAREZA CUSTOM: Aplica el color exacto #d9354e de forma nativa
            Item.rare = ModContent.RarityType<Rarities.ColorVoluntadBasica>();

            Item.accessory = false;
            Item.material = true;
        }

        public override void PostUpdate()
        {
            // 🔮 Luz morada suave (se emite mientras el ítem esté en el suelo o inventario visible en el mundo)
            Lighting.AddLight(Item.Center, 0.45f, 0.15f, 0.55f);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {

        }
    }
}