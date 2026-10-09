using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Projectiles.StarPlatinum;
using Jojo.Content.Clases;
using Jojo.Content.Items; // Añadido para poder llamar a Virtud_Basica

namespace Jojo.Content.Projectiles.StarPlatinum.StarPlatinum_Tier_1
{
    public class StarPlatinumItem_Tier_1 : ModItem
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

            Item.rare = ModContent.RarityType<Rarities.MoradoStarPlatinum>();

            Item.accessory = false;
            Item.material = true; // Ahora este ítem se considera un material

            Item.crit = baseCrit;
            Item.prefix = -1;
        }

        public override bool AllowPrefix(int prefix) => false;

        public override int ChoosePrefix(Terraria.Utilities.UnifiedRandom rand) => -1;

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            int type = ModContent.ProjectileType<STARPLATINUMSTAND_Tier_1>();

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

            // Multiplicamos la escala para mantener el tamaño personalizado sin encogerse al arrastrarlo o en el Hero's Mod
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
                    if (line.Text.Contains("Tier 1"))
                    {
                        line.Text = line.Text.Replace("Tier 1", "[c/F2F2F2:Tier 1]");
                    }
                }

                if (line.Name == "CritChance" && line.Mod == "Terraria")
                {
                    // FIX: LegacyTooltip.5 ya incluye el símbolo "%", así que no hay que añadirlo otra vez
                    string critText = Terraria.Localization.Language.GetTextValue("LegacyTooltip.5");
                    line.Text = realCrit + critText;
                }
            }
        }

        // === RECETA ÚNICA (ORO O PLATINO, SIN ESTACIÓN) ===
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<Voluntad_Basica>(), 10)
                .AddRecipeGroup("Jojo:GoldOrPlatinum", 10)
                .AddIngredient(ItemID.FallenStar, 10)
                .Register();
        }
    }
}