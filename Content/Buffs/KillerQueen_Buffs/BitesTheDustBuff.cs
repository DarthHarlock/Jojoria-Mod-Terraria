using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.KillerQueen_Buffs
{
    public class BitesTheDustBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
        }
    }
}