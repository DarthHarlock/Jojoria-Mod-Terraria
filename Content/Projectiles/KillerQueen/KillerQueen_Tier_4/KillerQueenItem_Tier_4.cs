using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_3; // Necesario para pedir la Tier 3 en la receta
using Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_4;
using Jojo.Content.Clases;
using Jojo.Content.Items; // Necesario para la Flecha_Stand

namespace Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_4
{
    public class KillerQueenItem_Tier_4 : ModItem
    {
        public int baseCrit = 15; // Crítico aumentado para Tier 4

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;

            Item.damage = 50; // Daño escalado a nivel post-Plantera
            Item.DamageType = ModContent.GetInstance<ClaseStand>();

            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.None;
            Item.noMelee = true;

            Item.rare = ModContent.RarityType<Rarities.RosaKillerQueen>();

            Item.accessory = false;

            Item.crit = baseCrit;
            Item.prefix = -1;
        }

        // === RECETA DE CRAFTEO (Sin mesa de crafteo) ===
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<KillerQueenItem_Tier_3>(), 1) // Pide el ítem Tier 3
                .AddIngredient(ItemID.Ectoplasm, 20)                             // 🔥 20 de Ectoplasma vanilla
                .AddIngredient(ModContent.ItemType<Flecha_Stand>(), 1)           // 🔥 1 Flecha Stand
                .Register();
        }

        public override bool AllowPrefix(int prefix) => false;

        public override int ChoosePrefix(Terraria.Utilities.UnifiedRandom rand) => -1;

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            int type = ModContent.ProjectileType<KILLERQUEENSTAND_Tier_4>();

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
    }
}