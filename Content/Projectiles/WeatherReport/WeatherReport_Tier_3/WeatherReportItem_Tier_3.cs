using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Projectiles.WeatherReport;
using Jojo.Content.Clases;
using Jojo.Content.Items; // Añadido para Voluntad_Bondadosa
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_2; // Añadido para WeatherReportItem_Tier_2

namespace Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_3
{
    public class WeatherReportItem_Tier_3 : ModItem
    {
        public int baseCrit = 1;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;

            Item.damage = 15;
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
            int type = ModContent.ProjectileType<WEATHERSTAND_Tier_3>();

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
            Texture2D texture = Terraria.GameContent.TextureAssets.Item[Item.type].Value;
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
                    if (line.Text.Contains("Tier 3"))
                    {
                        line.Text = line.Text.Replace("Tier 3", "[c/F2F2F2:Tier 3]");
                    }
                }

                if (line.Name == "CritChance" && line.Mod == "Terraria")
                {
                    string critText = Terraria.Localization.Language.GetTextValue("LegacyTooltip.5");
                    line.Text = realCrit + critText;
                }
            }
        }

        // --- RECETA CON EL GRUPO TITANIO/ADAMANTITA AÑADIDO ---
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            // Weather Report Tier 2
            recipe.AddIngredient(ModContent.ItemType<WeatherReportItem_Tier_2>(), 1);

            // 10 Voluntades Bondadosas
            recipe.AddIngredient(ModContent.ItemType<Voluntad_Bondadosa>(), 10);

            // 10 Barras Sagradas (Hallowed Bars)
            recipe.AddIngredient(ItemID.HallowedBar, 10);

            // 10 Barras de Titanio o Adamantita (faltaba, añadido)
            recipe.AddRecipeGroup("Jojo:TitaniumOrAdamantite", 10);

            // 20 Almas de Vuelo
            recipe.AddIngredient(ItemID.SoulofFlight, 20);

            // 30 Bloques de Nubes
            recipe.AddIngredient(ItemID.Cloud, 30);

            // 30 Bloques de Nubes de Lluvia (mojadas)
            recipe.AddIngredient(ItemID.RainCloud, 30);

            // No requiere estación de crafteo
            recipe.Register();
        }
    }
}