using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Rarities;

namespace Jojo.Content.Items.Cosmeticos
{
    public class KillerQueen_Green : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(0, 5, 0, 0);

            Item.rare = ModContent.RarityType<RarezaDoradoJojo>();

            Item.accessory = false;
        }
    }
}