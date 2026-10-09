using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Projectiles.CMoon.CMoon_Tier_1;
using Jojo.Content.Items;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.CMoon.CMoon_Tier_2
{
    public class CMoonItem_Tier_2 : ModItem
    {
        public int baseCrit = 1;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;

            Item.damage = 35;
            Item.DamageType = ModContent.GetInstance<ClaseStand>();

            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.None;
            Item.noMelee = true;

            Item.rare = ModContent.RarityType<Rarities.VerdeCmoon>();

            Item.accessory = false;

            Item.crit = baseCrit;
            Item.prefix = -1;
        }

        // === RECETA UNIFICADA CON GRUPO ===
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<CMoonItem_Tier_1>(), 1)
                .AddIngredient(ItemID.HallowedBar, 10) // 10 Lingotes sagrados
                .AddRecipeGroup("Jojo:TitaniumOrAdamantite", 10) // 10 barras de Titanio o Adamantita
                .AddIngredient(ItemID.GravitationPotion, 5) // 5 Pociones de gravedad
                .AddIngredient(ModContent.ItemType<Voluntad_Tenebrosa>(), 10) // 10 Voluntades Tenebrosas
                                                                              // No añadimos .AddTile(...) para que se pueda craftear directamente con la mano
                .Register();
        }

        public override bool AllowPrefix(int prefix) => false;

        public override int ChoosePrefix(Terraria.Utilities.UnifiedRandom rand) => -1;

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            int type = ModContent.ProjectileType<CMOONSTAND_Tier_2>();

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
                    if (line.Text.Contains("Tier 2"))
                    {
                        line.Text = line.Text.Replace("Tier 2", "[c/F2F2F2:Tier 2]");
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