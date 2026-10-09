// UBICACIÓN: Content/Projectiles/WeatherReport/WeatherReport_Tier_4/Arcoiris_Tier_4/NubeCielo_Arcoiris_Tier_4.cs
// TEXTURAS NECESARIAS (mismo folder que este script):
//   Content/Projectiles/WeatherReport/WeatherReport_Tier_4/Arcoiris_Tier_4/NubeCielo1.png
//   Content/Projectiles/WeatherReport/WeatherReport_Tier_4/Arcoiris_Tier_4/NubeCielo2.png
//   Content/Projectiles/WeatherReport/WeatherReport_Tier_4/Arcoiris_Tier_4/NubeCielo3.png
//   Content/Projectiles/WeatherReport/WeatherReport_Tier_4/Arcoiris_Tier_4/NubeCielo4.png
// (Puedes reutilizar el mismo arte que las nubes de LluviaDeRanas, o hacer una
//  variante con tinte de colores para que se note que es la habilidad de arcoiris)

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Buffs.WeatherReport_Buffs;

namespace Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_4.Arcoiris_Tier_4
{
    public class NubeCielo_Arcoiris_Tier_4 : ModProjectile
    {
        private bool dying = false;
        private float scale;

        // Evita crasheos de compilación cargando la textura 1 por defecto
        public override string Texture => "Jojo/Content/Projectiles/WeatherReport/WeatherReport_Tier_4/Arcoiris_Tier_4/NubeCielo1";

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

            // Primer frame: la colocamos de golpe para que no "vuele" desde el origen
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.position.Y = targetY;
                Projectile.localAI[0] = 1f;
            }
            else
            {
                Projectile.position.Y = MathHelper.Lerp(Projectile.position.Y, targetY, 0.05f);
            }

            // Parallax infinito: si sale por los bordes, reaparece al otro lado
            if (Projectile.Center.X < p.Center.X - 1500f) Projectile.position.X += 3000f;
            if (Projectile.Center.X > p.Center.X + 1500f) Projectile.position.X -= 3000f;

            bool hasBuff = p.HasBuff(ModContent.BuffType<DuracionArcoiris>());

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
            // Dibuja el sprite aleatorio (1-4) que le pasó ArcoirisSkill_Tier_4
            string texturePath = "Jojo/Content/Projectiles/WeatherReport/WeatherReport_Tier_4/Arcoiris_Tier_4/NubeCielo" + (int)Projectile.ai[0];
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