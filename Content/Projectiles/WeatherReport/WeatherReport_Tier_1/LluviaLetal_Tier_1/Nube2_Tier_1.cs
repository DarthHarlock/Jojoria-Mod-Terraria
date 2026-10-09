using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Clases;
using Jojo.Content.Players;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_1;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_1.LluviaLetal_Tier_1;

namespace Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_1.LluviaLetal_Tier_1
{
    public class Nube2_Tier_1 : ModProjectile
    {
        private bool isFadingOut = false;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
        }

        // Comprobación infalible nativa de Terraria
        bool StandInvalido(Player p)
        {
            int standType = ModContent.ProjectileType<WEATHERSTAND_Tier_1>();
            return !p.active || p.dead || p.ownedProjectileCounts[standType] <= 0;
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];

            if (StandInvalido(p))
            {
                isFadingOut = true;
            }

            if (isFadingOut)
            {
                Projectile.velocity *= 0.9f;
                Projectile.alpha += 15;
                if (Projectile.alpha >= 255)
                {
                    Projectile.Kill();
                }
                return;
            }

            if (++Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= 4)
                {
                    Projectile.frame = 0;
                }
            }

            if (Main.rand.NextBool(3))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Cloud, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
            }

            Vector2 targetPosition = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            Vector2 direction = targetPosition - Projectile.Center;
            float distance = direction.Length();

            if (distance < 15f)
            {
                Projectile.Kill();
            }
            else
            {
                direction.Normalize();
                Projectile.velocity = direction * 12f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Player p = Main.player[Projectile.owner];

                if (!StandInvalido(p) && !isFadingOut)
                {
                    int nube1Type = ModContent.ProjectileType<Nube_1_Tier_1>();
                    int maxNubes = 2; //CANTIDA MAXIMA DE NIBES
                    int count = 0;
                    int oldestIndex = -1;
                    int oldestTimeLeft = int.MaxValue;

                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile proj = Main.projectile[i];
                        if (proj.active && proj.type == nube1Type && proj.owner == Projectile.owner)
                        {
                            count++;
                            if (proj.timeLeft < oldestTimeLeft)
                            {
                                oldestTimeLeft = proj.timeLeft;
                                oldestIndex = i;
                            }
                        }
                    }

                    if (count >= maxNubes && oldestIndex != -1)
                    {
                        Main.projectile[oldestIndex].Kill();
                    }

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        Vector2.Zero,
                        nube1Type,
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner
                    );
                }
            }
            SoundEngine.PlaySound(SoundID.Item21, Projectile.Center);
        }
    }
}