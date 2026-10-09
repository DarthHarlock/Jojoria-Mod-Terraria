using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_4
{
    public class WhitesnakePistolaPlayer_Tier_4 : ModPlayer
    {
        public override bool CanUseItem(Item item)
        {
            int standType = ModContent.ProjectileType<WHITESNAKESTAND_Tier_4>();

            foreach (Projectile proj in Main.projectile) // busca el stand del jugador y revisa si está en modo Pistola
            {
                if (proj.active && proj.owner == Player.whoAmI && proj.type == standType &&
                    proj.ModProjectile is WHITESNAKESTAND_Tier_4 stand && stand.IsPistolaActive)
                    return false;
            }

            return true;
        }
    }
}