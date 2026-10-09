using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Audio;
using System;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem.Laser
{
    public class GoldenRequiem_Laser : ModProjectile
    {
        public override string Texture => "Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Requiem/Laser/Laser_Segmento";

        bool initialized = false;
        Vector2 startPos;
        Vector2 fixedVelocity;

        // --- Estado de choque contra un tile sólido ---
        bool hitWall = false;      // true en cuanto el frente del rayo toca algo sólido
        int hitFrontIndex = 0;     // segmento exacto en el que se congela el crecimiento
        int hitTimer = 0;          // valor de localAI[0] en el instante del impacto

        // Asumimos un ancho de segmento aproximado de 22 píxeles para el cálculo de colisiones.
        const float partWidth = 22f;
        const int totalSegments = 20;
        const int maxPieces = totalSegments + 2; // 1 Inicio + 20 Segmentos + 1 Final = 22 piezas
        const int speed = 3; // Cuántos segmentos aparecen/desaparecen por tick

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1; // Atraviesa infinitamente
            Projectile.tileCollide = false;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.ignoreWater = true;

            Projectile.timeLeft = 300; // El proyectil se destruirá manualmente en la AI
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // Solo golpea 1 vez por enemigo durante su rápido ciclo de vida
        }

        public override void AI()
        {
            if (!initialized)
            {
                // Guardamos la posición y ángulo exactos en el frame 1. Ya NO seguirá al Stand.
                startPos = Projectile.Center;
                fixedVelocity = Projectile.velocity;
                Projectile.rotation = fixedVelocity.ToRotation();
                initialized = true;
            }

            Projectile.localAI[0]++; // Aumentamos el temporizador interno

            GetSegmentRange(out int frontIndex, out int backIndex);

            // Mientras el rayo siga creciendo (no haya chocado todavía), comprobamos si su frente
            // acaba de tocar un tile sólido.
            if (!hitWall)
            {
                CheckTileCollision(frontIndex);
                if (hitWall) GetSegmentRange(out frontIndex, out backIndex); // recalculamos si chocó justo este tick
            }

            // Partículas doradas/amarillas mientras el rayo está activo (ahora bastantes más)
            if (frontIndex > backIndex)
            {
                Vector2 tipPos = startPos + fixedVelocity * (frontIndex * partWidth);
                SpawnTrailDust(tipPos);

                for (int i = backIndex; i < frontIndex; i++)
                {
                    Vector2 segPos = startPos + fixedVelocity * (i * partWidth);

                    // Antes soltaba partículas en 1 de cada 4 segmentos; ahora casi todos sueltan algo.
                    if (Main.rand.NextBool(2))
                    {
                        Dust d = Dust.NewDustPerfect(segPos, DustID.GoldFlame, Main.rand.NextVector2Circular(1.6f, 1.6f), 0, default, Main.rand.NextFloat(0.9f, 1.4f));
                        d.noGravity = true;
                    }

                    if (Main.rand.NextBool(3))
                    {
                        Dust d = Dust.NewDustPerfect(segPos, DustID.YellowStarDust, Main.rand.NextVector2Circular(1.0f, 1.0f), 0, default, Main.rand.NextFloat(0.7f, 1.1f));
                        d.noGravity = true;
                        d.fadeIn = 0.8f;
                    }
                }
            }

            // Si ya está en fase de retroceso (por choque o por llegar a su longitud máxima) y la
            // cola alcanzó a la cabeza, el rayo se destruye por completo (como ya hacía normalmente).
            bool receding = hitWall || frontIndex >= maxPieces;
            if (receding && backIndex >= frontIndex)
            {
                Projectile.Kill();
            }
        }

        // Calcula, de forma centralizada, qué segmentos van desde "backIndex" (cola) hasta
        // "frontIndex" (cabeza) en el tick actual. Se usa en AI, Colliding y PreDraw para que
        // los tres estén siempre sincronizados.
        void GetSegmentRange(out int frontIndex, out int backIndex)
        {
            int timer = (int)Projectile.localAI[0];

            if (hitWall)
            {
                // El rayo dejó de generarse en seco justo en el punto de impacto.
                // A partir de ahí solo le queda "desaparecer" hacia adelante, igual que al final normal.
                frontIndex = hitFrontIndex;
                int ticksSinceHit = timer - hitTimer;
                backIndex = ticksSinceHit * speed;
            }
            else
            {
                frontIndex = timer * speed;
                backIndex = 0;

                if (frontIndex > maxPieces)
                {
                    frontIndex = maxPieces;
                    backIndex = (timer - (maxPieces / speed)) * speed;
                }
            }

            frontIndex = Math.Clamp(frontIndex, 0, maxPieces);
            backIndex = Math.Clamp(backIndex, 0, maxPieces);
        }

        // Revisa, segmento a segmento desde el inicio del rayo, si alguno ya está dentro de un tile sólido.
        void CheckTileCollision(int frontIndex)
        {
            for (int i = 0; i <= frontIndex && i <= maxPieces; i++)
            {
                Vector2 segPos = startPos + fixedVelocity * (i * partWidth);

                if (Collision.SolidCollision(segPos - new Vector2(4f), 8, 8))
                {
                    hitWall = true;
                    hitFrontIndex = i;
                    hitTimer = (int)Projectile.localAI[0];
                    SpawnImpactDust(segPos);
                    break;
                }
            }
        }

        // Chispas doradas continuas en la punta del rayo mientras avanza (el doble que antes).
        void SpawnTrailDust(Vector2 tipPos)
        {
            for (int i = 0; i < 7; i++)
            {
                Vector2 vel = fixedVelocity.RotatedByRandom(MathHelper.ToRadians(25)) * Main.rand.NextFloat(0.5f, 3f);
                Dust d = Dust.NewDustPerfect(tipPos + Main.rand.NextVector2Circular(8f, 8f), DustID.GoldFlame, vel, 0, default, Main.rand.NextFloat(1.0f, 1.7f));
                d.noGravity = true;
                d.fadeIn = 1f;
            }
        }

        // Explosión de partículas doradas/amarillas al chocar contra una superficie sólida.
        void SpawnImpactDust(Vector2 position)
        {
            Vector2 back = -fixedVelocity;

            // Ráfaga principal, como si el rayo "rebotara" hacia atrás al impactar.
            for (int i = 0; i < 35; i++)
            {
                Vector2 vel = back.RotatedByRandom(MathHelper.ToRadians(75)) * Main.rand.NextFloat(1.5f, 8f);
                Dust d = Dust.NewDustPerfect(position, DustID.GoldFlame, vel, 0, default, Main.rand.NextFloat(1.1f, 2.0f));
                d.noGravity = Main.rand.NextBool(2);
                d.fadeIn = 1f;
            }

            // Chispas amarillas que caen y se esparcen contra el suelo.
            for (int i = 0; i < 20; i++)
            {
                Vector2 vel = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-2f, 1f));
                Dust d = Dust.NewDustPerfect(position, DustID.YellowStarDust, vel, 0, default, Main.rand.NextFloat(1.0f, 1.5f));
                d.noGravity = false; // estas sí caen y "chocan" visualmente contra el suelo
            }

            SoundEngine.PlaySound(SoundID.Dig, position);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!initialized) return false;

            GetSegmentRange(out int frontIndex, out int backIndex);

            Vector2 actualStart = startPos + fixedVelocity * (backIndex * partWidth);
            Vector2 actualEnd = startPos + fixedVelocity * (frontIndex * partWidth);

            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), actualStart, actualEnd, 22f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!initialized) return false;

            Texture2D texInicio = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Requiem/Laser/Laser_Inicio").Value;
            Texture2D texSeg = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Requiem/Laser/Laser_Segmento").Value;
            Texture2D texFinal = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Requiem/Laser/Laser_Final").Value;

            float rot = fixedVelocity.ToRotation();
            Color color = lightColor * ((255 - Projectile.alpha) / 255f);

            GetSegmentRange(out int frontIndex, out int backIndex);

            Vector2 baseDrawPos = startPos - Main.screenPosition;

            // Dibuja solo los segmentos entre la cola (que va desapareciendo) y el frente (que va avanzando)
            for (int i = backIndex; i < frontIndex; i++)
            {
                Texture2D texToDraw = texSeg;
                Vector2 origin = new Vector2(0, texSeg.Height / 2f);

                // Si el rayo chocó, dibujamos la puntera de "final" justo en el último segmento visible,
                // para que se vea un corte limpio en el punto de impacto en vez de un corte abrupto.
                bool esUltimoSegmento = i == maxPieces - 1 || (hitWall && i == frontIndex - 1);

                if (i == 0)
                {
                    texToDraw = texInicio;
                    origin = new Vector2(0, texInicio.Height / 2f);
                }
                else if (esUltimoSegmento)
                {
                    texToDraw = texFinal;
                    origin = new Vector2(0, texFinal.Height / 2f);
                }

                // Ajustamos la posición en base al índice para crear la línea recta
                Vector2 drawPos = baseDrawPos + fixedVelocity * (i * partWidth);

                Main.EntitySpriteDraw(
                    texToDraw, drawPos, null, color, rot,
                    origin, Projectile.scale, SpriteEffects.None, 0
                );
            }

            return false;
        }
    }
}