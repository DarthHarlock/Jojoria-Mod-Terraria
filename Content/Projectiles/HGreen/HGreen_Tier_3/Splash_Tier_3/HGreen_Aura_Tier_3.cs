using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Clases;
using Jojo.Content.Players;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_3;

namespace Jojo.Content.Projectiles.HGreen.HGreen_Tier_3.Splash_Tier_3
{
    public class HGreen_Aura_Tier_3 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        // ==============================================================
        // --- CONFIGURACIÓN DE LA HABILIDAD H (Independiente del Stand) ---
        // ==============================================================
        public const float AuraRadius = 300f;         // Radio visual del área

        public const float AuraDamageBase = 20f;       // Daño BASE de esta habilidad, totalmente independiente del 45f del Stand
        public const int AuraShotCount = 4;             // Cuántos proyectiles (Esmeralda_2_Tier_4) dispara el círculo
        public const float AuraShotSpeed = 11f;         // Velocidad de salida de cada proyectil

        // --- CONFIGURACIÓN DE RUPTURA (al desequipar/despawnear el Stand) ---
        public const int RupturaCantidadParticulas = 10; // Partículas al romperse el área entera

        static readonly Color RupturaColor = new Color(60, 255, 90);

        static int standTypeCache = -1;
        static int StandType => standTypeCache != -1 ? standTypeCache : (standTypeCache = ModContent.ProjectileType<HGREENSTAND_Tier_3>());
        // ==============================================================

        public override void SetDefaults()
        {
            Projectile.width = (int)(AuraRadius * 2);
            Projectile.height = (int)(AuraRadius * 2);

            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1200;
            Projectile.hide = true;
        }

        public override bool? CanCutTiles() => false;

        // Comprueba si el Stand dueño de esta habilidad sigue equipado/vivo.
        // Se ejecuta igual en todos los clientes porque StandStatsPlayer.activeStand
        // se actualiza localmente en cada uno (ver HGREENSTAND_Tier_4.AI()).
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
                    Vector2 offset = Main.rand.NextVector2Circular(AuraRadius, AuraRadius);
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.GreenFairy, Vector2.Zero, 100, RupturaColor, 1.2f);
                    dust.noGravity = true;
                    dust.fadeIn = 0.3f;
                }
            }

            Projectile.Kill();
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];

            // --- RUPTURA: si el Stand se desequipó, murió, o se cambió a otro Stand, ---
            // toda el área se rompe y desaparece aquí mismo, en vez de seguir viva.
            if (StandRoto(p))
            {
                RomperYDesvanecer();
                return;
            }

            // --- DAÑO INDEPENDIENTE: se calcula aquí mismo, ignorando lo que el Stand ---
            // haya pasado al crear este proyectil. Así, cambiar AuraDamageBase arriba
            // afecta SOLO a esta habilidad, sin tocar HGREENSTAND_Tier_4.cs.
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(AuraDamageBase);

            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(AuraRadius, AuraRadius);
                Dust ringDust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.GreenFairy, Vector2.Zero, 100, Color.LimeGreen, 1.5f);
                ringDust.noGravity = true;
            }

            if (Main.rand.NextBool(3))
            {
                Vector2 offset = Main.rand.NextVector2Circular(AuraRadius, AuraRadius);
                Dust innerDust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.CursedTorch, new Vector2(0f, -1.5f), 100, Color.Lime, 1.2f);
                innerDust.noGravity = true;
            }

            if (Projectile.localAI[0] == 0f)
            {
                if (Main.myPlayer == Projectile.owner)
                {
                    int shots = System.Math.Max(1, AuraShotCount);
                    for (int i = 0; i < shots; i++)
                    {
                        float angle = MathHelper.TwoPi / shots * i;
                        Vector2 velocity = angle.ToRotationVector2() * AuraShotSpeed;

                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<Esmeralda_2_Tier_3>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.whoAmI, 0f);
                    }
                }
                Projectile.localAI[0] = 1f;
            }
        }
    }
}