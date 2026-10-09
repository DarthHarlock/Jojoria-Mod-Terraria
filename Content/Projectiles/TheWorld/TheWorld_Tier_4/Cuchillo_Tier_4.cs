using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Jojo.Content.Buffs;

namespace Jojo.Content.Projectiles.TheWorld.TheWorld_Tier_4
{
    // ==========================================
    // EL PROYECTIL DEL STAND (CONFIGURABLE)
    // ==========================================
    public class Cuchillo_Tier_4 : ModProjectile
    {
        // =========================================================================
        // ⚙️ CONFIGURA AQUÍ TODO LO DE ESTE TIER (copia este archivo a Cuchillo_Tier_X
        // y solo cambia estos valores + el nombre de la clase para reutilizar):
        // =========================================================================
        public static readonly int CuchillosEnAbanico = 6;   // Cuchillos por ráfaga
        public static readonly int NumeroDeRafagas = 3;      // Cuántas ráfagas en total
        public static readonly int Daño = 100;                // Daño (clase Stand)
        public static readonly int DelayEntreRafagas = 20;   // Frames entre ráfagas
        public static readonly float AnguloSeparacion = 5f;  // Grados entre cuchillos del abanico
        public static readonly float Velocidad = 15f;        // Velocidad del proyectil
        public static readonly float Knockback = 2f;         // Knockback
        // =========================================================================

        private const float gravInicial = 0.15f;
        private const float velMaxCaida = 12f;

        private bool stuck = false;
        private bool isFalling = false;
        private NPC stuckNPC;
        private Vector2 offsetFromNPC;

        private float finalRotation = 0f;
        private bool rotationFrozen = false;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxModifier)
        {
            if (!stuck && !rotationFrozen)
            {
                if (Projectile.velocity != Vector2.Zero)
                {
                    float angle = Projectile.velocity.ToRotation();
                    if (Projectile.spriteDirection == -1)
                    {
                        finalRotation = angle + MathHelper.Pi - MathHelper.PiOver4;
                    }
                    else
                    {
                        finalRotation = angle + MathHelper.PiOver4;
                    }
                }
            }
            return true;
        }

        public override void AI()
        {
            if (rotationFrozen && !isFalling && stuckNPC == null)
            {
                Projectile.rotation = finalRotation;
                Projectile.velocity = Vector2.Zero;
                return;
            }

            if (!stuck && !isFalling)
            {
                Projectile.velocity.Y += gravInicial;
                if (Projectile.velocity.Y > velMaxCaida) Projectile.velocity.Y = velMaxCaida;

                float angle = Projectile.velocity.ToRotation();

                if (Projectile.velocity.X < 0)
                {
                    Projectile.spriteDirection = -1;
                    Projectile.rotation = angle + MathHelper.Pi - MathHelper.PiOver4;
                }
                else
                {
                    Projectile.spriteDirection = 1;
                    Projectile.rotation = angle + MathHelper.PiOver4;
                }
            }
            else if (stuck)
            {
                if (stuckNPC != null && stuckNPC.active && stuckNPC.life > 0)
                {
                    Projectile.velocity = Vector2.Zero;
                    Projectile.friendly = false;
                    Projectile.tileCollide = false;
                    Projectile.Center = stuckNPC.Center + offsetFromNPC;
                    Projectile.rotation = finalRotation;
                }
                else
                {
                    stuck = false;
                    isFalling = true;
                    rotationFrozen = false;
                    stuckNPC = null;
                    Projectile.tileCollide = true;
                    Projectile.velocity.X = Main.rand.NextFloat(-2f, 2f);
                    Projectile.velocity.Y = 1f;
                }
            }

            if (isFalling)
            {
                Projectile.velocity.Y += 0.4f;
                if (Projectile.velocity.Y > 14f) Projectile.velocity.Y = 14f;

                float angle = Projectile.velocity.ToRotation();
                if (Projectile.spriteDirection == -1)
                {
                    Projectile.rotation = angle + MathHelper.Pi - MathHelper.PiOver4;
                }
                else
                {
                    Projectile.rotation = angle + MathHelper.PiOver4;
                }
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (stuck || isFalling || (rotationFrozen && stuckNPC == null)) return false;
            return null;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!rotationFrozen)
            {
                float angle = oldVelocity.ToRotation();
                if (Projectile.spriteDirection == -1)
                {
                    finalRotation = angle + MathHelper.Pi - MathHelper.PiOver4;
                }
                else
                {
                    finalRotation = angle + MathHelper.PiOver4;
                }
            }

            rotationFrozen = true;
            Projectile.rotation = finalRotation;

            if (Projectile.spriteDirection == -1)
            {
                Projectile.position += oldVelocity * 1.2f;
            }
            else
            {
                Projectile.position += oldVelocity * 0.4f;
            }

            if (isFalling)
            {
                Projectile.velocity = Vector2.Zero;
                isFalling = false;
                return false;
            }

            Stick(null);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!stuck && !isFalling && !rotationFrozen)
            {
                target.AddBuff(ModContent.BuffType<BleedingCustom>(), 600);

                if (target.life <= 0)
                {
                    isFalling = true;
                    Projectile.friendly = false;
                    Projectile.tileCollide = true;

                    Projectile.velocity.X = Main.rand.NextFloat(-0.5f, 0.5f);
                    Projectile.velocity.Y = 1.5f;

                    SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
                    return;
                }

                finalRotation = Projectile.rotation;
                Stick(target);
            }
        }

        private void Stick(NPC target)
        {
            if (stuck) return;

            stuck = true;
            rotationFrozen = true;

            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;

            if (target != null)
            {
                stuckNPC = target;
                offsetFromNPC = Projectile.Center - target.Center;
            }

            SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
        }
    }
}