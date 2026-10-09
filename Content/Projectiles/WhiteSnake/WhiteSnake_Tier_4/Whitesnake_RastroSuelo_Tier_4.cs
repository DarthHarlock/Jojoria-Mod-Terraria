using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Buffs;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_4.Vomito
{
    public class Whitesnake_RastroSuelo_Tier_4 : ModProjectile
    {
        public override string Texture => "Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_4/Vomito/RastroSuelo";

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;

            // Daño corregido a tu clase personalizada
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.penetrate = -1;
            Projectile.timeLeft = 660;
            Projectile.tileCollide = true;
            Projectile.alpha = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return true;
        }

        public override void AI()
        {
            Projectile.rotation = 0f;
            Projectile.velocity.Y += 0.45f;
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
            }

            if (Projectile.timeLeft <= 60)
            {
                Projectile.alpha += 255 / 60;
                if (Projectile.alpha > 255)
                {
                    Projectile.alpha = 255;
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = Vector2.Zero;
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // ÁCIDO ÚNICO: las stats (duración, daño, slow) las pone el stand del dueño.
            AcidoBlancoNPC_Tier_4.AplicarDesdeProyectil(target, Projectile);
        }
    }
}