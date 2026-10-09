using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Items;
using Jojo.Content.Clases;
using Jojo.Content.Items.Potenciadores;

namespace Jojo.Content.Items.Armor.Chariot_Requiem
{
    [AutoloadEquip(EquipType.Body)]
    public class Chariot_Requiem_Peto : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.value = Item.sellPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Purple;
            Item.defense = 14;
        }

        public override void UpdateEquip(Player player)
        {
            // +6% de rango de movimiento
            if (player.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
            {
                rangoPlayer.multiplicadorRango += 0.06f;
            }

            // +2% de daño Stand
            player.GetDamage<ClaseStand>() += 0.02f;

            // Luz Morada
            Lighting.AddLight(player.Center, 0.6f, 0.1f, 0.8f);

            // Partículas concentradas en el torso
            if (Main.rand.NextBool(40))
            {
                int d = Dust.NewDust(new Vector2(player.position.X, player.position.Y + (player.height / 4)), player.width, player.height / 2, DustID.PurpleCrystalShard, 0f, -1f, 100, default, 1f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.2f;
            }

            if (Main.rand.NextBool(15))
            {
                int d = Dust.NewDust(new Vector2(player.position.X, player.position.Y + (player.height / 4)), player.width, player.height / 2, DustID.Granite, 0f, -1f, 150, Color.Black, 1.2f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.2f;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.ObsidianShirt, 1)
                .AddIngredient(ItemID.SpectreBar, 20)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}