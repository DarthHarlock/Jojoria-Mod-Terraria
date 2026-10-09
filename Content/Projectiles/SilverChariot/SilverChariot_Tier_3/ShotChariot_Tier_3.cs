using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Jojo.Content.Clases;
using Jojo.Content.UI;

namespace Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_3
{
    public class ShotChariot_Tier_3 : ModProjectile
    {
        // ai[0] = número de rebotes usados
        // ai[1] = tiempo de vida en vuelo (aceleración)
        // ai[2] = 1 si está clavada en el suelo

        const float MaxSpeed = 22f;
        const float Accel = 0.35f;
        const int MaxBounces = 8; // <-- Cambiado a 5 rebotes
        const int StuckTicks = 180;

        // === DAÑO PROPIO DEL PROYECTIL ===
        const float BaseDamage = 100f;   // <-- cambia este valor para ajustar el daño
        // =================================

        // === RUTA BASE DE TEXTURAS SEGÚN SKIN ===
        const string PathNormal = "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Tier_3/";
        const string PathGolden = "Jojo/Content/Projectiles/Skins/SilverChariot/Golden/";

        bool Stuck => Projectile.ai[2] == 1f;

        float ScaleX => 1f;
        float ScaleY => 0.7f;

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 10;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.timeLeft = 600;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Player owner = Main.player[Projectile.owner];
            Projectile.damage = (int)owner
                .GetTotalDamage(ModContent.GetInstance<ClaseStand>())
                .ApplyTo(BaseDamage);
        }

        public override void AI()
        {
            if (Stuck)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.friendly = false;

                if (Projectile.timeLeft <= 60)
                    Projectile.alpha = (int)(255 * (1f - Projectile.timeLeft / 60f));

                return;
            }

            Projectile.ai[1]++;

            float currentSpeed = Projectile.velocity.Length();
            if (currentSpeed < MaxSpeed)
            {
                float newSpeed = Math.Min(currentSpeed + Accel, MaxSpeed);
                Projectile.velocity = Vector2.Normalize(Projectile.velocity) * newSpeed;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity * 0.5f + Main.rand.NextVector2Circular(3f, 3f),
                    DustID.WhiteTorch,
                    -Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.3f, 0.3f),
                    180,
                    Color.White,
                    Main.rand.NextFloat(0.6f, 1.1f)
                );
                d.noGravity = true;
                d.fadeIn = 0.4f;
            }
        }

        // =========================================================================
        // NUEVO: CUANDO GOLPEA A UN ENEMIGO, REBOTA HACIA EL SIGUIENTE
        // =========================================================================
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int bounces = (int)Projectile.ai[0];

            if (bounces < MaxBounces)
            {
                // Busca un enemigo en 800 píxeles, ignorando al que acabamos de atravesar
                NPC nextTarget = FindClosestNPC(800f, target.whoAmI);

                if (nextTarget != null)
                {
                    // Redirigir hacia el nuevo enemigo
                    Vector2 direction = (nextTarget.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                    Projectile.velocity = direction * MaxSpeed;

                    Projectile.ai[0]++; // Gastamos un rebote
                    Projectile.netUpdate = true;

                    // Efecto de sonido (opcional, para dar feedback de que encadenó el golpe)
                    SoundEngine.PlaySound(SoundID.Item154, Projectile.Center);
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            int bounces = (int)Projectile.ai[0];

            if (bounces >= MaxBounces)
            {
                Projectile.ai[2] = 1f;
                Projectile.velocity = Vector2.Zero;
                Projectile.tileCollide = false;
                Projectile.timeLeft = StuckTicks;
                Projectile.netUpdate = true;

                SoundEngine.PlaySound(SoundID.Tink, Projectile.Center);
                return false;
            }

            // Rebote normal en la pared
            if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > 0.1f)
                Projectile.velocity.X = -oldVelocity.X;
            if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > 0.1f)
                Projectile.velocity.Y = -oldVelocity.Y;

            // NUEVO: SI REBOTA EN LA PARED, TAMBIÉN INTENTA BUSCAR A UN ENEMIGO
            NPC nextTarget = FindClosestNPC(800f, -1);
            if (nextTarget != null)
            {
                Vector2 direction = (nextTarget.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                Projectile.velocity = direction * MaxSpeed;
            }

            Projectile.ai[0]++;
            SoundEngine.PlaySound(SoundID.Tink, Projectile.Center);
            Projectile.netUpdate = true;
            return false;
        }

        // =========================================================================
        // MÉTODO DE BÚSQUEDA DE ENEMIGOS
        // =========================================================================
        private NPC FindClosestNPC(float maxDetectDistance, int ignoreNPC)
        {
            NPC closestNPC = null;
            float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC target = Main.npc[i];

                // Que el NPC esté activo, sea hostil y NO sea el mismo que acabamos de golpear
                if (target.CanBeChasedBy() && target.whoAmI != ignoreNPC)
                {
                    float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);
                    if (sqrDistanceToTarget < sqrMaxDetectDistance)
                    {
                        // Comprueba que no haya paredes de por medio (para que no intente atravesar el suelo inútilmente)
                        if (Collision.CanHit(Projectile.Center, 1, 1, target.Center, 1, 1))
                        {
                            sqrMaxDetectDistance = sqrDistanceToTarget;
                            closestNPC = target;
                        }
                    }
                }
            }
            return closestNPC;
        }

        public override void OnKill(int timeLeft) { }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            bool goldenSkin = StandSlotSystem.HasGoldenSkinFor(owner);
            string pathBase = goldenSkin ? PathGolden : PathNormal;

            Texture2D texture = ModContent.Request<Texture2D>(goldenSkin ? pathBase + "Shot_Golden" : PathNormal + "Shot_Tier_3").Value;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() / 2f;
            Color finalColor = lightColor * ((255 - Projectile.alpha) / 255f);

            Vector2 scale = new Vector2(ScaleX, ScaleY);

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                null,
                finalColor,
                Projectile.rotation,
                origin,
                scale,
                SpriteEffects.None
            );

            return false;
        }
    }
}