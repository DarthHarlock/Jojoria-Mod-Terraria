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

namespace Jojo.Content.Items.Armor.Jotaro_Part_4
{
    [AutoloadEquip(EquipType.Body)]
    public class Jotaro_4_Peto : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.value = Item.sellPrice(0, 5, 0, 0);
            Item.rare = ModContent.RarityType<Rarities.WhiteJotaro>();
            Item.defense = 9;
        }

        public override void UpdateEquip(Player player)
        {
            // +6% de daño Stand
            player.GetDamage<ClaseStand>() += 0.05f;

            // +2% de rango de Stand
            if (player.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
            {
                rangoPlayer.multiplicadorRango += 0.01f;
            }
        }

        // Texto informativo con el total real de la armadura completa
        private string ObtenerTextoBonus()
        {
            string idioma = Language.ActiveCulture.Name;

            string texto = "Full set total: +15% Stand damage, +4% Stand range";
            if (idioma.StartsWith("es")) texto = "Total del set completo: +15% daño de Stand, +4% rango de Stand";
            if (idioma.StartsWith("ru")) texto = "Итого полного сета: +15% урона Stand, +4% дальности Stand";
            if (idioma.StartsWith("pt")) texto = "Total do set completo: +15% de dano de Stand, +4% de alcance de Stand";
            if (idioma.StartsWith("ja")) texto = "フルセット合計: スタンドダメージ +15%、スタンド射程 +4%";
            if (idioma.StartsWith("zh")) texto = "整套总计：替身伤害 +15%，替身范围 +4%";

            return texto;
        }

        // Sustituye la línea automática "Bonus conjunto:" por nuestro texto limpio
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (TooltipLine line in tooltips)
            {
                if (line.Name == "SetBonus")
                {
                    line.Text = $"[c/E2AF82:{ObtenerTextoBonus()}]";
                }
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Jotaro_3_Peto>(), 1); // Requiere Peto 3
            recipe.AddIngredient(ItemID.Silk, 20);
            recipe.AddRecipeGroup("Jojo:CobaltOrPalladium", 12);
            recipe.AddTile(TileID.Loom);
            recipe.Register();
        }
    }
}