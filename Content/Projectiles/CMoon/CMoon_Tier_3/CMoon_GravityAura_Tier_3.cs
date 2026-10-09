using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.CMoon.CMoon_Tier_3
{
    public class CMoon_GravityAura_Tier_3 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 20; // Un poquito más (1/3 de segundo) para permitirte moverte y atrapar varios
            Projectile.hide = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];

            // 1. El aura sigue al jugador
            Projectile.Center = p.Center;

            float radius = Projectile.ai[0];

            // 2. Ejecución continua: Atrapa a cualquier enemigo que entre en el área durante los 20 ticks
            CMoonSkillHandler_Tier_3.DeteccionContinuaAura(p, Projectile.Center, radius);

            // 3. Efectos visuales de seguimiento continuo
            for (int i = 0; i < 15; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                Dust ringDust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.ChlorophyteWeapon, Vector2.Zero, 100, Color.LimeGreen, 1.5f);
                ringDust.noGravity = true;
            }

            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(radius, radius);
                Dust innerDust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.GreenFairy, new Vector2(0f, -3f), 100, Color.LimeGreen, 1.3f);
                innerDust.noGravity = true;
            }
        }
    }
}