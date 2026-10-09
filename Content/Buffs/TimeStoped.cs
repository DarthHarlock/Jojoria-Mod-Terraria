using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs
{
    public class TimeStoped : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true;
        }

        public override bool RightClick(int buffIndex)
        {
            return false;
        }
    }
}