using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Projectiles.D4C.D4C_Tier_3; // Necesario para la receta
using Jojo.Content.Projectiles.D4C.D4C_Tier_4;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.D4C.D4C_Tier_4
{
    public class D4CItem_Tier_4 : ModItem
    {
        public int baseCrit = 1;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;

            // Incremento de daño para el Tier Final (60 es un valor sugerido, puedes ajustarlo)
            Item.damage = 60;
            Item.DamageType = ModContent.GetInstance<ClaseStand>();

            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.None;
            Item.noMelee = true;

            Item.rare = ModContent.RarityType<Rarities.RosaD4C>();

            Item.accessory = false;

            Item.crit = baseCrit;
            Item.prefix = -1;
        }

        // === RECETA DE CRAFTEO ===
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<D4CItem_Tier_3>(), 1)
                .AddIngredient(ItemID.Ectoplasm, 20)
                .Register();
        }

        public override bool AllowPrefix(int prefix) => false;

        public override int ChoosePrefix(Terraria.Utilities.UnifiedRandom rand) => -1;

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            int type = ModContent.ProjectileType<D4CSTAND_Tier_4>();

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

            // Multiplicamos la escala por 2 para que mantenga el tamaño grande
            float inventoryScale = scale * 2f;

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