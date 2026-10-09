using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_3.Mariposas_Tier_3
{
    public static class MariposaTierManager_Tier_3
    {
        // =========================================================================
        // CONFIGURABLE: cantidad de mariposas que invoca la habilidad H.
        // Antes era "const int CantidadMariposas = 10". Ahora es un campo estático
        // normal, así se puede ajustar desde fuera (otro sistema, un config,
        // un item potenciador, etc.) sin tener que tocar este archivo.
        // =========================================================================
        public static int CantidadMariposas = 10;

        const float RadioSpawn = 28f;
        const float VelocidadSalida = 2.5f;

        static readonly SoundStyle MariposaSummonSound = SoundID.Item29 with { Volume = 0.5f, Pitch = 0.7f };

        public static void SpawnMariposas(Player p)
        {
            // Esta función solo debe decidir/crear NPCs en Servidor o Singleplayer.
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int tipoMariposa = ModContent.NPCType<Mariposa_Refleja_Tier_3>();

            // =========================================================================
            // FIX "cada vez hay menos mariposas al spammear la habilidad": antes, cada
            // uso de H simplemente AÑADÍA mariposas nuevas encima de las que ya
            // hubiera vivas de un casteo anterior (llegaban a vivir hasta 2 minutos).
            // Repetir la habilidad varias veces acumulaba decenas de mariposas en el
            // mundo, hasta agotar los slots de NPC disponibles (límite global,
            // compartido con el resto de enemigos/criaturas del mundo). A partir de
            // ahí, NPC.NewNPC ya no podía crear más -> cada casteo "spawneaba" menos
            // hasta llegar a 0. No era un fallo al invocarlas, es que ya no quedaba
            // hueco.
            //
            // Solución: antes de invocar el nuevo enjambre, eliminamos (con su
            // efecto normal de partículas doradas, vía Sacrificar()) cualquier
            // mariposa que el jugador ya tuviera viva de un uso anterior. Así el
            // jugador siempre tiene exactamente CantidadMariposas vivas tras cada
            // casteo, nunca más, y nunca se acumulan ni agotan slots.
            // =========================================================================
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && npc.type == tipoMariposa
                    && npc.ModNPC is Mariposa_Refleja_Tier_3 vieja
                    && vieja.OwnerIndex == p.whoAmI)
                {
                    vieja.Sacrificar();
                }
            }

            SoundEngine.PlaySound(MariposaSummonSound, p.Center);

            int cantidad = Math.Max(0, CantidadMariposas);

            for (int i = 0; i < cantidad; i++)
            {
                float angulo = MathHelper.TwoPi * i / cantidad + Main.rand.NextFloat(-0.15f, 0.15f);
                Vector2 dir = new Vector2((float)Math.Cos(angulo), (float)Math.Sin(angulo));
                Vector2 spawnPos = p.Center + dir * RadioSpawn;

                int index = NPC.NewNPC(
                    p.GetSource_FromThis(),
                    (int)spawnPos.X,
                    (int)spawnPos.Y,
                    tipoMariposa
                );

                if (index >= 0 && index < Main.maxNPCs)
                {
                    NPC npc = Main.npc[index];
                    npc.velocity = dir * VelocidadSalida;

                    // El vínculo con el dueño ya NO se manda por ai[3], se manda por SendExtraAI.
                    if (npc.ModNPC is Mariposa_Refleja_Tier_3 mariposa)
                    {
                        mariposa.OwnerIndex = p.whoAmI;
                    }

                    // Sincroniza inmediatamente el spawn completo (incluye ExtraAI) a todos los clientes.
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, index);
                    }
                }
            }
        }
    }
}