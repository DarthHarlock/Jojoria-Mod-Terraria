using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_2;
using Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_3;
using Jojo.Content.Clases;
using Jojo.Content.Items;

namespace Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_3
{
    public class KillerQueenItem_Tier_3 : ModItem
    {
        public int baseCrit = 10;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;

            Item.damage = 30;
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

        // === RECETA UNIFICADA CON GRUPO (SIN mesa de crafteo) ===
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<KillerQueenItem_Tier_2>(), 1)
                .AddIngredient(ModContent.ItemType<Voluntad_Tenebrosa>(), 10)
                .AddRecipeGroup("Jojo:TitaniumOrAdamantite", 10)
                .AddIngredient(ItemID.HallowedBar, 10)
                .AddIngredient(ModContent.ItemType<Alma_Npc>(), 1)
                .Register();
        }

        public override bool AllowPrefix(int prefix) => false;

        public override int ChoosePrefix(Terraria.Utilities.UnifiedRandom rand) => -1;

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            int type = ModContent.ProjectileType<KILLERQUEENSTAND_Tier_3>();

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
    }
}