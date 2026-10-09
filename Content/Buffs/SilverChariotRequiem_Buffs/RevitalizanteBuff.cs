using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.SilverChariotRequiem_Buffs
{
    public class RevitalizanteBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;      // es un buff, no un debuff
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        // Solo se aplica a NPCs hostiles, no a jugadores
        public override void Update(NPC npc, ref int buffIndex)
        {
            // EFECTO PLACEHOLDER: se mueve un 25% más rápido (lo contrario
            // al truco de la lentitud). Cámbialo por lo que quieras.
            npc.position += npc.velocity * 0.25f;
        }
    }
}