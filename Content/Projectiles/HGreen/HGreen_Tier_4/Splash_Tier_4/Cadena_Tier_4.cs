using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Clases;
using Jojo.Content.Players;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_4;
using Jojo.Systems;

namespace Jojo.Content.Projectiles.HGreen.HGreen_Tier_4.Splash_Tier_4
{
    public class Cadena_Tier_4 : ModProjectile
    {
        // --- CONFIGURACIÓN DE RUPTURA (al desequipar/despawnear el Stand) ---
        public const int RupturaCantidadParticulas = 3; // Muy pocas: hay MUCHOS segmentos a la vez

        static readonly Color RupturaColor = new Color(60, 255, 90);

        static int standTypeCache = -1;
        static int StandType => standTypeCache != -1 ? standTypeCache : (standTypeCache = ModContent.ProjectileType<HGREENSTAND_Tier_4>());

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1200;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
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
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < RupturaCantidadParticulas; i++)
                {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f);
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GreenFairy, dir * Main.rand.NextFloat(0.5f, 1.5f), 100, RupturaColor, 0.9f);
                    dust.noGravity = true;
                }
            }

            Projectile.Kill();
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];

            // --- RUPTURA: si el Stand se desequipó, murió, o se cambió a otro Stand, ---
            // este eslabón de la cadena se rompe aquí mismo, con sus propias partículas.
            if (StandRoto(p))
            {
                RomperYDesvanecer();
                return;
            }

            Projectile.velocity = Vector2.Zero;

            // Garantizar escalado de impacto crítico idéntico al Stand
            if (p.active)
            {
                Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, 5);
            }

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.rotation = Projectile.ai[0];
                Projectile.localAI[0] = 1f;
            }

            if (Projectile.timeLeft < 60)
            {
                Projectile.alpha += 4;
            }
        }
    }
}