using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_1;
using Jojo.Content.Clases;
using Jojo.Content.Items;

namespace Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_1
{
    public class SilverChariotItem_Tier_1 : ModItem
    {
        public int baseCrit = 1;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;

            Item.damage = 5;
            Item.DamageType = ModContent.GetInstance<ClaseStand>();

            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.None;
            Item.noMelee = true;

            Item.rare = ModContent.RarityType<Rarities.AzulSilverChariot>();

            Item.accessory = false;

            Item.crit = baseCrit;
            Item.prefix = -1;
        }

        public override bool AllowPrefix(int prefix) => false;

        public override int ChoosePrefix(Terraria.Utilities.UnifiedRandom rand) => -1;

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            int type = ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_1>();

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
                    if (line.Text.Contains("Tier 1"))
                    {
                        line.Text = line.Text.Replace("Tier 1", "[c/F2F2F2:Tier 1]");
                    }
                }

                if (line.Name == "CritChance" && line.Mod == "Terraria")
                {
                    string critText = Terraria.Localization.Language.GetTextValue("LegacyTooltip.5");
                    line.Text = realCrit + critText;
                }
            }
        }

        // === RECETAS DE CRAFTEO ===
        public override void AddRecipes()
        {
            // 1. Receta con Plata
            Recipe recetaPlata = CreateRecipe();
            recetaPlata.AddIngredient(ItemID.SilverBar, 15);
            recetaPlata.AddIngredient(ModContent.ItemType<Voluntad_Basica>(), 10);
            recetaPlata.AddIngredient(ItemID.SilverShortsword, 1);
            // Sin AddTile = Crafteo a mano
            recetaPlata.Register();

            // 2. Receta con Tungsteno
            Recipe recetaTungsteno = CreateRecipe();
            recetaTungsteno.AddIngredient(ItemID.TungstenBar, 15);
            recetaTungsteno.AddIngredient(ModContent.ItemType<Voluntad_Basica>(), 10);
            recetaTungsteno.AddIngredient(ItemID.TungstenShortsword, 1);
            // Sin AddTile = Crafteo a mano
            recetaTungsteno.Register();
        }
    }
}