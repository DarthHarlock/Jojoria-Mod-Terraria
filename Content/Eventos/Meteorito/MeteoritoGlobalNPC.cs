using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Eventos.Meteorito
{
    public class MeteoritoGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            // Si el meteorito ya cayó una vez en este mundo, no hacemos nada
            if (MeteoritoSystem.meteoritoCaido) return;

            if (npc.type == NPCID.BrainofCthulhu)
            {
                MeteoritoCustomUtils.SpawnMeteoritoCustom();
            }

            if (npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail)
            {
                int segmentosVivos = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC otroNPC = Main.npc[i];
                    if (otroNPC.active && (otroNPC.type == NPCID.EaterofWorldsHead || otroNPC.type == NPCID.EaterofWorldsBody || otroNPC.type == NPCID.EaterofWorldsTail))
                    {
                        segmentosVivos++;
                    }
                }

                if (segmentosVivos <= 1)
                {
                    MeteoritoCustomUtils.SpawnMeteoritoCustom();
                }
            }
        }
    }
}