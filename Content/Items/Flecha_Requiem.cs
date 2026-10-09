using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.UI; // StandSlotPlayer

using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Requiem;
using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_3;
using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_4;

using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_3;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_4;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem;

namespace Jojo.Content.Items
{
    public class Flecha_Requiem : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 99;
            Item.value = Item.sellPrice(gold: 5);
            Item.rare = ItemRarityID.Purple;

            // En lugar de comer, el personaje levantará la flecha majestuosamente
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item29; // Sonido místico
            Item.consumable = true;
        }

        // Solo se puede usar si el jugador tiene:
        // - Silver Chariot Tier 3 o Tier 4    -> evoluciona a Silver Chariot Requiem
        // - Golden Experience Tier 3 o Tier 4 -> evoluciona a Golden Experience Requiem
        public override bool CanUseItem(Player player)
        {
            var sp = player.GetModPlayer<StandSlotPlayer>();

            if (sp.standItem == null || sp.standItem.IsAir || sp.standItem.ModItem == null)
                return false;

            string name = sp.standItem.ModItem.Name;
            return name == "SilverChariotItem_Tier_3" ||
                   name == "SilverChariotItem_Tier_4" ||
                   name == "GoldenItem_Tier_3" ||
                   name == "GoldenItem_Tier_4";
        }

        public override bool? UseItem(Player player)
        {
            // Ejecutar solo en el cliente que usa el ítem
            if (player.whoAmI != Main.myPlayer)
                return true;

            var sp = player.GetModPlayer<StandSlotPlayer>();

            if (sp.standItem == null || sp.standItem.IsAir || sp.standItem.ModItem == null)
                return true;

            string standName = sp.standItem.ModItem.Name;
            bool esSilverChariot = standName == "SilverChariotItem_Tier_3" || standName == "SilverChariotItem_Tier_4";
            bool esGoldenExperience = standName == "GoldenItem_Tier_3" || standName == "GoldenItem_Tier_4";

            if (esSilverChariot)
            {
                // ── RUTA SILVER CHARIOT -> SILVER CHARIOT REQUIEM ──

                // 1) Matar proyectiles de Silver Chariot Tier 3 y Tier 4 activos
                foreach (Projectile proj in Main.projectile)
                {
                    if (!proj.active || proj.owner != player.whoAmI) continue;

                    // FIX: el nombre real de la clase del Tier 3 también lleva el typo
                    // "SLIVER" (igual que Tier 1, 2 y 4), no "SILVER". Solo el Requiem
                    // usa la ortografía correcta.
                    if (proj.type == ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_3>() ||
                        proj.type == ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_4>())
                    {
                        proj.Kill();
                    }
                }

                // 2) Reemplazar en el slot de Stand por Silver Chariot Requiem
                if (ModContent.TryFind<ModItem>("Jojo", "SilverChariotRequiemItem_Requiem", out ModItem requiemModItem))
                {
                    Item newStand = new Item();
                    newStand.SetDefaults(requiemModItem.Type);
                    sp.standItem = newStand;
                }

                // 3) Invocar automáticamente el proyectil de Requiem
                Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<SILVERCHARIOTSTAND_Requiem>(),
                    0, // Daño base inicial, el accesorio lo ajustará luego
                    0f,
                    player.whoAmI
                );

                // 4) Ráfaga instantánea de partículas moradas
                SpawnBurst(player, DustID.Shadowflame, Color.Purple, DustID.PurpleCrystalShard, Color.Violet);
            }
            else if (esGoldenExperience)
            {
                // ── RUTA GOLDEN EXPERIENCE TIER 3/4 -> GOLDEN EXPERIENCE REQUIEM ──

                // 1) Matar proyectiles de Golden Experience Tier 3 y Tier 4 activos
                foreach (Projectile proj in Main.projectile)
                {
                    if (!proj.active || proj.owner != player.whoAmI) continue;

                    if (proj.type == ModContent.ProjectileType<GOLDENSTAND_Tier_3>() ||
                        proj.type == ModContent.ProjectileType<GOLDENSTAND_Tier_4>())
                    {
                        proj.Kill();
                    }
                }

                // 2) Reemplazar en el slot de Stand por Golden Experience Requiem
                if (ModContent.TryFind<ModItem>("Jojo", "GoldenItem_Requiem", out ModItem goldenRequiemItem))
                {
                    Item newStand = new Item();
                    newStand.SetDefaults(goldenRequiemItem.Type);
                    sp.standItem = newStand;
                }

                // 3) Invocar automáticamente el proyectil de Golden Experience Requiem
                Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<GOLDENSTAND_Requiem>(),
                    0, // Daño base inicial, el accesorio lo ajustará luego
                    0f,
                    player.whoAmI
                );

                // 4) Ráfaga instantánea de partículas DORADAS (en vez de moradas)
                SpawnBurst(player, DustID.GoldFlame, Color.Gold, DustID.GoldCoin, Color.Yellow);
            }

            return true;
        }

        // Ráfaga de partículas genérica: reutiliza los mismos tipos de dust que antes
        // (ring dust para el anillo exterior, inner dust para el interior)
        // pero permite pintarlos del color que corresponda según el Stand evolucionado.
        private void SpawnBurst(Player player, int ringDustType, Color ringColor, int innerDustType, Color innerColor)
        {
            float radius = 45f;

            // Anillo exterior
            for (int i = 0; i < 35; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                Dust ringDust = Dust.NewDustPerfect(player.Center + offset, ringDustType, Vector2.Zero, 100, ringColor, 1.8f);
                ringDust.noGravity = true;
            }

            // Partículas internas flotantes
            for (int i = 0; i < 15; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(radius, radius);
                Dust innerDust = Dust.NewDustPerfect(player.Center + offset, innerDustType, new Vector2(0f, -4f), 100, innerColor, 1.4f);
                innerDust.noGravity = true;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<Flecha_Stand>(), 1)
                .AddIngredient(ItemID.HerculesBeetle, 1)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}