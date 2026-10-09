using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace Jojo.Content.Items
{
    public class Flecha_Stand : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 6;
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

        // Evita que se pueda usar otra flecha mientras ya tiene el buff activo
        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(ModContent.BuffType<Buffs.DespertandoStand>()))
            {
                return false;
            }

            return base.CanUseItem(player);
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                // 13 segundos x 60 tics del motor de Terraria = 780 tics
                player.AddBuff(ModContent.BuffType<Buffs.DespertandoStand>(), 600);

                // Se recibe 1 punto de daño, como si te hubieran golpeado
                player.Hurt(PlayerDeathReason.LegacyDefault(), 1, 0, pvp: false, quiet: false, dodgeable: false);
            }
            return true;
        }

        // Añadimos el crafteo solicitado
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<LingoteMeteoritoJojo>(), 10)
                .AddIngredient(ModContent.ItemType<Voluntad_Basica>(), 10)
                .AddTile(TileID.Anvils) // Se hace en el Yunque
                .Register();
        }
    }
}