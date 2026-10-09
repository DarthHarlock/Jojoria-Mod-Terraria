using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Jojo.Content.Clases;
using Jojo.Content.Systems;

namespace Jojo.Content.Projectiles.HGreen.HGreen_Tier_4.ControlMarioneta_Tier_4
{
    public class Esmeralda_3_Tier_4 : ModProjectile
    {
        static readonly Color NeonGreen = new Color(60, 255, 90);
        const int ChainCount = 4;

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

            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.alpha = 0;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= 4) Projectile.frame = 0;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.15f, 0.8f, 0.27f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.GreenFairy, 0f, 0f, 100, default, 0.65f);
                dust.noGravity = true;
                dust.color = NeonGreen;
                dust.velocity = Projectile.velocity * -0.05f;
                dust.fadeIn = 0.4f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];

            if (Projectile.owner == Main.myPlayer)
            {
                if (MarionetaGlobalNPC.IsTetherable(target))
                {
                    var marioneta = target.GetGlobalNPC<MarionetaGlobalNPC>();

                    int standType = ModContent.ProjectileType<HGREENSTAND_Tier_4>();
                    float maxDist = Control_Cadena_Tier_4.MaxChainDistance;
                    // [CORRECCIÓN]: La duración ahora la define la propia cadena de este tier
                    int duration = Control_Cadena_Tier_4.TetherDuration;

                    marioneta.Attach(target, player, standType, maxDist, duration);

                    for (int i = 0; i < ChainCount; i++)
                    {
                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            player.Center,
                            Vector2.Zero,
                            ModContent.ProjectileType<Control_Cadena_Tier_4>(),
                            0,
                            0f,
                            player.whoAmI,
                            target.whoAmI,
                            i
                        );
                    }
                }
            }

            SpawnImpactDust();
            Projectile.Kill();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnImpactDust();
            Projectile.Kill();
            return false;
        }

        void SpawnImpactDust()
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