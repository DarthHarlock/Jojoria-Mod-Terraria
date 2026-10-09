using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules; // <--- OBLIGATORIO PARA EL BESTIARIO
using Jojo.Content.Items;

namespace Jojo.Content.Drops
{
    public class Almas_Npc_Drop : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // Solo aplica a NPCs de pueblo (Guía, Mercader, Enfermera, etc.)
            if (npc.townNPC)
            {
                // ============================================
                // 🔧 REGLA DE DROPEO
                // El "1" significa probabilidad de 1 entre 1 (100%).
                // Si alguna vez quieres cambiarlo:
                // 2 = 50% (1 entre 2)
                // 4 = 25% (1 entre 4)
                // 100 = 1% (1 entre 100)
                // ============================================

                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Alma_Npc>(), 1));
            }
        }
    }
}