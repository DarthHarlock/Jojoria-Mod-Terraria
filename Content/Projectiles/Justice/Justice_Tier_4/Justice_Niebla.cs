using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.Justice.Justice_Tier_4
{
    public class Justice_Niebla : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        public const float AuraRadius = 650f;

        public override void SetDefaults()
        {
            Projectile.width = (int)(AuraRadius * 2);
            Projectile.height = (int)(AuraRadius * 2);
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 10; // Se mantendrá vivo dinámicamente
            Projectile.hide = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            Player dueño = Main.player[Projectile.owner];

            // Verificar si el Stand sigue activo
            bool standActivo = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Projectile.owner && p.type == ModContent.ProjectileType<JUSTICESTAND_Tier_4>())
                {
                    standActivo = true;
                    break;
                }
            }

            // Si el jugador muere o el Stand desaparece, la niebla muere
            if (!standActivo || !dueño.active || dueño.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 10; // Resetear tiempo de vida continuamente
            Projectile.Center = dueño.Center;

            // Partículas visuales
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(AuraRadius, AuraRadius);
                Dust ringDust = Dust.NewDustPerfect(Projectile.Center + offset, 31, Vector2.Zero, 150, Color.White, 1.5f);
                ringDust.noGravity = true;
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(AuraRadius, AuraRadius);
                Dust innerDust = Dust.NewDustPerfect(Projectile.Center + offset, 31, new Vector2(0f, -1.5f), 100, Color.White, 1.2f);
                innerDust.noGravity = true;
            }
        }
    }
}