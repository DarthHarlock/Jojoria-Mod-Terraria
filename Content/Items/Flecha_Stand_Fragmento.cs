using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Items
{
    public class Flecha_Stand_Fragmento : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 1;
            Item.value = 50000;

            Item.rare = ModContent.RarityType<Rarities.RarezaDoradoJojo>();

            Item.accessory = false;
            Item.material = true;

            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item4;
            Item.consumable = true;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                // 13 segundos x 60 tics del motor de Terraria = 780 tics
                player.AddBuff(ModContent.BuffType<Buffs.DespertandoStand>(), 3600);
            }
            return true;
        }
    }
}