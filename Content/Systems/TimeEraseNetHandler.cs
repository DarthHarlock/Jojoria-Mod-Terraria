using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_2;
using Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_3;
using Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_4;

namespace Jojo.Content.Systems
{
    public interface IKingCrimsonTimeTracker
    {
        bool isRecording { get; }
        bool isReplaying { get; }
        bool HasRecordedData { get; }
        bool IsTimeEraseLockActive { get; }
        void IniciarGrabacion(int playerWhoAmI = 0);
        void IniciarRebobinado(NPC npc);
        void DetenerHabilidad(NPC npc);
        Vector2 GetInitialPosition();
        void ForceInitialPosition(Vector2 pos);
    }

    public static class TimeEraseNetHandler
    {
        public const byte PacketType_TimeEraseStart = 10;
        public const byte PacketType_TimeEraseEnd = 11;
        public const byte PacketType_TimeEraseRewind = 12;
        public const byte PacketType_TimeEraseLock = 13;
        public const byte PacketType_TimeEraseSyncReq = 20; // CAMBIO: antes 14 (chocaba con ShockwavePush)

        public static bool timeEraseLocked = false;
        public static bool timeEraseActive = false;
        public static int timeEraseOwner = -1;
        public static int activeTier = 4;
        private static int activeReplayCount = 0;

        public static IKingCrimsonTimeTracker GetTrackerByTier(NPC npc, int tier)
        {
            switch (tier)
            {
                case 2:
                    return npc.GetGlobalNPC<KingCrimsonTimeTracker_Tier_2>();
                case 3:
                    return npc.GetGlobalNPC<KingCrimsonTimeTracker_Tier_3>();
                case 4:
                default:
                    return npc.GetGlobalNPC<KingCrimsonTimeTracker_Tier_4>();
            }
        }

        public static IKingCrimsonTimeTracker GetActiveTracker(NPC npc)
        {
            return GetTrackerByTier(npc, activeTier);
        }

        public static bool IsTimeManipulationActive()
        {
            if (timeEraseActive || timeEraseLocked)
                return true;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active) continue;

                var tracker2 = npc.GetGlobalNPC<KingCrimsonTimeTracker_Tier_2>();
                if (tracker2 != null && tracker2.IsTimeEraseLockActive)
                    return true;

                var tracker3 = npc.GetGlobalNPC<KingCrimsonTimeTracker_Tier_3>();
                if (tracker3 != null && tracker3.IsTimeEraseLockActive)
                    return true;

                var tracker4 = npc.GetGlobalNPC<KingCrimsonTimeTracker_Tier_4>();
                if (tracker4 != null && tracker4.IsTimeEraseLockActive)
                    return true;
            }

            return false;
        }

        public static void SendTimeEraseStart(int playerWhoAmI, int tier = 4)
        {
            activeTier = tier;
            StartRecordingAllNPCs(playerWhoAmI);
            timeEraseLocked = true;
            timeEraseActive = true;
            timeEraseOwner = playerWhoAmI;

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                activeReplayCount = 0;
                return;
            }

            ModPacket packet = ModContent.GetInstance<Jojo>().GetPacket();
            packet.Write(PacketType_TimeEraseStart);
            packet.Write(playerWhoAmI);
            packet.Write(tier);
            packet.Send();
        }

        public static void SendTimeEraseEnd(int playerWhoAmI)
        {
            timeEraseActive = false;
            timeEraseOwner = -1;

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                RewindAllNPCs(playerWhoAmI);
                return;
            }

            ModPacket packet = ModContent.GetInstance<Jojo>().GetPacket();
            packet.Write(PacketType_TimeEraseEnd);
            packet.Write(playerWhoAmI);
            packet.Send();
        }

        public static void RequestStateSync()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient) return;

            ModPacket packet = ModContent.GetInstance<Jojo>().GetPacket();
            packet.Write(PacketType_TimeEraseSyncReq);
            packet.Write(Main.myPlayer);
            packet.Send();
        }

        public static void TryRegisterNewNPC(NPC npc)
        {
            if (!timeEraseActive) return;
            if (!npc.active || npc.friendly || npc.dontTakeDamage) return;

            IKingCrimsonTimeTracker tracker = GetActiveTracker(npc);
            if (tracker != null && !tracker.isRecording && !tracker.isReplaying && !tracker.HasRecordedData)
                tracker.IniciarGrabacion(timeEraseOwner >= 0 ? timeEraseOwner : 0);
        }

        public static void NotifyReplayFinished()
        {
            activeReplayCount--;
            if (activeReplayCount <= 0)
            {
                activeReplayCount = 0;
                SetLock(false);
            }
        }

        public static bool HandlePacket(byte packetType, BinaryReader reader, int whoAmI)
        {
            switch (packetType)
            {
                case PacketType_TimeEraseStart: return HandleStart(reader, whoAmI);
                case PacketType_TimeEraseEnd: return HandleEnd(reader, whoAmI);
                case PacketType_TimeEraseRewind: return HandleRewind(reader, whoAmI);
                case PacketType_TimeEraseLock: return HandleLock(reader, whoAmI);
                case PacketType_TimeEraseSyncReq: return HandleSyncReq(reader, whoAmI);
                default: return false;
            }
        }

        private static bool HandleStart(BinaryReader reader, int whoAmI)
        {
            int playerWhoAmI = reader.ReadInt32();
            int tier = reader.ReadInt32();

            activeTier = tier;
            timeEraseLocked = true;
            timeEraseActive = true;
            timeEraseOwner = playerWhoAmI;
            activeReplayCount = 0;
            StartRecordingAllNPCs(playerWhoAmI);

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket pkt = ModContent.GetInstance<Jojo>().GetPacket();
                pkt.Write(PacketType_TimeEraseStart);
                pkt.Write(playerWhoAmI);
                pkt.Write(tier);
                pkt.Send(-1, whoAmI);

                BroadcastLock(true);
            }
            return true;
        }

        private static bool HandleEnd(BinaryReader reader, int whoAmI)
        {
            int playerWhoAmI = reader.ReadInt32();
            timeEraseActive = false;
            timeEraseOwner = -1;

            if (Main.netMode == NetmodeID.Server)
                RewindAllNPCsAndBroadcast(playerWhoAmI, whoAmI);
            else
                RewindAllNPCs(playerWhoAmI);

            return true;
        }

        private static bool HandleRewind(BinaryReader reader, int whoAmI)
        {
            int npcIndex = reader.ReadInt32();
            int playerWhoAmI = reader.ReadInt32();
            float posX = reader.ReadSingle();
            float posY = reader.ReadSingle();
            int tier = reader.ReadInt32();

            if (npcIndex < 0 || npcIndex >= Main.maxNPCs) return true;

            NPC npc = Main.npc[npcIndex];
            if (!npc.active || npc.friendly) return true;

            IKingCrimsonTimeTracker tracker = GetTrackerByTier(npc, tier);
            if (tracker != null)
            {
                tracker.ForceInitialPosition(new Vector2(posX, posY));
                tracker.IniciarRebobinado(npc);
            }

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket pkt = ModContent.GetInstance<Jojo>().GetPacket();
                pkt.Write(PacketType_TimeEraseRewind);
                pkt.Write(npcIndex);
                pkt.Write(playerWhoAmI);
                pkt.Write(posX);
                pkt.Write(posY);
                pkt.Write(tier);
                pkt.Send(-1, whoAmI);
            }
            return true;
        }

        private static bool HandleLock(BinaryReader reader, int whoAmI)
        {
            bool locked = reader.ReadBoolean();
            timeEraseLocked = locked;

            if (!locked)
            {
                timeEraseActive = false;
                timeEraseOwner = -1;
            }

            if (Main.netMode == NetmodeID.Server)
                BroadcastLock(locked, whoAmI);

            return true;
        }

        private static bool HandleSyncReq(BinaryReader reader, int whoAmI)
        {
            int requestingClient = reader.ReadInt32();

            if (Main.netMode != NetmodeID.Server) return true;

            ModPacket pkt = ModContent.GetInstance<Jojo>().GetPacket();
            pkt.Write(PacketType_TimeEraseLock);
            pkt.Write(timeEraseLocked);
            pkt.Send(whoAmI);

            if (timeEraseActive)
            {
                ModPacket pkt2 = ModContent.GetInstance<Jojo>().GetPacket();
                pkt2.Write(PacketType_TimeEraseStart);
                pkt2.Write(timeEraseOwner);
                pkt2.Write(activeTier);
                pkt2.Send(whoAmI);
            }

            return true;
        }

        private static void StartRecordingAllNPCs(int playerWhoAmI)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && !npc.dontTakeDamage)
                    GetActiveTracker(npc)?.IniciarGrabacion(playerWhoAmI);
            }
        }

        private static void RewindAllNPCs(int playerWhoAmI)
        {
            int count = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly) continue;

                IKingCrimsonTimeTracker tracker = GetActiveTracker(npc);
                if (tracker != null && (tracker.isRecording || tracker.HasRecordedData))
                {
                    tracker.IniciarRebobinado(npc);
                    count++;
                }
            }

            if (count == 0)
                SetLock(false);
            else
                activeReplayCount = count;
        }

        private static void RewindAllNPCsAndBroadcast(int playerWhoAmI, int excludeWho)
        {
            int count = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly) continue;

                IKingCrimsonTimeTracker tracker = GetActiveTracker(npc);
                if (tracker != null && (tracker.isRecording || tracker.HasRecordedData))
                {
                    Vector2 initPos = tracker.GetInitialPosition();
                    tracker.IniciarRebobinado(npc);
                    count++;

                    ModPacket pkt = ModContent.GetInstance<Jojo>().GetPacket();
                    pkt.Write(PacketType_TimeEraseRewind);
                    pkt.Write(i);
                    pkt.Write(playerWhoAmI);
                    pkt.Write(initPos.X);
                    pkt.Write(initPos.Y);
                    pkt.Write(activeTier);
                    pkt.Send(-1, -1);
                }
            }

            if (count == 0)
                SetLock(false);
            else
                activeReplayCount = count;
        }

        private static void SetLock(bool locked)
        {
            timeEraseLocked = locked;
            if (!locked)
            {
                timeEraseActive = false;
                timeEraseOwner = -1;
            }
            if (Main.netMode == NetmodeID.Server)
                BroadcastLock(locked);
        }

        private static void BroadcastLock(bool locked, int excludeWhoAmI = -1)
        {
            ModPacket pkt = ModContent.GetInstance<Jojo>().GetPacket();
            pkt.Write(PacketType_TimeEraseLock);
            pkt.Write(locked);
            pkt.Send(-1, excludeWhoAmI);
        }
    }

    public class KingCrimsonTimeEraseSystem : ModSystem
    {
        public static int FailSafeTimer = 0;
        public const int MAX_ABILITY_TIME = 1800;

        public override void OnWorldLoad()
        {
            ResetGlobalState();
        }

        public override void OnWorldUnload()
        {
            ResetGlobalState();
        }

        private void ResetGlobalState()
        {
            TimeEraseNetHandler.timeEraseLocked = false;
            TimeEraseNetHandler.timeEraseActive = false;
            TimeEraseNetHandler.timeEraseOwner = -1;
            FailSafeTimer = 0;
        }

        public override void PostUpdateNPCs()
        {
            if (TimeEraseNetHandler.timeEraseActive || TimeEraseNetHandler.timeEraseLocked)
            {
                FailSafeTimer++;

                if (FailSafeTimer >= MAX_ABILITY_TIME)
                {
                    ResetGlobalState();

                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (npc.active)
                        {
                            var tracker = TimeEraseNetHandler.GetActiveTracker(npc);
                            tracker?.DetenerHabilidad(npc);
                        }
                    }
                }
            }
            else
            {
                FailSafeTimer = 0;
            }
        }
    }

    public class KingCrimsonTimeTrackerPlayerHook : ModPlayer
    {
        public override void OnEnterWorld()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                TimeEraseNetHandler.RequestStateSync();
        }
    }
}