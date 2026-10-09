using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.Justice_Buffs
{
    public class ControlMental1 : ModBuff
    {
        public override string Texture => "Jojo/Content/Buffs/Justice_Buffs/ControlMental1";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = false;
        }
    }
}