using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using System;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.KillerQueen_Buffs;
using Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_4;

namespace Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_4.BitesTheDust
{
    public static class BitesTheDustSystem
    {
        public static NPC FindBTDTarget(Player player, float maxRange)
        {
            Vector2 mouseWorld = Main.MouseWorld;
            NPC closest = null;
            float closestDist = maxRange;

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.lifeMax > 5 && !npc.dontTakeDamage)
                {
                    if (npc.Hitbox.Contains(mouseWorld.ToPoint())) return npc;
                }
            }

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.lifeMax > 5 && !npc.dontTakeDamage && npc.chaseable)
                {
                    float dist = Vector2.Distance(player.Center, npc.Center);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = npc;
                    }
                }
            }

            return closest;
        }

        public static void FollowNPC(Projectile proj, NPC npc, Vector2 baseOffset)
        {
            int dir = npc.direction != 0 ? npc.direction : 1;
            if (npc.spriteDirection != 0) dir = npc.spriteDirection;

            float floatY = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f) * 6f;

            Vector2 finalOffset = new Vector2(baseOffset.X * -dir, baseOffset.Y + floatY);
            Vector2 targetPos = npc.Center + finalOffset;

            proj.Center = Vector2.Lerp(proj.Center, targetPos, 0.15f);
            proj.direction = dir;
        }

        public static void CancelBTD(Player player, Projectile proj, KILLERQUEENSTAND_Tier_4 kq)
        {
            kq.isBitesTheDustMini = false;
            kq.btdHostNPC = -1;
            proj.netUpdate = true;

            var btdPlayer = player.GetModPlayer<BitesTheDustPlayer>();
            btdPlayer.btdActive = false;
            btdPlayer.isExplodingPhase = false;

            if (player.HasBuff(ModContent.BuffType<BitesTheDustBuff>()))
            {
                player.ClearBuff(ModContent.BuffType<BitesTheDustBuff>());
            }
        }
    }
}