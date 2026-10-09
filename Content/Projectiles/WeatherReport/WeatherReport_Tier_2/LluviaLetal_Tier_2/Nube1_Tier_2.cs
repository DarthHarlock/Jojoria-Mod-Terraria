using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using Jojo.Content.Clases;
using Jojo.Content.Players;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_2;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_2.LluviaLetal_Tier_2;

namespace Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_2.LluviaLetal_Tier_2
{
    public class Nube_1_Tier_2 : ModProjectile
    {
        public override string Texture => "Jojo/Content/Projectiles/WeatherReport/WeatherReport_Tier_2/LluviaLetal_Tier_2/Nube_1_Tier_2";

        const int FadeInTime = 30;
        const int FadeOutTime = 30;
        const int MaxLife = 1800;

        // ================== CONFIGURACIÓN DE LLUVIA ==================
        public float gotasPorSegundo = 6f;
        public float extraSpawnWidth = 0f;
        public float dropSpawnYOffset = 15f;
        // ================================================================

        private float spawnAcumulador = 0f;
        private bool isFadingOut = false;

        static int standTypeCache = -1;
        static int StandType => standTypeCache != -1 ? standTypeCache : (standTypeCache = ModContent.ProjectileType<WEATHERSTAND_Tier_2>());

        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 28;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = MaxLife;
            Projectile.alpha = 255;
        }

        bool StandRoto(Player p)
        {
            if (!p.active || p.dead) return true;
            if (!p.TryGetModPlayer(out StandStatsPlayer stats)) return true;
            if (stats.activeStand == null) return true;
            if (!stats.activeStand.active) return true;
            if (stats.activeStand.type != StandType) return true;
            return false;
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];

            if (StandRoto(p))
            {
                isFadingOut = true;
            }

            if (isFadingOut)
            {
                Projectile.alpha += 10;
                if (Projectile.alpha >= 255)
                {
                    Projectile.Kill();
                }
                return;
            }

            if (Projectile.owner == Main.myPlayer && Projectile.ai[0] == 0f)
            {
                Projectile.ai[0] = Main.rand.Next(1, 4); // 1, 2 o 3
                Projectile.netUpdate = true;
            }

            int age = MaxLife - Projectile.timeLeft;

            if (age < FadeInTime)
            {
                Projectile.alpha = (int)MathHelper.Lerp(255, 0, age / (float)FadeInTime);
            }
            else if (Projectile.timeLeft <= FadeOutTime)
            {
                Projectile.alpha = (int)MathHelper.Lerp(255, 0, Projectile.timeLeft / (float)FadeOutTime);
            }
            else
            {
                Projectile.alpha = 0;
            }

            if (Projectile.owner == Main.myPlayer && age >= FadeInTime && Projectile.timeLeft > FadeOutTime)
            {
                spawnAcumulador += gotasPorSegundo / 60f;

                while (spawnAcumulador >= 1f)
                {
                    SpawnGota();
                    spawnAcumulador -= 1f;
                }
            }
        }

        void SpawnGota()
        {
            float halfWidth = (Projectile.width + extraSpawnWidth) / 2f;
            float spawnY = Projectile.position.Y + Projectile.height + dropSpawnYOffset;

            float spawnX = Projectile.Center.X - halfWidth + Main.rand.NextFloat(halfWidth * 2f);

            Vector2 spawnPos = new Vector2(spawnX, spawnY);
            Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(4f, 6f));

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPos,
                velocity,
                ModContent.ProjectileType<Gota_Tier_2>(),
                Projectile.damage,
                0f,
                Projectile.owner
            );
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int nubeIndex = Projectile.ai[0] == 0f ? 1 : (int)Projectile.ai[0];
            Texture2D texture = ModContent.Request<Texture2D>($"Jojo/Content/Projectiles/WeatherReport/WeatherReport_Tier_2/LluviaLetal_Tier_2/Nube_{nubeIndex}_Tier_2").Value;

            Vector2 drawOrigin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                null,
                lightColor * ((255 - Projectile.alpha) / 255f),
                Projectile.rotation,
                drawOrigin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }
}