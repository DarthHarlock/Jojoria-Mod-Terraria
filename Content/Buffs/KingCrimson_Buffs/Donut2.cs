using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.KingCrimson_Buffs
{
    public class Donut2 : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Si tiene Donut1, quitamos Donut2 inmediatamente
            if (player.HasBuff(ModContent.BuffType<Donut1>()))
                player.ClearBuff(Type);
        }
    }
}