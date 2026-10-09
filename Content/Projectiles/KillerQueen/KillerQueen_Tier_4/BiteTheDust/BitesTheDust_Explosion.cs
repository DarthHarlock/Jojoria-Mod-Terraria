using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Jojo.Content.Clases;
using Jojo.Content.UI;
using Jojo.Content.Systems;

namespace Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_4.BitesTheDust
{
    public class BitesTheDust_Explosion : ModProjectile
    {
        public override string Texture => "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_4/BOMBA1_Tier_4";

        public override void SetDefaults()
        {
            // Hitbox ampliado para que coincida con la explosión masiva
            Projectile.width = 350;
            Projectile.height = 350;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3;
            Projectile.hide = true;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            if (Projectile.timeLeft == 2)
            {
                Explode();
            }
        }

        void Explode()
        {
            if (Main.myPlayer == Projectile.owner)
            {
                Player owner = Main.player[Projectile.owner];
                float radius = 350f;

                // 1. Aquí pones el daño fijo que quieres (ej. 200)
                float baseDamage = 200f;

                // 2. Le aplicamos los bonificadores de tu ClaseStand (armaduras, accesorios, etc.)
                float scaledDamage = owner.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);

                // 3. Calculamos la probabilidad de crítico base
                int critChance = StandCritSystem.GetFinalCritChance(owner, 5);

                foreach (NPC npc in Main.npc)
                {
                    if (!npc.active || npc.life <= 0 || npc.friendly) continue;

                    // Validamos que el enemigo esté dentro de la explosión
                    if (Vector2.Distance(npc.Center, Projectile.Center) > radius) continue;
                    if (npc.immune[Projectile.owner] > 0) continue;

                    // 4. Usamos la función nativa de Terraria para darle la variación normal (+/- 15% + Suerte del jugador)
                    // Esto generará números como 190, 220, 203, etc., basándose en tu daño escalado.
                    int finalDamage = Main.DamageVar(scaledDamage, owner.luck);

                    NPC.HitInfo hit = new()
                    {
                        Damage = finalDamage,
                        Knockback = 0f,
                        HitDirection = npc.Center.X > Projectile.Center.X ? 1 : -1,
                        Crit = Main.rand.Next(100) < critChance
                    };

                    npc.StrikeNPC(hit);
                    npc.immune[Projectile.owner] = Projectile.localNPCHitCooldown;

                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        NetMessage.SendStrikeNPC(npc, hit);
                    }
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            // Sonido modificado para ser mucho más grave e imponente
            SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.4f, Volume = 1.2f }, Projectile.Center);

            // 1. Humo denso masivo
            for (int i = 0; i < 300; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(20.0f, 20.0f);
                Dust smoke = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Smoke,
                    velocity.X, velocity.Y, 130, Color.DarkGray, Main.rand.NextFloat(3.5f, 5.0f));
                smoke.noGravity = true;
                smoke.fadeIn = 1.5f;
            }

            // 2. Fuego esparcido caótico (Conservando tu color rosa)
            for (int i = 0; i < 200; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(25.0f, 25.0f);
                Dust fire = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Torch,
                    velocity.X, velocity.Y, 100, new Color(255, 105, 180), Main.rand.NextFloat(2.5f, 4.0f));
                fire.noGravity = true;
                fire.velocity = velocity * 1.5f;
            }

            // 3. ONDA EXPANSIVA PRINCIPAL (Anillo de fuego rápido y gigante)
            int fireRingParticles = 200;
            float ringSpeed = 22.0f;
            for (int i = 0; i < fireRingParticles; i++)
            {
                float angle = i * (MathHelper.TwoPi / fireRingParticles);
                Vector2 velocity = angle.ToRotationVector2() * ringSpeed;

                Dust ringDust = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Torch,
                    velocity.X, velocity.Y, 100, new Color(255, 105, 180), Main.rand.NextFloat(2.5f, 3.5f));
                ringDust.noGravity = true;
                ringDust.velocity = velocity;
            }

            // 4. ONDA EXPANSIVA SECUNDARIA (Anillo interior más lento para dar volumen)
            int fireRingParticles2 = 150;
            float ringSpeed2 = 14.0f;
            for (int i = 0; i < fireRingParticles2; i++)
            {
                float angle = i * (MathHelper.TwoPi / fireRingParticles2);
                Vector2 velocity = angle.ToRotationVector2() * ringSpeed2;

                Dust ringDust2 = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Torch,
                    velocity.X, velocity.Y, 100, new Color(255, 105, 180), Main.rand.NextFloat(2.0f, 3.0f));
                ringDust2.noGravity = true;
                ringDust2.velocity = velocity;
            }

            // 5. Metralla y Gores volando
            for (int g = 0; g < 8; g++)
            {
                Terraria.Gore.NewGore(
                    Projectile.GetSource_Death(),
                    Projectile.Center,
                    new Vector2(Main.rand.NextFloat(-8.0f, 8.0f), Main.rand.NextFloat(-8.0f, 8.0f)),
                    Main.rand.Next(61, 64),
                    1.5f
                );
            }
        }
    }
}