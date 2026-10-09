using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Systems;

namespace Jojo.Content.Projectiles.SilverChariot.SilverChariot_Requiem
{
    public class SCR_BarragePunch_Requiem : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public bool ignoreTimeStop = true;

        private const int TimeLeftMax = 30;
        private bool offsetRead = false;

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;

            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.damage = 0;

            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.timeLeft = TimeLeftMax;
            Projectile.alpha = 0;
        }

        public static float FadeSpeed = 35f;

        public override void AI()
        {
            int standIndex = (int)Projectile.ai[0];

            if (standIndex < 0 || standIndex >= Main.maxProjectiles)
            {
                Projectile.Kill();
                return;
            }

            Projectile stand = Main.projectile[standIndex];

            if (!stand.active)
            {
                Projectile.Kill();
                return;
            }

            if (!offsetRead)
            {
                offsetRead = true;
                Projectile.localAI[0] = Projectile.velocity.X;
                Projectile.localAI[1] = Projectile.velocity.Y;
                Projectile.velocity = Vector2.Zero;
            }

            // Desencriptamos el ángulo exacto enviado por el sistema
            int rawAi1Int = (int)Projectile.ai[1];
            float decimalPart = Projectile.ai[1] - rawAi1Int;
            float angle = (decimalPart * (MathHelper.TwoPi + 0.01f)) - MathHelper.Pi;
            Vector2 dir = angle.ToRotationVector2();

            Vector2 offset = new Vector2(
                Projectile.localAI[0],
                Projectile.localAI[1]
            );

            int elapsed = TimeLeftMax - Projectile.timeLeft;
            float progress = elapsed * BarrageSystem.PunchSpeed;

            // ==========================================================
            // El punto de partida ya es correcto en ambas direcciones
            // gracias a la dirección real (dir) calculada en BarrageSystem.
            // El viejo "compensacionSimetria" de +22px solo a la izquierda
            // era un parche del bug de dirección anterior: eliminado.
            // ==========================================================
            Projectile.Center =
                stand.Center +
                offset +
                dir * progress;

            Projectile.rotation = stand.rotation;

            // Control de desvanecimiento
            if (progress >= BarrageSystem.PunchDistance)
            {
                Projectile.alpha += (int)FadeSpeed;

                if (Projectile.alpha >= 255)
                    Projectile.Kill();
            }
            else
            {
                Projectile.alpha = 0;
            }
        }

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            int raw = (int)Projectile.ai[1];
            bool goRight = raw <= 9;
            int variant = goRight ? raw : raw - 10;

            Player player = Main.player[Projectile.owner];

            string path = variant switch
            {
                1 => "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Requiem/SCR_BarragePunch_2",
                2 => "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Requiem/SCR_BarragePunch_3",
                _ => "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Requiem/SCR_BarragePunch_1",
            };

            Texture2D tex = ModContent.Request<Texture2D>(path).Value;
            Vector2 origin = tex.Size() / 2f;

            Projectile stand = Main.projectile[(int)Projectile.ai[0]];

            bool flip = stand.active && stand.Center.X < player.Center.X;

            SpriteEffects fx = flip
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            Main.EntitySpriteDraw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor * (1f - Projectile.alpha / 255f),
                Projectile.rotation,
                origin,
                1f,
                fx,
                0f
            );

            return false;
        }
    }
}