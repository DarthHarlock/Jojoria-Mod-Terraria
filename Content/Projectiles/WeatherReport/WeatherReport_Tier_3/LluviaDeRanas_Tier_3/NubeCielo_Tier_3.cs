using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Buffs.WeatherReport_Buffs;

namespace Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_3.LluviaDeRanas_Tier_3
{
    public class NubeCielo_Tier_3 : ModProjectile
    {
        private bool dying = false;
        private float scale;

        // Evita crasheos de compilación cargando la textura 1 por defecto
        public override string Texture => "Jojo/Content/Projectiles/WeatherReport/WeatherReport_Tier_3/LluviaDeRanas_Tier_3/NubeCielo1";

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 60;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 60;
            scale = Main.rand.NextFloat(0.85f, 1.3f);
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];

            // Calculamos a qué altura debería estar la nube
            float targetY = p.Center.Y - Projectile.ai[1] - (Projectile.height / 2f);

            // Si es el primer frame (localAI[0] == 0), ponemos la nube en su sitio de golpe para que no vuele desde el origen
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.position.Y = targetY;
                Projectile.localAI[0] = 1f;
            }
            else
            {
                // A partir del primer frame, la nube sigue tu altura con un retraso suave (Lerp)
                // El 0.05f controla la velocidad a la que te sigue. Cuanto menor sea, más tardará en alcanzarte.
                Projectile.position.Y = MathHelper.Lerp(Projectile.position.Y, targetY, 0.05f);
            }

            // Efecto Parallax infinito: si salen por los bordes reaparecen al otro lado
            if (Projectile.Center.X < p.Center.X - 1500f) Projectile.position.X += 3000f;
            if (Projectile.Center.X > p.Center.X + 1500f) Projectile.position.X -= 3000f;

            bool hasBuff = p.HasBuff(ModContent.BuffType<DuracionLluvia>());

            if (hasBuff && !dying)
            {
                Projectile.timeLeft = 60;
                if (Projectile.alpha > 0)
                {
                    Projectile.alpha -= 5;
                }
            }
            else
            {
                dying = true;
                Projectile.alpha += 5;

                if (Projectile.alpha >= 255)
                {
                    Projectile.Kill();
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Controla que se dibujen los 4 sprites aleatorios que le pasa el LluviaDeRanasSkill_Tier_3
            string texturePath = "Jojo/Content/Projectiles/WeatherReport/WeatherReport_Tier_3/LluviaDeRanas_Tier_3/NubeCielo" + (int)Projectile.ai[0];
            Texture2D texture = ModContent.Request<Texture2D>(texturePath).Value;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() / 2f;

            Color drawColor = lightColor * ((255f - Projectile.alpha) / 255f);

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                null,
                drawColor,
                Projectile.rotation,
                origin,
                scale,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }
}