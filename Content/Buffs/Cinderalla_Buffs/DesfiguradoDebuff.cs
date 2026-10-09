using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.Cinderalla_Buffs
{
    // Debuff genérico de la línea Cinderella: reduce defensa y velocidad.
    // A PROPÓSITO no define aquí cuánto reduce ni cuánto dura — eso lo decide
    // cada stand que lo aplique (cualquier tier, presente o futuro) a través de
    // DesfiguradoIconGlobalNPC.Aplicar(...). Este archivo no debería volver a
    // tocarse al añadir tiers nuevos.
    public class DesfiguradoDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            var stats = npc.GetGlobalNPC<DesfiguradoIconGlobalNPC>();

            // Reduce la defensa según lo que haya configurado el stand que lo aplicó
            npc.defense = (int)(npc.defense * stats.multiplicadorDefensa);

            // Reduce la velocidad de movimiento sin romper la IA (mismo truco que Requiem)
            npc.position -= npc.velocity * stats.porcentajeReduccionVelocidad;
        }
    }
}