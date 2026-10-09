using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using System;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_1.LluviaLetal_Tier_1;

namespace Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_1.Nubes_Tier_1
{
    public class NubeVisual
    {
        public Vector2 offset;
        public float velocityX;
        public float alpha;
        public int lifeTime;
        public int maxLifeTime;
        public int textureIndex;
        public float scale;
        public bool isDying;
        public bool enFrente; // Determina si va por encima del stand

        public NubeVisual(Vector2 startOffset, float speed, int life, int texIndex, float scale, bool enFrente)
        {
            this.offset = startOffset;
            this.velocityX = speed;
            this.lifeTime = 0;
            this.maxLifeTime = life;
            this.textureIndex = texIndex;
            this.alpha = 0f;
            this.scale = scale;
            this.isDying = false;
            this.enFrente = enFrente; // Asignamos si va delante o detrás
        }

        public void Update(bool standDespawning)
        {
            offset.X += velocityX;

            if (standDespawning)
            {
                isDying = true;
            }

            if (isDying)
            {
                alpha -= 0.08f;
            }
            else
            {
                lifeTime++;

                if (lifeTime < 12)
                {
                    alpha += 0.083f;
                }
                else if (lifeTime > maxLifeTime - 15)
                {
                    alpha -= 0.066f;
                }

                if (lifeTime >= maxLifeTime)
                {
                    isDying = true;
                }
            }

            alpha = MathHelper.Clamp(alpha, 0f, 1f);
        }
    }
}