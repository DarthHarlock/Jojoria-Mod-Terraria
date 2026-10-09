using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.KingCrimson_Buffs
{
    public class Donut1 : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Si tiene Donut2, quitamos Donut1 inmediatamente
            if (player.HasBuff(ModContent.BuffType<Donut2>()))
                player.ClearBuff(Type);
        }
    }
}