using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Items; // Necesario para encontrar el DiarioDIO

// 📁 Cambio aplicado: El namespace ahora es Jojo.Content.Drops para coincidir con tu carpeta
namespace Jojo.Content.Drops
{
    public class DiarioDIOWorldGen : ModSystem
    {
        public override void PostWorldGen()
        {
            int diarioType = ModContent.ItemType<DiarioDIO>();
            if (diarioType <= 0) return;

            List<Chest> skywareChests = new List<Chest>();
            List<Chest> skyRegionChests = new List<Chest>();

            // Escanear todos los cofres que se han generado en el mundo
            for (int i = 0; i < Main.maxChests; i++)
            {
                Chest chest = Main.chest[i];
                if (chest == null) continue;

                Tile tile = Main.tile[chest.x, chest.y];
                if (!tile.HasTile) continue;

                // Si la baldosa es un cofre (TileID.Containers)
                if (tile.TileType == TileID.Containers)
                {
                    // frameX dividido entre 36 nos da el "estilo" del cofre.
                    int chestStyle = tile.TileFrameX / 36;

                    // El estilo 13 corresponde EXACTAMENTE al Skyware Chest (Cofre Celeste) de las Islas Flotantes
                    if (chestStyle == 13)
                    {
                        skywareChests.Add(chest);
                    }
                    // Si por algún casual de otro mod el cofre no es celeste pero está en el cielo, lo guardamos como respaldo
                    else if (chest.y < Main.worldSurface)
                    {
                        skyRegionChests.Add(chest);
                    }
                }
            }

            Chest selectedChest = null;

            // 1. Elegimos un cofre celeste al azar de las islas flotantes (Prioridad absoluta)
            if (skywareChests.Count > 0)
            {
                selectedChest = skywareChests[WorldGen.genRand.Next(skywareChests.Count)];
            }
            // 2. Respaldo: un cofre cualquiera en la zona del cielo
            else if (skyRegionChests.Count > 0)
            {
                selectedChest = skyRegionChests[WorldGen.genRand.Next(skyRegionChests.Count)];
            }
            // 3. Fallback absoluto para que NO falle la generación: el cofre con la 'Y' más pequeña (el más alto del mundo)
            else
            {
                int highestY = Main.maxTilesY;
                for (int i = 0; i < Main.maxChests; i++)
                {
                    Chest chest = Main.chest[i];
                    if (chest != null && chest.y < highestY)
                    {
                        highestY = chest.y;
                        selectedChest = chest;
                    }
                }
            }

            // Una vez que tenemos el cofre de la isla flotante, le inyectamos 1 Diario de DIO
            if (selectedChest != null)
            {
                for (int slot = 0; slot < 40; slot++)
                {
                    if (selectedChest.item[slot] == null || selectedChest.item[slot].IsAir || selectedChest.item[slot].type == ItemID.None)
                    {
                        selectedChest.item[slot] = new Item();
                        selectedChest.item[slot].SetDefaults(diarioType);
                        selectedChest.item[slot].stack = 1;
                        break; // Terminamos, ya lo ha metido en el primer hueco disponible
                    }
                }
            }
        }
    }
}