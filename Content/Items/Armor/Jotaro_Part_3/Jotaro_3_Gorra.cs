using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Jojo.Content.Items;
using Jojo.Content.Clases;
using Jojo.Content.Items.Potenciadores;

namespace Jojo.Content.Items.Armor.Jotaro_Part_3
{
    [AutoloadEquip(EquipType.Head)]
    public class Jotaro_3_Gorra : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.value = Item.sellPrice(0, 5, 0, 0);
            Item.rare = ModContent.RarityType<Rarities.AmarilloJotaro>();
            Item.defense = 3;
        }

        public override void UpdateEquip(Player player)
        {
            // +2% de daño Stand
            player.GetDamage<ClaseStand>() += 0.02f;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<Jotaro_3_Peto>() &&
                   legs.type == ModContent.ItemType<Jotaro_3_Pantalones>();
        }

        // Texto informativo con el total real de la armadura completa
        private string ObtenerTextoBonus()
        {
            string idioma = Language.ActiveCulture.Name;

            string texto = "Full set total: +6% Stand damage";
            if (idioma.StartsWith("es")) texto = "Total del set completo: +6% daño de Stand";
            if (idioma.StartsWith("ru")) texto = "Итого полного сета: +6% урона Stand";
            if (idioma.StartsWith("pt")) texto = "Total do set completo: +6% de dano de Stand";
            if (idioma.StartsWith("ja")) texto = "フルセット合計: スタンドダメージ +6%";
            if (idioma.StartsWith("zh")) texto = "整套总计：替身伤害 +6%";

            return texto;
        }

        public override void UpdateArmorSet(Player player)
        {
            // Sin bonus de stats por set completo. Solo texto informativo.
            player.setBonus = $"[c/6A1B9A:{ObtenerTextoBonus()}]";
        }

        // Sustituye la línea automática "Bonus conjunto:" por nuestro texto limpio
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (TooltipLine line in tooltips)
            {
                if (line.Name == "SetBonus")
                {
                    line.Text = $"[c/6A1B9A:{ObtenerTextoBonus()}]";
                }
            }
        }

        public override void ArmorSetShadows(Player player) { }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Voluntad_Basica>(), 10);
            recipe.AddIngredient(ItemID.Silk, 15);
            recipe.AddTile(TileID.Loom);
            recipe.Register();
        }
    }
}