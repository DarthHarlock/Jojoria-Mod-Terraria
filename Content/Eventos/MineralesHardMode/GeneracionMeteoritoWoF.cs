using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Localization;
using Jojo.Content.Items.Tiles; // Llama a tu tile de mineral

namespace Jojo.Content.Eventos.MineralesHardMode
{
    // 1. SISTEMA DE GUARDADO: Evita que se genere infinitas veces si farmeas al Muro de Carne
    public class MineralMeteoritoSystem : ModSystem
    {
        public static bool meteoritoGeneradoHM = false;

        public override void OnWorldLoad()
        {
            // Al cargar un mundo, reiniciamos la variable temporalmente hasta que lea el guardado
            meteoritoGeneradoHM = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            // Guardamos en el archivo del mundo que ya ocurrió el evento
            if (meteoritoGeneradoHM)
            {
                tag["meteoritoGeneradoHM"] = true;
            }
        }

        public override void LoadWorldData(TagCompound tag)
        {
            // Leemos si el mundo ya tiene la etiqueta guardada
            meteoritoGeneradoHM = tag.ContainsKey("meteoritoGeneradoHM");
        }
    }

    // 2. DETECTOR DEL MURO DE CARNE Y GENERACIÓN
    public class GeneracionMeteoritoWoF : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            // Si el NPC muerto es el Muro de Carne Y el mineral aún no se ha generado en este mundo
            if (npc.type == NPCID.WallofFlesh && !MineralMeteoritoSystem.meteoritoGeneradoHM)
            {
                GenerarMineralMeteorito();

                // Marcamos que ya se generó para que no vuelva a pasar en este mundo
                MineralMeteoritoSystem.meteoritoGeneradoHM = true;
            }
        }

        private void GenerarMineralMeteorito()
        {
            // Mensaje en el chat al morir el Muro de Carne
            string mensaje = "The underground has been enriched with Stand energy!";
            if (Language.ActiveCulture.Name.StartsWith("es"))
            {
                mensaje = "¡El subsuelo se ha enriquecido con energía Stand!";
            }
            else if (Language.ActiveCulture.Name.StartsWith("pt"))
            {
                mensaje = "O subsolo foi enriquecido com energia Stand!";
            }

            // Usamos el mismo color amarillento/dorado de tu mineral (#fadf7d)
            Color colorMineral = new Color(250, 223, 125);

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(mensaje, colorMineral);
            }
            else if (Main.netMode == NetmodeID.Server)
            {
                Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(mensaje), colorMineral);
            }

            // --- LÓGICA DE GENERACIÓN EN EL MUNDO ---
            int tipoMineral = ModContent.TileType<MineralMeteoritoJojoTile>();

            // Fórmula para determinar cuántas vetas habrá. 
            // 0.00015 es el multiplicador estándar equivalente al Tungsteno/Plata en Terraria.
            // Esto escala perfectamente dependiendo de si el mundo es Pequeño, Mediano o Grande.
            int cantidadDeVetas = (int)(Main.maxTilesX * Main.maxTilesY * 0.00015);

            for (int i = 0; i < cantidadDeVetas; i++)
            {
                // Coordenada X: En cualquier parte a lo ancho del mapa
                int x = WorldGen.genRand.Next(0, Main.maxTilesX);

                // Coordenada Y: Desde la capa de Cavernas (RockLayer) hasta justo antes del Inframundo (maxTilesY - 200)
                int y = WorldGen.genRand.Next((int)Main.rockLayer, Main.maxTilesY - 200);

                // WorldGen.OreRunner crea la veta de mineral.
                // Los números (4, 8) representan el tamaño de la veta (Strength) y la longitud de los pasos (Steps).
                WorldGen.OreRunner(x, y, WorldGen.genRand.Next(4, 8), WorldGen.genRand.Next(4, 8), (ushort)tipoMineral);
            }
        }
    }
}