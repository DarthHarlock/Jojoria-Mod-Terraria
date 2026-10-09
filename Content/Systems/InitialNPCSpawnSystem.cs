using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Jojo.Content.NPCs.TownNPCs;

namespace Jojo.Content.Systems
{
    public class InitialNPCSpawnSystem : ModSystem
    {
        public override void PostWorldGen()
        {
            int spawnX = Main.spawnTileX * 16;
            int spawnY = Main.spawnTileY * 16;

            // Añade aquí cualquier NPC futuro que deba aparecer al generar el mundo.
            int[] startingNPCs = new int[]
            {
                ModContent.NPCType<Jotaro>(),
                // ModContent.NPCType<Dio>(),
                // ModContent.NPCType<Josuke>()
            };

            foreach (int npcType in startingNPCs)
            {
                int npcIndex = NPC.NewNPC(
                    new EntitySource_WorldGen(),
                    spawnX,
                    spawnY,
                    npcType
                );

                if (npcIndex >= 0 && npcIndex < Main.maxNPCs)
                {
                    Main.npc[npcIndex].homeless = true;
                    Main.npc[npcIndex].direction = 1;
                }
            }
        }
    }
}