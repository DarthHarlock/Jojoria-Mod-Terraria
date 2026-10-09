using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.SilverChariotRequiem_Buffs
{
    public class RequiemLentitudDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // TRUCO MAGISTRAL: En lugar de reducir npc.velocity (lo cual rompe
            // la aceleración de la IA voladora y causa pausas), simplemente
            // movemos al NPC hacia atrás un porcentaje de su velocidad actual.
            // Esto anula un 25% de su movimiento real sin que la IA se vuelva loca.
            npc.position -= npc.velocity * 0.50f;
        }
    }
}