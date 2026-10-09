using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Jojo.Content.Clases;
using Jojo.Content.Players;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_4;

namespace Jojo.Content.Projectiles.HGreen.HGreen_Tier_4.Splash_Tier_4
{
    public class Esmeralda_2_Tier_4 : ModProjectile
    {
        static readonly Color NeonGreen = new Color(60, 255, 90);

        // --- CONFIGURACIÓN DE RUPTURA (al desequipar/despawnear el Stand) ---
        public const int RupturaCantidadParticulas = 6; // Pocas partículas, es solo un "eslabón" de la habilidad

        static int standTypeCache = -1;
        static int StandType => standTypeCache != -1 ? standTypeCache : (standTypeCache = ModContent.ProjectileType<HGREENSTAND_Tier_4>());

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;

            // FIX: la esmeralda es solo la "portadora" que va dejando cadenas detrás.
            // No debe hacer daño por sí misma al tocar enemigos mientras rebota; el único
            // daño real debe venir de las Cadenas. NO se pone damage = 0 porque este valor
            // se hereda al spawnear cada Cadena_Tier_4 más abajo.
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.penetrate = -1;
            Projectile.timeLeft = 3600;
            Projectile.alpha = 0;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        // Comprueba si el Stand dueño de esta habilidad sigue equipado/vivo.
        bool StandRoto(Player p)
        {
            if (!p.active || p.dead) return true;
            if (!p.TryGetModPlayer(out StandStatsPlayer stats)) return true;
            if (stats.activeStand == null) return true;
            if (!stats.activeStand.active) return true;
            if (stats.activeStand.type != StandType) return true;
            return false;
        }

        void RomperYDesvanecer()
        {
            SpawnBreakDust();
            Projectile.Kill();
        }

        void SpawnBreakDust()
        {
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < RupturaCantidadParticulas; i++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GreenFairy, dir * Main.rand.NextFloat(1f, 2.5f), 100, default, 1.1f);
                dust.noGravity = true;
                dust.color = NeonGreen;
            }
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];

            // --- RUPTURA: si el Stand se desequipó, murió, o se cambió a otro Stand, ---
            // esta esmeralda (y la cadena que va dejando) se rompe aquí mismo.
            if (StandRoto(p))
            {
                RomperYDesvanecer();
                return;
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 4;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.15f, 0.8f, 0.27f);

            // Generar rastro de cadenas estáticas de forma más continua para formar el hilo
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] > 1) // Aparece una cadena cada 2 ticks para que se solapen
            {
                Projectile.localAI[0] = 0;
                if (Main.myPlayer == Projectile.owner)
                {
                    // Pasamos Projectile.rotation como el parámetro ai0 para que la cadena sepa a dónde mirar
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Cadena_Tier_4>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.rotation);
                }
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.GreenFairy, 0f, 0f, 100, default, 0.65f);
                dust.noGravity = true;
                dust.color = NeonGreen;
                dust.velocity = Projectile.velocity * -0.05f;
                dust.fadeIn = 0.4f;
            }

            int auraID = (int)Projectile.ai[0];

            if (auraID >= 0 && auraID < Main.maxProjectiles)
            {
                Projectile aura = Main.projectile[auraID];

                if (aura.active && aura.type == ModContent.ProjectileType<HGreen_Aura_Tier_4>())
                {
                    float distanciaAlCentro = Vector2.Distance(Projectile.Center + Projectile.velocity, aura.Center);
                    float radioReal = HGreen_Aura_Tier_4.AuraRadius;

                    if (distanciaAlCentro >= radioReal - (Projectile.width / 2f))
                    {
                        Projectile.ai[1]++;

                        // MUERE DESPUÉS DE 4 REBOTES
                        if (Projectile.ai[1] > 2) //Nuemro de rebotes 2
                        {
                            SpawnImpactDust();
                            Projectile.Kill();
                        }
                        else
                        {
                            // REBOTE ALEATORIO
                            float currentSpeed = Projectile.velocity.Length();
                            Vector2 normalDeSuperficie = Vector2.Normalize(aura.Center - Projectile.Center);
                            Vector2 baseReflection = Vector2.Reflect(Projectile.velocity, normalDeSuperficie);

                            float randomRotation = MathHelper.ToRadians(Main.rand.NextFloat(-35f, 35f));
                            Projectile.velocity = baseReflection.RotatedBy(randomRotation).SafeNormalize(Vector2.Zero) * currentSpeed;

                            Vector2 direccionHaciaAfuera = Vector2.Normalize(Projectile.Center - aura.Center);
                            Projectile.Center = aura.Center + (direccionHaciaAfuera * (radioReal - Projectile.width));

                            SpawnImpactDust();
                        }
                    }
                }
                else
                {
                    // El aura ya no existe (rota o expiró) -> esta esmeralda también se rompe
                    RomperYDesvanecer();
                }
            }
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
                texture, Projectile.Center - Main.screenPosition, sourceRectangle,
                glowColor * ((255 - Projectile.alpha) / 255f), Projectile.rotation, origin, Projectile.scale, effects, 0
            );
            return false;
        }
    }
}