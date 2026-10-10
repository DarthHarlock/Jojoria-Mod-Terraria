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
            Main.buffNoTimeDisplay[Type] = true;
        }

        // El buff es solo visual: se mantiene vivo en servidor y clientes sin depender
        // de su duración (en multijugador la duración viaja como short, máx. 32767).
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.buffTime[buffIndex] = 18000;
        }
    }
}