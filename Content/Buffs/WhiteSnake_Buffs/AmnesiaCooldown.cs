using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.WhiteSnake_Buffs
{

    public class AmnesiaCooldown : ModBuff
    {
        
        public override string Texture => "Jojo/Content/Buffs/WhiteSnake_Buffs/AmnesiaDebuff";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {

        }
    }
}