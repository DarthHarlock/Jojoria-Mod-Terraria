using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Buffs;

namespace Jojo.Content.Buffs.KingCrimson_Buffs
{
    public class TimeErased : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override bool RightClick(int buffIndex) => false;

        public override void Update(Player player, ref int buffIndex)
        {
            player.immune = true;
            player.immuneTime = 2;
            for (int i = 0; i < player.hurtCooldowns.Length; i++)
                player.hurtCooldowns[i] = 2;
            player.GetModPlayer<TimeErasedPlayer>().isTimeErased = true;
        }
    }

    public class TimeErasedPlayer : ModPlayer
    {
        public bool isTimeErased;
        private bool wasTimeErased = false;

        public override void ResetEffects() => isTimeErased = false;

        public override void PostUpdate()
        {
            if (isTimeErased)
            {
                wasTimeErased = true;
            }
            else if (wasTimeErased)
            {
                // Habilidad terminó o fue cancelada: dar Donut1 por 3 seg  
                Player.AddBuff(ModContent.BuffType<Donut1>(), 100);
                wasTimeErased = false;
            }
        }

        public override bool CanUseItem(Item item)
        {
            if (isTimeErased && (item.createTile != -1 || item.createWall != -1)) return false;
            return base.CanUseItem(item);
        }

        public override bool? CanHitNPCWithItem(Item item, NPC target)
        {
            if (isTimeErased) return false;
            return base.CanHitNPCWithItem(item, target);
        }

        public override bool? CanHitNPCWithProj(Projectile proj, NPC target)
        {
            if (isTimeErased) return false;
            return base.CanHitNPCWithProj(proj, target);
        }

        public override bool CanHitPvp(Item item, Player target)
        {
            if (isTimeErased) return false;
            return base.CanHitPvp(item, target);
        }

        public override bool CanHitPvpWithProj(Projectile proj, Player target)
        {
            if (isTimeErased) return false;
            return base.CanHitPvpWithProj(proj, target);
        }
    }
}