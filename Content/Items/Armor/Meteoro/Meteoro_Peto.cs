using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Jojo.Content.Items;
using Jojo.Content.Clases;

namespace Jojo.Content.Items.Armor.Meteoro
{
    [AutoloadEquip(EquipType.Body)]
    public class Meteoro_Peto : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.value = Item.sellPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Pink;
            Item.defense = 6; // Defensa balanceada para sumar 16 en total
        }

        public override void UpdateEquip(Player player)
        {
            // +3% de daño Stand
            player.GetDamage<ClaseStand>() += 0.04f;
        }

        // Texto informativo con el total real de la armadura completa
        private string ObtenerTextoBonus()
        {
            string idioma = Language.ActiveCulture.Name;

            string texto = "Full set total: +9% Stand damage";
            if (idioma.StartsWith("es")) texto = "Total del set completo: +9% daño de Stand";
            if (idioma.StartsWith("ru")) texto = "Итого полного сета: +9% урона Stand";
            if (idioma.StartsWith("pt")) texto = "Total do set completo: +9% de dano de Stand";
            if (idioma.StartsWith("ja")) texto = "フルセット合計: スタンドダメージ +9%";
            if (idioma.StartsWith("zh")) texto = "整套总计：替身伤害 +9%";

            return texto;
        }

        // Sustituye la línea automática "Bonus conjunto:" por nuestro texto limpio
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (TooltipLine line in tooltips)
            {
                if (line.Name == "SetBonus")
                {
                    line.Text = $"[c/E57373:{ObtenerTextoBonus()}]";
                }
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<LingoteMeteoritoJojo>(), 20);
            recipe.AddTile(TileID.Anvils);
            recipe.Register();
        }
    }
}