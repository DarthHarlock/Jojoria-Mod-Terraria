using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.UI;
using Jojo.Content.Projectiles.CMoon.CMoon_Tier_1;

using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_1;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_2;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_3;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_4;

namespace Jojo.Content.Items
{
    public class GreenBaby : ModItem
    {
        private static readonly string[] WhitesnakeItemNames =
        {
            "WhiteSnakeItem_Tier_1",
            "WhiteSnakeItem_Tier_2",
            "WhiteSnakeItem_Tier_3",
            "WhiteSnakeItem_Tier_4"
        };

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            // Hitbox ajustada (16x16)
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(silver: 2);
            Item.rare = ModContent.RarityType<Rarities.VerdeBebeVerde>();

            // Escala del sprite en el inventario / al usarlo
            Item.scale = 0.5f;

            // Consumible
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item29;
            Item.consumable = true;
        }

        // Solo se puede usar con Whitesnake equipado (Tier 1, 2, 3 o 4)
        public override bool CanUseItem(Player player)
        {
            var sp = player.GetModPlayer<StandSlotPlayer>();

            if (sp.standItem == null || sp.standItem.IsAir || sp.standItem.ModItem == null)
                return false;

            string name = sp.standItem.ModItem.Name;
            foreach (string w in WhitesnakeItemNames)
            {
                if (name == w)
                    return true;
            }
            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            // 36 Almas
            recipe.AddIngredient(ModContent.ItemType<Alma_Npc>(), 36);

            // Estación: Altar Demoníaco (funciona con el Altar Demoníaco y el Carmesí)
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            // Mantiene el ítem cerca de la cara al comerlo
            player.itemLocation.Y -= 18f;
            player.itemLocation.X -= player.direction * 4f;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Item[Item.type].Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = frame.Size() / 2f;

            float customScale = 0.5f; // Forzamos la escala visual a la mitad

            Vector2 drawPos = Item.Bottom - Main.screenPosition;
            drawPos.Y -= (frame.Height * customScale) / 2f;

            spriteBatch.Draw(texture, drawPos, frame, lightColor, rotation, origin, customScale, SpriteEffects.None, 0f);

            return false;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
                return true;

            var sp = player.GetModPlayer<StandSlotPlayer>();

            // 1) Matar el proyectil de Whitesnake activo (cualquier tier)
            foreach (Projectile proj in Main.projectile)
            {
                if (!proj.active || proj.owner != player.whoAmI) continue;

                if (proj.type == ModContent.ProjectileType<WHITESNAKESTAND_Tier_1>() ||
                    proj.type == ModContent.ProjectileType<WHITESNAKESTAND_Tier_2>() ||
                    proj.type == ModContent.ProjectileType<WHITESNAKESTAND_Tier_3>() ||
                    proj.type == ModContent.ProjectileType<WHITESNAKESTAND_Tier_4>())
                {
                    proj.Kill();
                }
            }

            // 2) Reemplazar Whitesnake por C-Moon Tier 1 en el slot de Stand
            if (ModContent.TryFind<ModItem>("Jojo", "CMoonItem_Tier_1", out ModItem cmoonModItem))
            {
                Item newStand = new Item();
                newStand.SetDefaults(cmoonModItem.Type);
                sp.standItem = newStand;
            }

            // 3) Invocar a C-Moon
            Projectile.NewProjectile(
                player.GetSource_ItemUse(Item),
                player.Center,
                Vector2.Zero,
                ModContent.ProjectileType<CMOONSTAND_Tier_1>(),
                0,
                0f,
                player.whoAmI
            );

            // 4) Ráfaga verde
            SpawnGreenBurst(player);

            return true;
        }

        private void SpawnGreenBurst(Player player)
        {
            float radius = 40f;

            // Anillo exterior
            for (int i = 0; i < 30; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                Dust ringDust = Dust.NewDustPerfect(player.Center + offset, DustID.ChlorophyteWeapon, Vector2.Zero, 100, Color.LimeGreen, 1.5f);
                ringDust.noGravity = true;
            }

            // Partículas internas
            for (int i = 0; i < 12; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(radius, radius);
                Dust innerDust = Dust.NewDustPerfect(player.Center + offset, DustID.GreenFairy, new Vector2(0f, -3f), 100, Color.LimeGreen, 1.3f);
                innerDust.noGravity = true;
            }
        }
    }
}