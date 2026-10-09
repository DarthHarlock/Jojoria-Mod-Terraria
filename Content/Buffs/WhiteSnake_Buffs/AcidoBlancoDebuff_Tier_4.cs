using System;
using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.WhiteSnake_Buffs
{
    public class AcidoBlancoDebuff_Tier_4 : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            var g = npc.GetGlobalNPC<AcidoBlancoNPC_Tier_4>();

            // Normal: usa las stats grabadas por quien aplicó el ácido.
            // Solo si no hay ninguna (buff puesto por otro camino) usa el respaldo.
            if (g.damageAcido <= 0 && AcidoBlancoNPC_Tier_4.ObtenerStats(out AcidoBlancoStats respaldo))
            {
                g.damageAcido = respaldo.Damage;
                g.lifeRegenAcido = respaldo.LifeRegen;
                g.slowAcido = respaldo.Slow;
            }

            g.tieneAcidoBlanco = true;

            float slow = Math.Clamp(g.slowAcido, 0f, 1f); // más de 1 haría retroceder al enemigo
            npc.position -= npc.velocity * slow;
        }
    }
}