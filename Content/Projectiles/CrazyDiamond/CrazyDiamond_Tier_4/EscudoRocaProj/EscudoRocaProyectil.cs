using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using System;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.CrazyDiamond.EscudoRoca
{
    public class EscudoRocaProyectil : ModProjectile
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.StoneBlock;

        // =====================================================================
        //  ai[0] = indice de la roca (0, 1, 2...)
        //  ai[1] = cantidad TOTAL de rocas del escudo   (si es 0 usa TotalRocas)
        //  ai[2] = radio del escudo en pixeles           (si es 0 usa RadioEscudo)
        //  Asi cada Tier configura su escudo desde el propio stand.
        //  La Tier 4 sigue funcionando igual (solo manda ai[0]).
        // =====================================================================

        // Valores por defecto (se usan si el stand no manda ai[1] / ai[2])
        protected virtual int TotalRocas => 3;
        protected virtual float RadioEscudo => 64f;
        protected virtual int DustTipo => DustID.YellowTorch;
        protected virtual float VelocidadAcercamiento => 0.15f;
        protected virtual float MaxVelocidadVuelo => 22f;

        int GetTotalRocas() => Projectile.ai[1] > 0f ? (int)Projectile.ai[1] : TotalRocas;
        float GetRadio() => Projectile.ai[2] > 0f ? Projectile.ai[2] : RadioEscudo;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600; // 10 segundos de proteccion
            Projectile.netImportant = true;
            Projectile.alpha = 0;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            if (!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            float angle = Projectile.ai[0] * (MathHelper.TwoPi / GetTotalRocas());
            // Ligera prediccion con la velocidad del jugador para que no se queden atras si te mueves
            Vector2 targetPosition = player.Center + player.velocity * 0.5f + angle.ToRotationVector2() * GetRadio();

            if (Projectile.localAI[0] == 0f)
            {
                HoverMovement(player, targetPosition, angle);
            }
            else if (Projectile.localAI[0] == 1f)
            {
                OrbitBehavior(player, targetPosition);
            }
        }

        protected virtual void HoverMovement(Player player, Vector2 targetPosition, float angle)
        {
            Projectile.rotation += 0.4f;

            // Temporizador de seguridad de vuelo
            Projectile.localAI[1]++;

            // Particulas simples en vuelo
            for (int i = 0; i < 1; i++)
            {
                Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(8f, 8f);
                Dust d = Dust.NewDustPerfect(dustPos, DustTipo, Main.rand.NextVector2Circular(1.5f, 1.5f), 100, default, 1.1f);
                d.noGravity = true;
            }

            Vector2 dir = targetPosition - Projectile.Center;
            float dist = dir.Length();

            // FAILSAFE: a menos de 25 px O tras 35 frames se acoplan al instante
            if (dist > 25f && Projectile.localAI[1] < 35f)
            {
                dir.Normalize();
                float speed = MathHelper.Clamp(dist * VelocidadAcercamiento, 6f, MaxVelocidadVuelo);
                Projectile.velocity = (Projectile.velocity * 10f + dir * speed) / 11f;
            }
            else
            {
                // Llegada y acoplamiento al Escudo
                Projectile.localAI[0] = 1f;
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = angle + MathHelper.PiOver2;

                Projectile.friendly = true;
                Projectile.netUpdate = true;

                SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            }
        }

        protected virtual void OrbitBehavior(Player player, Vector2 targetPosition)
        {
            // Posicion exacta en orbita siguiendo al jugador
            float angle = Projectile.ai[0] * (MathHelper.TwoPi / GetTotalRocas());
            Projectile.Center = player.Center + angle.ToRotationVector2() * GetRadio();
            Projectile.velocity = Vector2.Zero;

            // Particulas sutiles
            if (Main.rand.NextBool(45))
            {
                Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(15f, 15f);
                Vector2 driftVel = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-1.5f, -0.2f));

                Dust d = Dust.NewDustPerfect(dustPos, DustTipo, driftVel, 0, default, 2.0f);
                d.noGravity = true;
                d.scale = 2.0f;
            }

            CheckEnemyProjectiles();
        }

        protected virtual void CheckEnemyProjectiles()
        {
            // Absorcion de proyectiles enemigos
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.hostile && proj.damage > 0)
                {
                    if (Projectile.Hitbox.Intersects(proj.Hitbox))
                    {
                        proj.Kill();
                        Projectile.Kill(); // Se destruye solo la roca impactada
                        break;
                    }
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Tink, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);

            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Stone,
                    Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f), Scale: 1.3f);

                Dust d = Dust.NewDustPerfect(Projectile.Center, DustTipo, Main.rand.NextVector2Circular(3f, 3f), Scale: 1.4f);
                d.noGravity = true;
            }
        }
    }
}