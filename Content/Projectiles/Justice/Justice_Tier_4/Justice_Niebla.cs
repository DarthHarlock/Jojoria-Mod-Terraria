using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.Justice.Justice_Tier_4
{
    public class Justice_Niebla : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        // Radio actual: lo marca el stand del dueño (RadioNiebla)
        public float Radio = 650f;

        public override void SetDefaults()
        {
            Projectile.width = 1300;
            Projectile.height = 1300;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 10;
            Projectile.hide = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            Player dueno = Main.player[Projectile.owner];

            JUSTICESTAND_Tier_4 stand = JUSTICESTAND_Tier_4.ObtenerStand(Projectile.owner);

            // Si el jugador muere o el Stand desaparece, la niebla muere
            if (stand == null || !dueno.active || dueno.dead)
            {
                Projectile.Kill();
                return;
            }

            Radio = stand.RadioNiebla;
            Projectile.width = Projectile.height = (int)(Radio * 2f);

            Projectile.timeLeft = 10;
            Projectile.Center = dueno.Center;

            // Partículas visuales
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(Radio, Radio);
                Dust ringDust = Dust.NewDustPerfect(Projectile.Center + offset, 31, Vector2.Zero, 150, Color.White, 1.5f);
                ringDust.noGravity = true;
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(Radio, Radio);
                Dust innerDust = Dust.NewDustPerfect(Projectile.Center + offset, 31, new Vector2(0f, -1.5f), 100, Color.White, 1.2f);
                innerDust.noGravity = true;
            }
        }
    }
}