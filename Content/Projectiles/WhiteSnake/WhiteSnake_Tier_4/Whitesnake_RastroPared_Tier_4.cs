using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Jojo.Content.Buffs;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_4.Vomito
{
    public class Whitesnake_RastroPared_Tier_4 : ModProjectile
    {
        public override string Texture => "Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_4/Vomito/RastroPared";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.hostile = false;

            // Daño corregido a tu clase personalizada
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.timeLeft = 300;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = 0f;

            if (Projectile.ai[0] == 1f)
            {
                Projectile.spriteDirection = -1;
            }
            else if (Projectile.ai[0] == -1f)
            {
                Projectile.spriteDirection = 1;
            }

            if (Main.rand.NextBool(15))
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Web, 0f, 0f, 150, Color.White, 0.7f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity.Y += 0.5f;
                Main.dust[d].velocity.X *= 0.1f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 origin = new Vector2(0f, tex.Height / 2f);

            Main.EntitySpriteDraw(tex, drawPos, null, lightColor, Projectile.rotation, origin, Projectile.scale, effects, 0);

            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // ÁCIDO ÚNICO: las stats (duración, daño, slow) las pone el stand del dueño.
            AcidoBlancoNPC_Tier_4.AplicarDesdeProyectil(target, Projectile);
        }
    }
}