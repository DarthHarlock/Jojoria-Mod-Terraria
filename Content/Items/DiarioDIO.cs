using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Jojo.Content.UI;
using Jojo.Content.Projectiles.CMoon.CMoon_Tier_3;
using Jojo.Content.Projectiles.MadeInHeaven.MadeInHeaven_Tier_Final;

namespace Jojo.Content.Items
{
    public class DiarioDIO : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(gold: 5);
            Item.rare = ItemRarityID.Yellow;

            // Consumible (estilo comer)
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item29; // Sonido místico
            Item.consumable = true;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            // Mantiene el ítem cerca de la cara al consumirlo
            player.itemLocation.Y -= 18f;
            player.itemLocation.X -= player.direction * 4f;
        }

        // 🔥 Condición: Solo deja consumirlo si estás en el ESPACIO y tienes a C-Moon Tier 3 equipado
        public override bool CanUseItem(Player player)
        {
            // 1. Debe estar en el espacio (ZoneSkyHeight)
            if (!player.ZoneSkyHeight)
            {
                return false;
            }

            // 2. Debe tener equipado C-Moon Tier 3 en la ranura de Stand
            var sp = player.GetModPlayer<StandSlotPlayer>();
            if (sp.standItem != null && !sp.standItem.IsAir && sp.standItem.type == ModContent.ItemType<CMoonItem_Tier_3>())
            {
                return true;
            }

            return false;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
                return true;

            var sp = player.GetModPlayer<StandSlotPlayer>();

            // 🔥 1. Eliminar a C-Moon Tier 3 al instante
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == player.whoAmI && proj.type == ModContent.ProjectileType<CMOONSTAND_Tier_3>())
                {
                    proj.Kill();
                }
            }

            // 🔥 2. Equipar Made in Heaven en la ranura de Stand
            if (ModContent.TryFind<ModItem>("Jojo", "MadeInHeavenItem", out ModItem mihModItem))
            {
                Item newStand = new Item();
                newStand.SetDefaults(mihModItem.Type);
                sp.standItem = newStand;
            }

            // 🔥 3. Invocar a Made in Heaven
            Projectile.NewProjectile(
                player.GetSource_ItemUse(Item),
                player.Center,
                Vector2.Zero,
                ModContent.ProjectileType<MADEINHEAVENSTAND>(),
                0,
                0f,
                player.whoAmI
            );

            // 🔥 4. Generar el efecto de partículas (sincronizado para todos los jugadores)
            Projectile.NewProjectile(
                player.GetSource_ItemUse(Item),
                player.Center,
                Vector2.Zero,
                ModContent.ProjectileType<DiarioExplosionProjectile>(),
                0,
                0f,
                player.whoAmI
            );

            return true;
        }
    }

    // 🌟 Proyectil invisible que reproduce las partículas en la pantalla de TODOS los jugadores
    public class DiarioExplosionProjectile : ModProjectile
    {
        // Reutiliza la textura del diario para evitar errores de PNG faltante. 
        // NOTA: Si tu mod internamente se llama "Jojos" en lugar de "Jojo", cambia "Jojo/..." por "Jojos/..."
        public override string Texture => "Jojo/Content/Items/DiarioDIO";

        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2; // Desaparece inmediatamente
            Projectile.alpha = 255;  // Completamente invisible
        }

        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            if (player == null || !player.active) return;

            // Sonido místico
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, player.Center);

            // 1. Onda masiva BLANCA
            for (int i = 0; i < 150; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(35f, 35f);
                Dust ringDust = Dust.NewDustPerfect(player.Center, DustID.WhiteTorch, velocity, 0, Color.White, 2f);
                ringDust.noGravity = true;
            }

            // 2. Onda masiva CELESTE
            for (int i = 0; i < 120; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(25f, 25f);
                Dust celesteDust = Dust.NewDustPerfect(player.Center, DustID.IceTorch, velocity, 0, Color.Cyan, 1.8f);
                celesteDust.noGravity = true;
            }

            // 3. Destellos
            for (int i = 0; i < 80; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(100f, 100f);
                Vector2 velocity = offset * 0.05f;

                Dust innerWhite = Dust.NewDustPerfect(player.Center + offset, DustID.GemDiamond, velocity, 0, Color.White, 1.5f);
                innerWhite.noGravity = true;

                Dust innerCyan = Dust.NewDustPerfect(player.Center + offset, DustID.MagicMirror, velocity * 1.2f, 0, Color.White, 1.5f);
                innerCyan.noGravity = true;
            }

            // 4. Pilar al cielo
            for (int i = 0; i < 60; i++)
            {
                Vector2 offset = new Vector2(Main.rand.NextFloat(-150f, 150f), Main.rand.NextFloat(-60f, 60f));
                Dust upDust = Dust.NewDustPerfect(
                    player.Center + offset,
                    DustID.WhiteTorch,
                    new Vector2(0f, Main.rand.NextFloat(-15f, -6f)),
                    0,
                    Color.White,
                    1.8f
                );
                upDust.noGravity = true;
            }
        }
    }
}