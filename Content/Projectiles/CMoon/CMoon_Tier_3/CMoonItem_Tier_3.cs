using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Projectiles.CMoon.CMoon_Tier_2; // 🔥 Necesario para referenciar el Tier 2
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.CMoon.CMoon_Tier_3
{
    public class CMoonItem_Tier_3 : ModItem
    {
        public int baseCrit = 1;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;

            Item.damage = 50; // Le subí un poco el daño respecto al Tier 2, puedes cambiarlo si lo deseas
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

        // === RECETA DEL TIER 3 ===
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<CMoonItem_Tier_2>(), 1) // 1 Tier 2
                .AddIngredient(ItemID.Ectoplasm, 20)                       // 20 Ectoplasmas
                .AddIngredient(ItemID.GravitationPotion, 5)                // 5 Pociones de gravedad
                .AddIngredient(ItemID.SoulofFlight, 10)                    // 10 Almas de vuelo
                                                                           // Al igual que el Tier 2, no ponemos .AddTile(...) para que se craftee a mano
                .Register();
        }

        public override bool AllowPrefix(int prefix) => false;

        public override int ChoosePrefix(Terraria.Utilities.UnifiedRandom rand) => -1;

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 🔥 Asegúrate de tener creado este proyectil
            int type = ModContent.ProjectileType<CMOONSTAND_Tier_3>();

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
                    if (line.Text.Contains("Tier 3"))
                    {
                        line.Text = line.Text.Replace("Tier 3", "[c/F2F2F2:Tier 3]");
                    }
                    // Opcional: Si este es el último tier de C-Moon antes de Made In Heaven, 
                    // puedes mantener el "Tier Final" en el Localización (.hjson) y usar esto:
                    else if (line.Text.Contains("Tier Final"))
                    {
                        line.Text = line.Text.Replace("Tier Final", "[c/F2F2F2:Tier Final]");
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