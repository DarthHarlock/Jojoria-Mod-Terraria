using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.SilverChariot_Buffs;
using Jojo.Systems;

namespace Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_2
{
    public static class ChariotDefensaSkill_Tier_2
    {
        // FIX: tope de distancia para interceptar, igual que el resto del Stand limita su
        // alcance con manR/autoR. Antes esto no existía y el Stand podía viajar hasta
        // ~200px (el radioProteccion completo) si el proyectil peligroso estaba en el borde.
        const float distanciaMaximaIntercepcion = 160f;

        public static bool EjecutarDefensa(Projectile stand, Player player, ref int frame, ref int animT, ref float syncOffX, ref float syncOffY, ref bool lastConfidentGoingRight)
        {
            if (!player.HasBuff(ModContent.BuffType<SilverDefensa>()))
                return false;

            float radioProteccion = 200f;
            Projectile proyectilPeligroso = null;
            float distanciaMinima = radioProteccion;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.hostile && proj.damage > 0)
                {
                    float dist = Vector2.Distance(player.Center, proj.Center);
                    if (dist < distanciaMinima)
                    {
                        proyectilPeligroso = proj;
                        distanciaMinima = dist;
                    }
                }
            }

            Vector2 offsetObjetivo;
            bool interceptando = (proyectilPeligroso != null);

            if (interceptando)
            {
                offsetObjetivo = proyectilPeligroso.Center - player.Center;

                // FIX: limitamos la distancia de intercepción para que el Stand no viaje
                // más lejos de lo que el resto del Stand permite normalmente.
                if (offsetObjetivo.Length() > distanciaMaximaIntercepcion)
                {
                    offsetObjetivo = Vector2.Normalize(offsetObjetivo) * distanciaMaximaIntercepcion;
                }

                stand.rotation = (proyectilPeligroso.Center - stand.Center).ToRotation();

                if (stand.Hitbox.Intersects(proyectilPeligroso.Hitbox) || Vector2.Distance(stand.Center, proyectilPeligroso.Center) < 40f)
                {
                    SoundEngine.PlaySound(SoundID.Tink, stand.Center);
                    for (int i = 0; i < 8; i++)
                    {
                        Dust.NewDustPerfect(proyectilPeligroso.Center, DustID.SilverCoin, Main.rand.NextVector2Circular(3f, 3f), 100, Color.White, 1.2f).noGravity = true;
                    }
                    proyectilPeligroso.Kill();
                }
            }
            else
            {
                offsetObjetivo = new Vector2(player.direction * 60f, -20f);
                stand.rotation = 0f;
            }

            syncOffX = offsetObjetivo.X;
            syncOffY = offsetObjetivo.Y;

            // FIX: antes esta función no tocaba lastConfidentGoingRight, y como el AI()
            // principal del Stand corta con un "return" nada más entrar en defensa, esa
            // variable se quedaba congelada con el valor de ANTES de pulsar G. Por eso el
            // Stand se quedaba mirando siempre al último lado en que habías atacado, sin
            // importar hacia dónde estuvieras mirando al activar la defensa.
            if (System.Math.Abs(offsetObjetivo.X) > 40f)
            {
                lastConfidentGoingRight = offsetObjetivo.X >= 0;
            }

            ParticulasStands.FollowPlayer(stand, player, offsetObjetivo, 0.45f);

            int delayAnimacion = (frame >= 4) ? 1 : 6;
            if (++animT >= delayAnimacion)
            {
                animT = 0;
                if (interceptando)
                {
                    frame = (frame < 4 || frame >= 7) ? 4 : frame + 1;
                }
                else
                {
                    frame = (frame >= 3) ? 0 : frame + 1;
                }
            }

            return true;
        }
    }
}