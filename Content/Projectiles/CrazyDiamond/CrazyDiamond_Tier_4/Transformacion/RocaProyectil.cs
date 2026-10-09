using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;

namespace Jojo.Content.Projectiles.CrazyDiamond.Transformacion
{
    public class RocaProyectil : ModProjectile
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.StoneBlock;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
        }

        public override void AI()
        {
            Projectile.rotation += 0.5f * (Projectile.velocity.X > 0 ? 1f : -1f);

            for (int i = 0; i < 2; i++)
            {
                Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(10f, 10f);
                Dust d = Dust.NewDustPerfect(dustPos, DustID.YellowTorch, Main.rand.NextVector2Circular(1.5f, 1.5f), 100, default, 1.3f);
                d.noGravity = true;
            }

            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex >= 0 && targetIndex < Main.maxNPCs && Main.npc[targetIndex].active)
            {
                NPC target = Main.npc[targetIndex];
                Vector2 dir = target.Center - Projectile.Center;
                float dist = dir.Length();

                if (dist < 22f)
                {
                    Projectile.Kill();
                    return;
                }

                Projectile.ai[1]++;
                if (Projectile.ai[1] > 8)
                {
                    dir.Normalize();
                    Projectile.velocity = (Projectile.velocity * 12f + dir * 22f) / 13f;
                }
            }
            else
            {
                Projectile.Kill();
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);

            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Stone,
                    Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f), Scale: 1.2f);

                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.YellowTorch, Main.rand.NextVector2Circular(3f, 3f), Scale: 1.5f);
                d.noGravity = true;
            }
        }
    }
}