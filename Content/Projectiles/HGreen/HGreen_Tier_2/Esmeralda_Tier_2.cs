using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.HGreen.HGreen_Tier_2
{
    public class Esmeralda_Tier_2 : ModProjectile
    {
        // Color neón esmeralda (más "chillón" que el verde default de Terraria)
        static readonly Color NeonGreen = new Color(60, 255, 90);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            // Bala tipo metralleta: golpea y muere
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.alpha = 0;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 0;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;

            // --- PENETRACIÓN DE ARMADURA AQUÍ ---
            Projectile.ArmorPenetration = 1004;
        }

        public override void AI()
        {
            // --- ANIMACIÓN DEL SPRITESHEET ---
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= 4)
                {
                    Projectile.frame = 0;
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            // Luz esmeralda reducida
            Lighting.AddLight(Projectile.Center, 0.15f, 0.8f, 0.27f);

            // --- RASTRO DE PARTÍCULAS ---
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.GreenFairy,
                    0f, 0f,
                    100,
                    default,
                    0.65f
                );

                dust.noGravity = true;
                dust.color = NeonGreen;
                dust.velocity = Projectile.velocity * -0.05f;
                dust.fadeIn = 0.4f;
            }
        }

        // Se cambia 'Kill' por 'OnKill' para corregir la advertencia de método obsoleto
        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;

            for (int i = 0; i < 10; i++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GreenFairy, dir * Main.rand.NextFloat(1.5f, 3.5f), 100, default, 1.4f);
                dust.noGravity = true;
                dust.color = NeonGreen;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;

            int frameWidth = texture.Width / 4;
            int frameHeight = texture.Height;

            Rectangle sourceRectangle = new Rectangle(Projectile.frame * frameWidth, 0, frameWidth, frameHeight);
            Vector2 origin = new Vector2(frameWidth * 0.5f, frameHeight * 0.5f);

            SpriteEffects effects = Projectile.velocity.X < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;

            Color glowColor = Color.White * 0.75f;

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                sourceRectangle,
                glowColor * ((255 - Projectile.alpha) / 255f),
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0
            );

            return false;
        }
    }
}