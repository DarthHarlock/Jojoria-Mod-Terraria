using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using System;
using Jojo.Content.Clases;
using Jojo.Content.UI;
using Jojo.Content.Items.Potenciadores;

namespace Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_4
{
    public class StrayCat_BOMBA_Tier_4 : ModProjectile
    {
        bool exploded;
        int timer;
        float spinRotation;

        const float speed = 4f;
        const int autoExplodeTime = 240;
        const float explosionRadius = 120f;
        const float baseDamage = 300f;
        const float spinSpeed = 0.01f;

        // =====================================================================
        //  AJUSTES DEL ARO DE PARTÍCULAS VERDES QUE RODEA AL PROYECTIL
        // =====================================================================
        // Cantidad de partículas que forman el aro. 0 = sin aro (ninguna partícula).
        // (Se removió el 'const' para evitar el error de código inaccesible)
        int ringParticleCount = 6;

        // Radio del aro alrededor del centro del proyectil. 0 = sin aro (radio nulo).
        float ringRadius = 16f;

        // Qué tan rápido gira el aro alrededor del proyectil (0 = no gira).
        float ringRotationSpeed = 0.05f;

        // Ángulo actual de rotación del aro (se va acumulando cada tick).
        float ringRotation;

        static readonly SoundStyle ExplodeSound = new("Terraria/Sounds/Item_14");

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.friendly = true;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override void AI()
        {
            if (exploded) return;

            timer++;

            if (Projectile.velocity == Vector2.Zero)
                Projectile.velocity = new Vector2(1f, 0f) * speed;
            else
                Projectile.velocity = Vector2.Normalize(Projectile.velocity) * speed;

            Projectile.rotation = Projectile.velocity.ToRotation();
            spinRotation += spinSpeed;

            if (timer >= autoExplodeTime)
                Explode();

            // Estela normal que ya tenías
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(2f, 2f),
                    163,
                    Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    120,
                    new Color(0, 255, 170),
                    Main.rand.NextFloat(0.9f, 1.3f)
                );

                d.noGravity = true;
                d.fadeIn = 0.6f;
            }

            SpawnParticleRing();
        }

        // NUEVO: aro de partículas verdes que rodea al proyectil y lo sigue
        // mientras vuela. Se puede desactivar poniendo ringParticleCount o
        // ringRadius en 0.
        void SpawnParticleRing()
        {
            if (ringParticleCount <= 0 || ringRadius <= 0f) return;

            ringRotation += ringRotationSpeed;

            for (int i = 0; i < ringParticleCount; i++)
            {
                float angle = ringRotation + (MathHelper.TwoPi / ringParticleCount) * i;
                Vector2 offset = angle.ToRotationVector2() * ringRadius;
                Vector2 pos = Projectile.Center + offset;

                Dust ring = Dust.NewDustPerfect(
                    pos,
                    163,
                    Projectile.velocity * 0.1f,
                    120,
                    new Color(0, 255, 170),
                    Main.rand.NextFloat(0.7f, 1f)
                );

                ring.noGravity = true;
                ring.fadeIn = 0.5f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!exploded) Explode();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!exploded) Explode();
            return false;
        }

        // NUEVO: permite detonar la burbuja manualmente desde afuera (por
        // ejemplo, al repulsar el mismo botón que la disparó), sin esperar a
        // que choque con algo o que se cumpla el tiempo automático.
        public void ForceDetonate()
        {
            if (!exploded) Explode();
        }

        void Explode()
        {
            if (exploded) return;
            exploded = true;

            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = false;

            if (Main.myPlayer == Projectile.owner)
            {
                Player owner = Main.player[Projectile.owner];

                float variation = Main.rand.NextFloat(0.9f, 1.15f);
                float finalBaseDamage = baseDamage * variation;

                int dmg = (int)owner
                    .GetTotalDamage(ModContent.GetInstance<ClaseStand>())
                    .ApplyTo(finalBaseDamage);

                int critChance = StandCritSystem.GetFinalCritChance(owner, 5);

                foreach (NPC npc in Main.npc)
                {
                    if (!npc.active || npc.life <= 0 || npc.friendly) continue;
                    if (Vector2.Distance(npc.Center, Projectile.Center) > explosionRadius) continue;
                    if (npc.immune[Projectile.owner] > 0) continue;

                    NPC.HitInfo hit = new()
                    {
                        Damage = dmg,
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

            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(ExplodeSound, Projectile.Center);

            // =========================================================================
            //  1. ARO VERDE PERFECTO (Velocidad constante y densidad aumentada)
            // =========================================================================
            int ringParticles = 200; // Aumentado a 200 para una línea sólida
            float ringSpeed = 10f;  // Velocidad fija para que todas las partículas formen el aro juntas

            for (int i = 0; i < ringParticles; i++)
            {
                float angle = i * (MathHelper.TwoPi / ringParticles);
                Vector2 velocity = angle.ToRotationVector2() * ringSpeed;

                // Usamos DustID.CursedTorch (61) para el fuego verde puro
                Dust greenFire = Dust.NewDustPerfect(Projectile.Center, 61, velocity, 0, default, 2.5f);
                greenFire.noGravity = true;
                greenFire.noLight = false;
            }

            // =========================================================================
            //  2. CORTINA DE HUMO Y EFECTOS SECUNDARIOS (Se mantienen igual)
            // =========================================================================
            for (int i = 0; i < 140; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(10f, 10f);
                Dust smoke = Dust.NewDustDirect(Projectile.Center, 40, 40, 31, velocity.X, velocity.Y, 120, Color.Gray, Main.rand.NextFloat(2.0f, 3.4f));
                smoke.noGravity = true;
                smoke.fadeIn = 1.2f;
            }

            for (int i = 0; i < 90; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 9f);
                Dust fire = Dust.NewDustDirect(Projectile.Center, 35, 35, 6, velocity.X, velocity.Y, 220, new Color(255, 105, 180), Main.rand.NextFloat(1.8f, 3.0f));
                fire.noGravity = true;
                fire.velocity = velocity * 1.6f;
                fire.fadeIn = 0.6f;
            }

            for (int i = 0; i < 40; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(8f, 14f);
                Dust burst = Dust.NewDustDirect(Projectile.Center, 60, 60, 31, velocity.X, velocity.Y, 100, Color.DarkGray, Main.rand.NextFloat(2.2f, 3.5f));
                burst.noGravity = true;
                burst.velocity *= 1.8f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            bool megumin = StandSlotSystem.HasMeguminSkinFor(owner);
            bool isRed = StandSlotSystem.HasKillerQueenRedSkinFor(owner);
            bool isBlue = StandSlotSystem.HasKillerQueenBlueSkinFor(owner);
            bool isGreen = StandSlotSystem.HasKillerQueenGreenSkinFor(owner);

            string path;
            if (megumin) path = "Jojo/Content/Projectiles/Skins/KillerQueen/Megumin/StrayCat_BOMBA_Megumin";
            else if (isRed) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Red/StrayCat_BOMBA_Red";
            else if (isBlue) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Blue/StrayCat_BOMBA_Blue";
            else if (isGreen) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Green/StrayCat_BOMBA_Green";
            else path = "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_4/StrayCat_BOMBA_Tier_4";

            Texture2D texture = ModContent.Request<Texture2D>(path).Value;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() / 2f;
            Color finalColor = Color.White * Projectile.Opacity;

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                null,
                finalColor,
                Projectile.rotation + spinRotation,
                origin,
                Projectile.scale,
                SpriteEffects.None
            );

            return false;
        }
    }
}