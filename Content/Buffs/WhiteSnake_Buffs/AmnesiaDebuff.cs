using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.WhiteSnake_Buffs
{
    public class AmnesiaDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }
    }
}