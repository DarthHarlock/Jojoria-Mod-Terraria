using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs
{
    public class BleedingCustom : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // Evita regeneración
            if (npc.lifeRegen > 0)
                npc.lifeRegen = 0;

            // 🔥 daño constante (esto SIEMPRE funciona)
            npc.lifeRegen -= 20; // ≈ 10 DPS

            // 🔥 fuerza que se vea el daño
            npc.netUpdate = true;
        }
    }
}