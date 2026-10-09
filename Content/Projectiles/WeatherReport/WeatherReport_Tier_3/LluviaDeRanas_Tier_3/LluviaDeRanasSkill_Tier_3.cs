using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Buffs.WeatherReport_Buffs;

namespace Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_3.LluviaDeRanas_Tier_3
{
    public class LluviaDeRanasSkill_Tier_3 : ModPlayer
    {
        // ------------------- CONFIG -------------------
        const int DuracionTicks = 600;
        const int CantidadOleadas = 40;
        const int RanasPorOleada = 4;

        const float RadioHorizontal = 1000f;
        const float AlturaMinSobreJugador = 300f;
        const float AlturaMaxSobreJugador = 500f;
        const float DistanciaMinimaCaida = 220f;
        const int RangoRevisionTiles = 60;

        public const float AlturaMinNubes = 300f;
        public const float AlturaMaxNubes = 500f;
        const int CantidadNubesCielo = 105;
        // -------------------------------------------------------------

        int timerRanas;

        public static void Start(Player p)
        {
            int buffType = ModContent.BuffType<DuracionLluvia>();
            if (p.HasBuff(buffType)) return;

            p.GetModPlayer<LluviaDeRanasSkill_Tier_3>().timerRanas = 0; // Reseteamos timer
            p.AddBuff(buffType, DuracionTicks);

            if (p.whoAmI == Main.myPlayer)
            {
                float espacioEntreNubes = 3000f / CantidadNubesCielo;

                for (int i = 0; i < CantidadNubesCielo; i++)
                {
                    float offsetX = -1500f + (i * espacioEntreNubes) + Main.rand.NextFloat(-15f, 15f);
                    float offsetY = Main.rand.NextFloat(AlturaMinNubes, AlturaMaxNubes);
                    float velX = Main.rand.NextFloat(0.1f, 0.45f) * (Main.rand.NextBool() ? 1 : -1);
                    int texturaRandom = Main.rand.Next(1, 5);

                    Projectile.NewProjectile(
                        p.GetSource_Misc("LluviaDeRanas_Tier_3"),
                        p.Center.X + offsetX,
                        p.Center.Y - offsetY,
                        velX, 0f,
                        ModContent.ProjectileType<NubeCielo_Tier_3>(),
                        0, 0f, p.whoAmI,
                        texturaRandom, offsetY
                    );
                }
            }
        }

        public override void PreUpdate()
        {
            // Solo el servidor o el Singleplayer lanzan ranas (evita desincronización)
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            int buffType = ModContent.BuffType<DuracionLluvia>();

            if (Player.HasBuff(buffType))
            {
                timerRanas++;
                int intervalo = Math.Max(1, DuracionTicks / CantidadOleadas); // Tiempo entre oleadas

                // Spawneamos ranas basándonos estrictamente en un contador de frames
                if (timerRanas >= intervalo)
                {
                    timerRanas = 0;
                    for (int i = 0; i < RanasPorOleada; i++)
                    {
                        SpawnRana();
                    }
                }
            }
            else
            {
                timerRanas = 0; // Resetear el timer si el jugador pierde el buff
            }
        }

        void SpawnRana()
        {
            if (!TryEncontrarPosicion(out Vector2 pos)) return;

            var fuente = Player.GetSource_Misc("LluviaDeRanas_Tier_3");
            int index = NPC.NewNPC(fuente, (int)pos.X, (int)pos.Y, ModContent.NPCType<RanaVenenosa_Tier_3>());

            if (index >= 0 && index < Main.maxNPCs)
            {
                NPC rana = Main.npc[index];
                rana.velocity = new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(2f, 5f));
            }

            for (int i = 0; i < 3; i++)
                Dust.NewDust(pos, 8, 8, DustID.Cloud, 0f, 1f, 100, default, 1f);
        }

        bool TryEncontrarPosicion(out Vector2 resultado)
        {
            float centroX = Player.Center.X;

            for (int intento = 0; intento < 8; intento++)
            {
                float x = centroX + Main.rand.NextFloat(-RadioHorizontal, RadioHorizontal);
                x = MathHelper.Clamp(x, 32f, Main.maxTilesX * 16f - 32f);

                float y = Player.Center.Y - Main.rand.NextFloat(AlturaMinSobreJugador, AlturaMaxSobreJugador);

                if (y < 400f) continue;

                float distanciaCaida = DistanciaHastaSuelo(x, y);

                if (distanciaCaida < 0f || distanciaCaida >= DistanciaMinimaCaida)
                {
                    resultado = new Vector2(x, y);
                    return true;
                }

                float yAjustada = y - (DistanciaMinimaCaida - distanciaCaida);
                if (yAjustada >= 400f)
                {
                    resultado = new Vector2(x, yAjustada);
                    return true;
                }
            }

            resultado = default;
            return false;
        }

        float DistanciaHastaSuelo(float x, float y)
        {
            int tileX = (int)(x / 16f);
            int tileYInicio = (int)(y / 16f);

            for (int i = 0; i < RangoRevisionTiles; i++)
            {
                int tileY = tileYInicio + i;
                if (!WorldGen.InWorld(tileX, tileY)) continue;

                Tile tile = Main.tile[tileX, tileY];
                if (tile != null && tile.HasTile && Main.tileSolid[tile.TileType])
                    return i * 16f;
            }
            return -1f;
        }
    }
}