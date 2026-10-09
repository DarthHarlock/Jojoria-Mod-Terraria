using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Jojo.Content.Clases;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_3.LluviaLetal_Tier_3;

namespace Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_3.LluviaLetal_Tier_3
{
    public class Gota_Tier_3 : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.timeLeft = 300;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        // La manda a la capa "detrás de proyectiles" para que la nube
        // (que se dibuja en la capa normal) siempre quede por encima.
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWireless)
        {
            behindProjectiles.Add(index);
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];

            Projectile.damage = (int)p.GetDamage<ClaseStand>().ApplyTo(8f); //DAÑO?

            Projectile.velocity.Y += 0.25f;
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Water, 0f, 0f, 100, default, 0.8f);
                dust.noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.SplashWeak, Projectile.Center);
            for (int i = 0; i < 5; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Water, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-4f, -1f));
            }
        }
    }
}