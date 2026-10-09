using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Jojo.Content.Items;
using Jojo.Content.Items.Tiles;

namespace Jojo.Content.Eventos.Meteorito
{
    public static class MeteoritoCustomUtils
    {
        public static bool SpawnMeteoritoCustom()
        {
            // Bloqueo de seguridad: Si ya cayó en este mundo, cancelamos inmediatamente
            if (MeteoritoSystem.meteoritoCaido) return false;

            bool spawneoExitoso = false;
            int targetX = 0;
            int targetY = 0;

            // --- INTENTO 1: BUSCAR EXCLUSIVAMENTE EN EL DESIERTO (1500 intentos) ---
            for (int i = 0; i < 1500; i++)
            {
                // Evitar los extremos del mapa (playas del océano) y la zona de spawn
                int tempX = WorldGen.genRand.Next(250, Main.maxTilesX - 250);
                if (Math.Abs(tempX - Main.spawnTileX) < 200) continue;

                int tempY = 0;
                for (int j = 50; j < Main.maxTilesY; j++)
                {
                    if (Main.tile[tempX, j].HasTile && Main.tileSolid[Main.tile[tempX, j].TileType])
                    {
                        tempY = j;
                        break;
                    }
                }

                if (tempY == 0) continue;

                Tile topTile = Main.tile[tempX, tempY];

                // Validar que el bloque superficial sea arena o derivados del desierto
                bool esDesierto = topTile.TileType == TileID.Sand ||
                                  topTile.TileType == TileID.HardenedSand ||
                                  topTile.TileType == TileID.Sandstone ||
                                  topTile.TileType == TileID.FossilOre;

                if (!esDesierto) continue;
                if (Main.wallDungeon[topTile.WallType] || topTile.TileType == TileID.LihzahrdBrick) continue;

                targetX = tempX;
                targetY = tempY;
                spawneoExitoso = true;
                break;
            }

            // --- INTENTO 2: FALLBACK (Si no hay desierto, caer en cualquier otro lado) ---
            if (!spawneoExitoso)
            {
                for (int i = 0; i < 1000; i++)
                {
                    targetX = WorldGen.genRand.Next(100, Main.maxTilesX - 100);
                    if (Math.Abs(targetX - Main.spawnTileX) < 200) continue;

                    for (int j = 50; j < Main.maxTilesY; j++)
                    {
                        if (Main.tile[targetX, j].HasTile && Main.tileSolid[Main.tile[targetX, j].TileType])
                        {
                            targetY = j;
                            break;
                        }
                    }

                    if (Main.wallDungeon[Main.tile[targetX, targetY].WallType] || Main.tile[targetX, targetY].TileType == TileID.LihzahrdBrick)
                    {
                        continue;
                    }

                    spawneoExitoso = true;
                    break;
                }
            }

            if (!spawneoExitoso) return false;

            // Variables de tamaño vanilla
            int radioBase = WorldGen.genRand.Next(20, 28);
            int miMineralID = ModContent.TileType<MineralMeteoritoJojoTile>();

            // --- FASE 1: LIMPIEZA RADICAL DE ÁRBOLES Y VEGETACIÓN ---
            for (int x = targetX - radioBase - 6; x <= targetX + radioBase + 6; x++)
            {
                for (int y = targetY - radioBase - 8; y <= targetY + radioBase + 6; y++)
                {
                    if (x > 0 && x < Main.maxTilesX && y > 0 && y < Main.maxTilesY)
                    {
                        Tile tile = Main.tile[x, y];
                        if (tile.HasTile && (!Main.tileSolid[tile.TileType] || TileID.Sets.IsATreeTrunk[tile.TileType] || tile.TileType == TileID.Trees || tile.TileType == TileID.PalmTree))
                        {
                            WorldGen.KillTile(x, y, noItem: true);
                        }
                    }
                }
            }

            // --- FASE 2: GENERACIÓN CON RAÍCES Y BLOQUES SUELTOS ---
            for (int x = targetX - radioBase - 5; x <= targetX + radioBase + 5; x++)
            {
                for (int y = targetY - radioBase - 5; y <= targetY + radioBase + 5; y++)
                {
                    if (x > 0 && x < Main.maxTilesX && y > 0 && y < Main.maxTilesY)
                    {
                        float diffX = x - targetX;
                        float diffY = y - targetY;
                        float distanciaReal = Vector2.Distance(Vector2.Zero, new Vector2(diffX, diffY));

                        double angulo = Math.Atan2(diffY, diffX);
                        float ondasDeRaiz = (float)(Math.Sin(angulo * 6) * 3.5f + Math.Cos(angulo * 3) * 2.0f);

                        float radioModificado = radioBase + ondasDeRaiz;
                        float radioCráterHueco = radioBase * 0.45f + (ondasDeRaiz * 0.3f);

                        if (distanciaReal < radioModificado)
                        {
                            Tile tile = Main.tile[x, y];

                            if (tile.HasTile && (TileID.Sets.BasicChest[tile.TileType] || tile.TileType == TileID.LihzahrdBrick))
                            {
                                continue;
                            }

                            if (distanciaReal < radioCráterHueco)
                            {
                                if (WorldGen.genRand.NextFloat() < 0.12f && tile.HasTile && Main.tileSolid[tile.TileType])
                                {
                                    tile.TileType = (ushort)miMineralID;
                                    WorldGen.SquareTileFrame(x, y, true);
                                }
                                else
                                {
                                    tile.ClearTile();
                                }
                            }
                            else
                            {
                                float factorDistancia = (distanciaReal - radioCráterHueco) / (radioModificado - radioCráterHueco);
                                float probabilidadMineral = 1f - factorDistancia;

                                if (WorldGen.genRand.NextFloat() < (probabilidadMineral * 0.85f))
                                {
                                    if (tile.HasTile && Main.tileSolid[tile.TileType])
                                    {
                                        tile.TileType = (ushort)miMineralID;
                                        WorldGen.SquareTileFrame(x, y, true);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // --- FASE 3: TRADUCCIÓN Y CHAT AMARILLO ---
            string mensaje = "A meteorite imbued with Stand energy has fallen from the sky!";
            if (Language.ActiveCulture.Name.StartsWith("es"))
            {
                mensaje = "¡Un meteorito impregnado de energía Stand ha caído del cielo!";
            }
            else if (Language.ActiveCulture.Name.StartsWith("pt"))
            {
                mensaje = "Um meteorito impregnado com energia Stand caiu do céu!";
            }

            Color colorAmarillo = Color.Yellow;

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(mensaje, colorAmarillo);
            }
            else if (Main.netMode == NetmodeID.Server)
            {
                Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(mensaje), colorAmarillo);
            }

            // --- FASE 4: REGISTRO DE EVENTO ÚNICO Y SINCRONIZACIÓN ---
            MeteoritoSystem.meteoritoCaido = true;

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendTileSquare(-1, targetX - radioBase - 5, targetY - radioBase - 5, (radioBase * 2) + 10);
                NetMessage.SendData(MessageID.WorldData); // Sincroniza la variable meteoritoCaido con los clientes
            }

            return true;
        }
    }
}