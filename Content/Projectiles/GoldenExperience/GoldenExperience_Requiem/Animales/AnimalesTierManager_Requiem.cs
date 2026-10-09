using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem.Animales
{
    public static class AnimalesTierManager_Requiem
    {
        // Cantidad por especie. Antes eran 3 ranas + 3 mariposas + 3 pájaros
        // "hardcodeado" dentro de InvocarAnimales; ahora es ajustable desde
        // fuera, igual que CantidadMariposas en MariposaTierManager_Tier_4.
        public static int CantidadPorEspecie = 3;

        static readonly SoundStyle InvocacionSound = SoundID.Item4 with { Volume = 0.7f };

        public static void SpawnAnimales(Player p)
        {
            // Esta función solo debe decidir/crear NPCs en Servidor o Singleplayer.
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            SoundEngine.PlaySound(InvocacionSound, p.Center);

            SpawnEspecie(p, ModContent.NPCType<PajaroRequiem>(), CantidadPorEspecie);
            SpawnEspecie(p, ModContent.NPCType<RanaRequiem>(), CantidadPorEspecie);
            SpawnEspecie(p, ModContent.NPCType<MariposaRequiem>(), CantidadPorEspecie);
        }

        static void SpawnEspecie(Player p, int tipoNPC, int cantidad)
        {
            for (int i = 0; i < cantidad; i++)
            {
                Vector2 spawnPos = p.Center + new Vector2(
                    Main.rand.NextFloat(-60f, 60f),
                    Main.rand.NextFloat(-40f, 10f)
                );

                int index = NPC.NewNPC(
                    p.GetSource_FromThis(),
                    (int)spawnPos.X,
                    (int)spawnPos.Y,
                    tipoNPC
                );

                if (index >= 0 && index < Main.maxNPCs)
                {
                    NPC npc = Main.npc[index];

                    if (npc.ModNPC is AnimalStandRequiem animal)
                    {
                        animal.OwnerIndex = p.whoAmI;
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