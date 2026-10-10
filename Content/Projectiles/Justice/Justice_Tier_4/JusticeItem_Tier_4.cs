using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Projectiles.Justice.Justice_Tier_4;
using Jojo.Content.Projectiles.Justice.Justice_Tier_3; // Para la receta
using Jojo.Content.Clases; // Arregla el error de ClaseStand
using Jojo.Systems; // Arregla el error de StandCritSystem

namespace Jojo.Content.Projectiles.Justice.Justice_Tier_4
{
    public class JusticeItem_Tier_4 : ModItem
    {
        public int baseCrit = 1;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;

            Item.damage = 0;
            Item.DamageType = ModContent.GetInstance<ClaseStand>();

            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.None;
            Item.noMelee = true;

            Item.rare = ModContent.RarityType<Rarities.AmarilloJustice>();

            Item.accessory = false;

            Item.crit = baseCrit;
            Item.prefix = -1;
        }

        public override bool AllowPrefix(int prefix) => false;

        public override int ChoosePrefix(Terraria.Utilities.UnifiedRandom rand) => -1;

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            int type = ModContent.ProjectileType<JUSTICESTAND_Tier_4>();

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

            // Multiplicamos la escala por 1.5 para mantener el tamaño personalizado
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

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient<JusticeItem_Tier_3>(1); // La Tier 3 anterior
            recipe.AddIngredient(ItemID.Ectoplasm, 30);  // Ectoplasma
            recipe.AddIngredient(ItemID.Bone, 20);       // Huesos
            recipe.Register();
        }
    }
}