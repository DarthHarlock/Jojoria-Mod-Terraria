using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Jojo.Content.Items;
using Jojo.Content.Clases;
using Jojo.Content.Items.Potenciadores;

//Con el set completo puesto: 35 defensa, 22% rango, 6% daño.

namespace Jojo.Content.Items.Armor.Chariot_Requiem
{
    [AutoloadEquip(EquipType.Head)]
    public class Chariot_Requiem_Gorra : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.value = Item.sellPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Purple;
            Item.defense = 8;
        }

        public override void UpdateEquip(Player player)
        {
            // +8% de rango de movimiento
            if (player.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
            {
                rangoPlayer.multiplicadorRango += 0.08f;
            }

            // +2% de daño Stand
            player.GetDamage<ClaseStand>() += 0.02f;

            // Luz Morada
            Lighting.AddLight(player.Center, 0.6f, 0.1f, 0.8f);

            // Partículas concentradas en la cabeza
            if (Main.rand.NextBool(40))
            {
                int d = Dust.NewDust(new Vector2(player.position.X, player.position.Y - 4), player.width, player.height / 2, DustID.PurpleCrystalShard, 0f, -1f, 100, default, 1f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.2f;
            }

            if (Main.rand.NextBool(15))
            {
                int d = Dust.NewDust(new Vector2(player.position.X, player.position.Y - 4), player.width, player.height / 2, DustID.Granite, 0f, -1f, 150, Color.Black, 1.2f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.2f;
            }
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<Chariot_Requiem_Peto>() &&
                   legs.type == ModContent.ItemType<Chariot_Requiem_Pantalones>();
        }

        public override void UpdateArmorSet(Player player)
        {
            // +2% de rango extra por el set completo (SIN bonus de daño de set)
            if (player.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
            {
                rangoPlayer.multiplicadorRango += 0.02f;
            }

            string idioma = Language.ActiveCulture.Name;

            // Línea 1: bonus exclusivo del set completo (+2% rango)
            string textoBonus = "+2% Stand range";
            if (idioma.StartsWith("es")) textoBonus = "+2% rango del Stand";
            if (idioma.StartsWith("ru")) textoBonus = "+2% дальности Stand";
            if (idioma.StartsWith("pt")) textoBonus = "+2% de alcance do Stand";
            if (idioma.StartsWith("ja")) textoBonus = "フルセットボーナス: スタンドの射程 +2%";
            if (idioma.StartsWith("zh")) textoBonus = "完整套装奖励：+2% 替身活动范围";

            // Línea 2: total de daño y rango que otorga TODA la armadura equipada
            // (6% daño = 2+2+2 de cada pieza; 22% rango = 6+6+8+2 del bonus de set)
            string textoTotal = "Full set total: +6% Stand damage, +22% Stand range";
            if (idioma.StartsWith("es")) textoTotal = "Total del set completo: +6% daño de Stand, +22% rango de Stand";
            if (idioma.StartsWith("ru")) textoTotal = "Итого полного сета: +6% урона Stand, +22% дальности Stand";
            if (idioma.StartsWith("pt")) textoTotal = "Total do set completo: +6% de dano de Stand, +22% de alcance de Stand";
            if (idioma.StartsWith("ja")) textoTotal = "フルセット合計: スタンドダメージ +6%、スタンド射程 +22%";
            if (idioma.StartsWith("zh")) textoTotal = "整套总计：替身伤害 +6%，替身范围 +22%";

            player.setBonus = $"[c/6A1B9A:{textoBonus}]\n[c/9C27B0:{textoTotal}]";
        }

        public override void ArmorSetShadows(Player player) { }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.SpectreBar, 12)
                .AddIngredient(ItemID.ObsidianHelm, 1)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}