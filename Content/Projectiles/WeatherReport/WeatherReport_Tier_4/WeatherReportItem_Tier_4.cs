using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Projectiles.WeatherReport;
using Jojo.Content.Clases;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_3; // Para detectar el Tier 3

namespace Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_4
{
    public class WeatherReportItem_Tier_4 : ModItem
    {
        public int baseCrit = 1;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;

            Item.damage = 25; // Puedes ajustar el daño base para este tier si quieres
            Item.DamageType = ModContent.GetInstance<ClaseStand>();

            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.None;
            Item.noMelee = true;

            Item.rare = ModContent.RarityType<Rarities.RojoWeatherReport>();

            Item.accessory = false;

            Item.crit = baseCrit;
            Item.prefix = -1;
        }

        public override bool AllowPrefix(int prefix) => false;

        public override int ChoosePrefix(Terraria.Utilities.UnifiedRandom rand) => -1;

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // Asumiendo que tu proyectil del tier 4 se llama WEATHERSTAND_Tier_4
            int type = ModContent.ProjectileType<WEATHERSTAND_Tier_4>();

            if (player.ownedProjectileCounts[type] == 0)
            {
                Projectile.NewProjectile(
                    player.GetSource_Accessory(Item),
                    player.Center,
                    Vector2.Zero,
                    type,
                    Item.damage,
                    2f,
                    player.whoAmI
                );
            }
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            // Obtenemos la textura del ítem
            Texture2D texture = Terraria.GameContent.TextureAssets.Item[Item.type].Value;

            // Multiplicamos la escala por 1.5 para mantener el tamaño personalizado sin encogerse al arrastrarlo o en el Hero's Mod
            float inventoryScale = scale * 1.5f;

            spriteBatch.Draw(
                texture,
                position,
                frame,
                drawColor,
                0f,
                origin,
                inventoryScale,
                SpriteEffects.None,
                0f
            );

            // Retornamos false para evitar que Terraria lo dibuje con la escala pequeña por defecto
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;

            int realCrit = StandCritSystem.GetFinalCritChance(player, baseCrit);

            foreach (var line in tooltips)
            {
                if (line.Mod == "Terraria" && line.Name == "ItemName")
                {
                    if (line.Text.Contains("Tier 4"))
                    {
                        line.Text = line.Text.Replace("Tier 4", "[c/F2F2F2:Tier 4]");
                    }
                }

                if (line.Name == "CritChance" && line.Mod == "Terraria")
                {
                    string critText = Terraria.Localization.Language.GetTextValue("LegacyTooltip.5");
                    line.Text = realCrit + critText;
                }
            }
        }

        // --- AQUÍ ESTÁ LA RECETA AÑADIDA ---
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            // Weather Report Tier 3
            recipe.AddIngredient(ModContent.ItemType<WeatherReportItem_Tier_3>(), 1);

            // 20 Ectoplasma (Material vanilla de la post-Plantera)
            recipe.AddIngredient(ItemID.Ectoplasm, 20);

            // 20 Almas de Vuelo
            recipe.AddIngredient(ItemID.SoulofFlight, 20);

            // 20 Bloques de Nubes
            recipe.AddIngredient(ItemID.Cloud, 20);

            // 20 Bloques de Nubes de Lluvia (mojadas)
            recipe.AddIngredient(ItemID.RainCloud, 20);

            // No requiere estación de crafteo
            recipe.Register();
        }
    }
}