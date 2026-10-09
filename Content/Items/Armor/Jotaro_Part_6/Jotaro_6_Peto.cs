using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Jojo.Content.Items;
using Jojo.Content.Clases;
using Jojo.Content.Items.Potenciadores;
using Jojo.Content.Items.Armor.Jotaro_Part_3;
using Jojo.Content.Items.Armor.Jotaro_Part_4;

namespace Jojo.Content.Items.Armor.Jotaro_Part_6
{
    [AutoloadEquip(EquipType.Body)]
    public class Jotaro_6_Peto : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.value = Item.sellPrice(0, 5, 0, 0);
            Item.rare = ModContent.RarityType<Rarities.MoradoJotaro>();
            Item.defense = 20;
        }

        public override void UpdateEquip(Player player)
        {
            // +6% de daño Stand
            player.GetDamage<ClaseStand>() += 0.08f;

            // +2% de rango de Stand
            if (player.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
            {
                rangoPlayer.multiplicadorRango += 0.02f;
            }
        }

        // Texto informativo con el total real de la armadura completa
        private string ObtenerTextoBonus()
        {
            string idioma = Language.ActiveCulture.Name;

            string texto = "Full set total: +25% Stand damage, +6% Stand range";
            if (idioma.StartsWith("es")) texto = "Total del set completo: +25% daño de Stand, +6% rango de Stand";
            if (idioma.StartsWith("ru")) texto = "Итого полного сета: +25% урона Stand, +6% дальности Stand";
            if (idioma.StartsWith("pt")) texto = "Total do set completo: +25% de dano de Stand, +6% de alcance de Stand";
            if (idioma.StartsWith("ja")) texto = "フルセット合計: スタンドダメージ +25%、スタンド射程 +6%";
            if (idioma.StartsWith("zh")) texto = "整套总计：替身伤害 +25%，替身范围 +6%";

            return texto;
        }

        // Sustituye la línea automática "Bonus conjunto:" por nuestro texto limpio
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (TooltipLine line in tooltips)
            {
                if (line.Name == "SetBonus")
                {
                    line.Text = $"[c/309478:{ObtenerTextoBonus()}]";
                }
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Jotaro_4_Peto>(), 1); // Requiere Peto 4
            recipe.AddIngredient(ModContent.ItemType<Voluntad_Justa>(), 10); // 10 Virtudes
            recipe.AddIngredient(ItemID.SoulofNight, 10);                   // 10 Almas de la Noche
            recipe.AddIngredient(ItemID.HallowedBar, 15);                   // 15 Lingotes Sagrados
            recipe.AddTile(TileID.Loom);
            recipe.Register();
        }
    }
}