using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Jojo.Content.NPCs.TownNPCs; // Necesario para referenciar a Jotaro

namespace Jojo
{
    public class JojoPlayer : ModPlayer
    {
        // Variable que define si ya le dieron el objeto
        public bool haRecibidoFlecha = false;

        // Guardar el dato en el mundo/personaje
        public override void SaveData(TagCompound tag)
        {
            tag["haRecibidoFlecha"] = haRecibidoFlecha;
        }

        // Cargar el dato al entrar al juego
        public override void LoadData(TagCompound tag)
        {
            haRecibidoFlecha = tag.GetBool("haRecibidoFlecha");
        }

        // --- SPAWN AUTOMÁTICO DE JOTARO JUNTO AL GUÍA ---
        public override void OnEnterWorld()
        {
            // Solo el servidor o el modo un jugador deciden el spawn (evita duplicados en multiplayer)
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int jotaroType = ModContent.NPCType<Jotaro>();

            // Si Jotaro ya existe en el mundo, no hacemos nada
            if (NPC.AnyNPCs(jotaroType))
                return;

            // Buscamos al Guía como referencia de posición
            int guiaIndex = NPC.FindFirstNPC(NPCID.Guide);

            int spawnX;
            int spawnY;

            if (guiaIndex >= 0)
            {
                NPC guia = Main.npc[guiaIndex];
                spawnX = (int)(guia.position.X / 16f) + 3; // un poco al lado del Guía
                spawnY = (int)(guia.position.Y / 16f);
            }
            else
            {
                // Si por lo que sea el Guía no existe aún, usamos el spawn del mundo
                spawnX = Main.spawnTileX;
                spawnY = Main.spawnTileY;
            }

            NPC.NewNPC(NPC.GetSource_NaturalSpawn(), spawnX * 16, spawnY * 16, jotaroType);
        }
    }
}