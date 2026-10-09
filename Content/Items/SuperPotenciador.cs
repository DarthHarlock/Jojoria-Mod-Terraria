using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Clases;

namespace Jojo.Content.Items
{
    public class SuperPotenciador : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;

            Item.maxStack = 1;

            Item.value = 187500;
            Item.rare = ItemRarityID.LightRed;

            Item.accessory = true;
            Item.material = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(ModContent.GetInstance<ClaseStand>()) += 9.15f;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // NO texto hardcodeado, SOLO orden si quieres
            // (opcional: podrías no tocar nada aquí)
        }
    }
}