using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;

namespace Jojo.Content.Projectiles.SilverChariot.SilverChariot_Requiem
{
    public class ZZZ_Visual : ModProjectile
    {
        // Ruta al sprite
        public override string Texture => "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Requiem/ZZZ";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 100; // Lo que tarda en desaparecer
        }

        public override void AI()
        {
            // Subir lentamente
            Projectile.velocity.Y = -1.2f;

            // Movimiento de serpiente suave (Onda Senoidal)
            Projectile.velocity.X = (float)Math.Sin(Projectile.timeLeft * 0.15f) * 1.5f;

            // Transparencia al final de su vida
            if (Projectile.timeLeft < 30)
            {
                Projectile.alpha += (255 / 30);
            }
        }
    }
}