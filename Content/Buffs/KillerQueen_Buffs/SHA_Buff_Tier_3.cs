using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Projectiles.Minions;

namespace Jojo.Content.Buffs.KillerQueen_Buffs
{
    public class SHA_Buff_Tier_3 : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false; // importante: visible para debug del timer
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.dead)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
                return;
            }

            int minionType = ModContent.ProjectileType<SHA_Minion_Tier_3>();

            // 🔧 CONTROL PRINCIPAL (igual que tu versión vieja estable)
            if (player.ownedProjectileCounts[minionType] <= 0 &&
                player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    player.GetSource_Buff(buffIndex),
                    player.Center,
                    Vector2.Zero,
                    minionType,
                    40,
                    0f,
                    player.whoAmI
                );
            }

            // ❌ IMPORTANTE:
            // NO escanear Main.projectile
            // NO forzar respawn adicional
            // NO resetear timeLeft aquí
        }
    }
}