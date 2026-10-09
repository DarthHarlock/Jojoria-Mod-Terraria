using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace Jojo.Content.Items
{
    public class FlechaStandMejorada : ModItem
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
            if (player.HasBuff(ModContent.BuffType<Buffs.DespertandoStandMejorado>()))
            {
                return false;
            }

            return base.CanUseItem(player);
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                // 3600 tics = 60 segundos
                player.AddBuff(ModContent.BuffType<Buffs.DespertandoStandMejorado>(), 600); //3600

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
                .AddIngredient(ItemID.Ectoplasm, 20)
                .AddTile(TileID.Anvils) // Se hace en el Yunque
                .Register();
        }
    }
}