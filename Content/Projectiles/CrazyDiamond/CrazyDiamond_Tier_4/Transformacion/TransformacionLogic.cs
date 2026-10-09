using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.CrazyDiamond.Transformacion
{
    public static class TransformacionLogic
    {
        public static void HealTargetNPC(NPC target, float hostileHealPct, float friendlyHealPct)
        {
            float healPercent = (target.friendly || target.townNPC) ? friendlyHealPct : hostileHealPct;

            int healAmount = (int)(target.lifeMax * healPercent);
            if (healAmount <= 0) healAmount = 1;

            target.life += healAmount;
            if (target.life > target.lifeMax)
            {
                target.life = target.lifeMax;
            }

            target.HealEffect(healAmount, true);
            target.netUpdate = true;
        }

        // Cura a un jugador (con o sin PvP). Se llama desde el cliente dueño del stand.
        public static void HealTargetPlayer(Player target, float healPercent)
        {
            int healAmount = (int)(target.statLifeMax2 * healPercent);
            if (healAmount <= 0) healAmount = 1;

            if (target.whoAmI == Main.myPlayer)
            {
                target.Heal(healAmount);
            }
            else if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // El servidor reenvía la curación al jugador correspondiente
                NetMessage.SendData(MessageID.SpiritHeal, -1, -1, null, target.whoAmI, healAmount);
            }
        }

        // Solo servidor / singleplayer (AddCharge ya ignora a los clientes).
        public static void AddRockCharge(NPC target, float amount, float decayRate, int decayDelay, float defenseMult, int cinDuration, int rockSpwnRate, float bossHpThreshold, float bossHpScaling, float cooldownDecay, float transformDamage)
        {
            if (target.type == ModContent.NPCType<Roca>()) return;

            if (target.TryGetGlobalNPC(out TransformacionGlobalNPC gNPC))
            {
                NPC master = gNPC.GetMasterNPC(target);
                if (master.TryGetGlobalNPC(out TransformacionGlobalNPC masterGNPC))
                {
                    masterGNPC.AddCharge(master, target.Center, amount, decayRate, decayDelay, defenseMult, cinDuration, rockSpwnRate, bossHpThreshold, bossHpScaling, cooldownDecay, transformDamage);
                }
            }
        }
    }
}