using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using Jojo.Content.Buffs;
using Jojo.Content.Systems;

namespace Jojo.Content.Habilidades
{
    public static class TimeStop_TW_NPC
    {
        public const int Duration = 120;

        private const string StartSound = "Jojo/Content/Sonidos/Start_Timestop";
        private const string EndSound = "Jojo/Content/Sonidos/End_Timestop";

        public static void Use(NPC ownerNpc, Projectile standProj)
        {
            if (TimeStopSystem.timeStopped)
                return;

            SoundEngine.PlaySound(new SoundStyle(StartSound), ownerNpc.Center);

            TimeStopSystem.endSoundPath = EndSound;
            TimeStopSystem.timeStopped = true;
            TimeStopSystem.owner = -1;
            TimeStopSystem.ownerNPC = ownerNpc != null ? ownerNpc.whoAmI : -1;
            TimeStopSystem.ownerProj = standProj != null ? standProj.whoAmI : -1;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int p = 0; p < Main.maxPlayers; p++)
                {
                    Player player = Main.player[p];
                    if (player.active && !player.dead)
                    {
                        player.AddBuff(ModContent.BuffType<TimeStoped>(), Duration);
                    }
                }
            }

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = ModContent.GetInstance<Jojo>().GetPacket();
                packet.Write(Jojo.PacketType_TimeStopSync);
                packet.Write(true);
                packet.Write(-1); // Jugador (-1)
                packet.Write(TimeStopSystem.ownerNPC); // Enviar ID del NPC
                packet.Write(TimeStopSystem.ownerProj); // Enviar ID del Stand
                packet.Write(EndSound);
                packet.Send();
            }
        }

        public static void Stop()
        {
            if (!TimeStopSystem.timeStopped) return;

            if (!string.IsNullOrEmpty(TimeStopSystem.endSoundPath))
            {
                SoundEngine.PlaySound(new SoundStyle(TimeStopSystem.endSoundPath));
            }

            TimeStopSystem.ResetTimeStop();

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = ModContent.GetInstance<Jojo>().GetPacket();
                packet.Write(Jojo.PacketType_TimeStopSync);
                packet.Write(false);
                packet.Write(-1);
                packet.Write(-1);
                packet.Write(-1);
                packet.Write("");
                packet.Send();
            }
        }
    }
}