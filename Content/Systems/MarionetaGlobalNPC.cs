using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ID;
using System.Collections.Generic;
using System.IO;

namespace Jojo.Content.Systems
{
    public class MarionetaGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool tethered;
        public bool isPrimary;
        public int primaryWhoAmI = -1;
        public int tetherPlayer = -1;
        public int tetherTimer;

        public int standType = -1;
        public float maxChainDistance = 0f;

        Vector2 anchorPosition;
        Vector2 relativeOffsetToPrimary;

        // [CORRECCIÓN]: Ya no es una constante fija global.
        // Cada NPC atado guarda su propia duración, definida por la cadena que lo ató.
        public const int DefaultTetherDuration = 720; // 12s (fallback por si no se especifica)
        public int tetherDuration = DefaultTetherDuration;

        enum MarionetaPacketType : byte
        {
            Attach = 0,
            Release = 1,
            SyncAnchor = 2
        }

        static readonly HashSet<int> UntetherableTypes = new()
        {
            NPCID.TargetDummy,
        };

        public static bool IsTetherable(NPC npc) => !UntetherableTypes.Contains(npc.type);

        public override bool PreAI(NPC npc)
        {
            if (!tethered) return true;

            if (isPrimary)
            {
                bool isAuthority = Main.netMode == NetmodeID.SinglePlayer
                    || Main.netMode == NetmodeID.Server
                    || (Main.netMode == NetmodeID.MultiplayerClient && tetherPlayer == Main.myPlayer);

                if (isAuthority)
                {
                    Player player = (tetherPlayer >= 0 && tetherPlayer < Main.maxPlayers) ? Main.player[tetherPlayer] : null;
                    if (player == null || !player.active || player.dead)
                    {
                        Release(npc);
                        return true;
                    }

                    Projectile stand = FindPlayerStand(tetherPlayer);
                    if (stand == null)
                    {
                        Release(npc);
                        return true;
                    }

                    tetherTimer++;
                    // [CORRECCIÓN]: Ahora se compara contra la duración propia de este NPC (definida por su cadena)
                    if (tetherTimer >= tetherDuration)
                    {
                        Release(npc);
                        return true;
                    }

                    float dist = Vector2.Distance(stand.Center, anchorPosition);

                    if (dist > maxChainDistance)
                    {
                        Vector2 dirToAnchor = (anchorPosition - stand.Center) / dist;
                        Vector2 clampedAnchor = stand.Center + dirToAnchor * maxChainDistance;
                        Vector2 delta = clampedAnchor - anchorPosition;
                        MoveAnchor(npc, delta);
                        npc.netUpdate = true;
                    }
                }
                else
                {
                    Projectile stand = FindPlayerStand(tetherPlayer);
                    if (stand != null)
                    {
                        float dist = Vector2.Distance(stand.Center, anchorPosition);

                        if (dist > maxChainDistance)
                        {
                            Vector2 dirToAnchor = (anchorPosition - stand.Center) / dist;
                            Vector2 clampedAnchor = stand.Center + dirToAnchor * maxChainDistance;
                            Vector2 delta = clampedAnchor - anchorPosition;
                            MoveAnchor(npc, delta);
                        }
                    }
                }

                npc.velocity = Vector2.Zero;
                npc.Center = anchorPosition;
            }
            else
            {
                NPC primary = primaryWhoAmI >= 0 && primaryWhoAmI < Main.maxNPCs ? Main.npc[primaryWhoAmI] : null;
                bool primaryStillTethered = primary != null && primary.active
                    && primary.GetGlobalNPC<MarionetaGlobalNPC>().tethered;

                if (!primaryStillTethered)
                {
                    ReleaseSingle(npc);
                    return true;
                }

                npc.velocity = Vector2.Zero;
                npc.Center = primary.Center + relativeOffsetToPrimary;
            }

            return false;
        }

        // [CORRECCIÓN]: Attach ahora recibe la duración desde quien lo invoca (la cadena del tier correspondiente)
        public void Attach(NPC npc, Player player, int incomingStandType, float incomingMaxDistance, int incomingTetherDuration)
        {
            if (!IsTetherable(npc)) return;

            AttachInternal(npc, player, npc.Center, incomingStandType, incomingMaxDistance, incomingTetherDuration);

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                SendAttachPacket(npc.whoAmI, player.whoAmI, npc.Center, incomingStandType, incomingMaxDistance, incomingTetherDuration);
            }
        }

        public void AttachInternal(NPC npc, Player player, Vector2 anchor, int incomingStandType, float incomingMaxDistance, int incomingTetherDuration)
        {
            TetherSingle(npc, player, isPrimary: true, primaryIndex: npc.whoAmI, anchor, Vector2.Zero, incomingStandType, incomingMaxDistance, incomingTetherDuration);

            foreach (NPC segment in GetLinkedSegments(npc))
            {
                Vector2 offset = segment.Center - npc.Center;
                segment.GetGlobalNPC<MarionetaGlobalNPC>()
                    .TetherSingle(segment, player, isPrimary: false, primaryIndex: npc.whoAmI, segment.Center, offset, incomingStandType, incomingMaxDistance, incomingTetherDuration);
            }
        }

        public void Release(NPC npc)
        {
            if (!tethered) return;
            ReleaseInternal(npc);
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                SendReleasePacket(npc.whoAmI);
            }
        }

        public void ReleaseInternal(NPC npc)
        {
            ReleaseSingle(npc);
            foreach (NPC segment in GetLinkedSegments(npc))
            {
                segment.GetGlobalNPC<MarionetaGlobalNPC>().ReleaseSingle(segment);
            }
        }

        public void MoveAnchor(NPC npc, Vector2 delta)
        {
            anchorPosition += delta;

            if (!isPrimary) return;

            foreach (NPC segment in GetLinkedSegments(npc))
            {
                segment.GetGlobalNPC<MarionetaGlobalNPC>().anchorPosition += delta;
            }
        }

        public Vector2 GetAnchor() => anchorPosition;

        void TetherSingle(NPC npc, Player player, bool isPrimary, int primaryIndex, Vector2 anchor, Vector2 offset, int incomingStandType, float incomingMaxDistance, int incomingTetherDuration)
        {
            tethered = true;
            this.isPrimary = isPrimary;
            this.primaryWhoAmI = primaryIndex;
            this.tetherPlayer = player.whoAmI;
            this.tetherTimer = 0;
            this.anchorPosition = anchor;
            this.relativeOffsetToPrimary = offset;
            this.standType = incomingStandType;
            this.maxChainDistance = incomingMaxDistance;
            // [CORRECCIÓN]: Se guarda la duración propia recibida desde la cadena que ató este NPC
            this.tetherDuration = incomingTetherDuration;

            npc.velocity = Vector2.Zero;
            npc.Center = anchor;
            npc.netUpdate = true;
        }

        void ReleaseSingle(NPC npc)
        {
            tethered = false;
            isPrimary = false;
            primaryWhoAmI = -1;
            tetherPlayer = -1;
            tetherTimer = 0;
            standType = -1;
            maxChainDistance = 0f;
            relativeOffsetToPrimary = Vector2.Zero;
            tetherDuration = DefaultTetherDuration;
            npc.netUpdate = true;
        }

        Projectile FindPlayerStand(int playerIndex)
        {
            if (standType == -1) return null;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == playerIndex && proj.type == standType)
                    return proj;
            }
            return null;
        }

        IEnumerable<NPC> GetLinkedSegments(NPC npc)
        {
            int masterHeadIndex = npc.realLife >= 0 ? npc.realLife : npc.whoAmI;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                if (other == npc || !other.active) continue;

                int otherHeadIndex = other.realLife >= 0 ? other.realLife : other.whoAmI;

                if (otherHeadIndex == masterHeadIndex || other.realLife == npc.whoAmI || npc.realLife == other.realLife || other.ai[3] == npc.ai[3] || (npc.boss && other.boss && Vector2.Distance(npc.Center, other.Center) < 800f))
                {
                    yield return other;
                }
            }
        }

        public static void HandlePacket(BinaryReader reader, int sender)
        {
            MarionetaPacketType subType = (MarionetaPacketType)reader.ReadByte();
            int npcIndex = reader.ReadInt16();

            if (npcIndex < 0 || npcIndex >= Main.maxNPCs) return;
            NPC npc = Main.npc[npcIndex];

            switch (subType)
            {
                case MarionetaPacketType.Attach:
                    int playerIndex = reader.ReadInt16();
                    float startX = reader.ReadSingle();
                    float startY = reader.ReadSingle();
                    int sType = reader.ReadInt16();
                    float maxDist = reader.ReadSingle();
                    // [CORRECCIÓN]: Se lee la duración enviada por el cliente/servidor origen
                    int duration = reader.ReadInt32();

                    if (npc.active && playerIndex >= 0 && playerIndex < Main.maxPlayers)
                    {
                        Player player = Main.player[playerIndex];
                        var marioneta = npc.GetGlobalNPC<MarionetaGlobalNPC>();
                        marioneta.AttachInternal(npc, player, new Vector2(startX, startY), sType, maxDist, duration);

                        if (Main.netMode == NetmodeID.Server)
                        {
                            SendAttachPacket(npcIndex, playerIndex, new Vector2(startX, startY), sType, maxDist, duration, ignoreClient: sender);
                        }
                    }
                    break;

                case MarionetaPacketType.Release:
                    if (npc.active)
                    {
                        var marioneta = npc.GetGlobalNPC<MarionetaGlobalNPC>();
                        marioneta.ReleaseInternal(npc);

                        if (Main.netMode == NetmodeID.Server)
                        {
                            SendReleasePacket(npcIndex, ignoreClient: sender);
                        }
                    }
                    break;

                case MarionetaPacketType.SyncAnchor:
                    float ax = reader.ReadSingle();
                    float ay = reader.ReadSingle();
                    if (npc.active)
                    {
                        var marioneta = npc.GetGlobalNPC<MarionetaGlobalNPC>();
                        marioneta.anchorPosition = new Vector2(ax, ay);
                        npc.Center = marioneta.anchorPosition;

                        if (Main.netMode == NetmodeID.Server)
                        {
                            SendAnchorPacket(npcIndex, new Vector2(ax, ay), ignoreClient: sender);
                        }
                    }
                    break;
            }
        }

        // [CORRECCIÓN]: Ahora recibe y transmite también la duración del tether
        public static void SendAttachPacket(int npcIndex, int playerIndex, Vector2 anchor, int sType, float maxDist, int duration, int ignoreClient = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer) return;

            ModPacket packet = ModContent.GetInstance<Jojo>().GetPacket();
            packet.Write(Jojo.PacketType_MarionetaSync);
            packet.Write((byte)MarionetaPacketType.Attach);
            packet.Write((short)npcIndex);
            packet.Write((short)playerIndex);
            packet.Write(anchor.X);
            packet.Write(anchor.Y);
            packet.Write((short)sType);
            packet.Write(maxDist);
            packet.Write(duration);
            packet.Send(toClient: -1, ignoreClient: ignoreClient);
        }

        public static void SendReleasePacket(int npcIndex, int ignoreClient = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer) return;

            ModPacket packet = ModContent.GetInstance<Jojo>().GetPacket();
            packet.Write(Jojo.PacketType_MarionetaSync);
            packet.Write((byte)MarionetaPacketType.Release);
            packet.Write((short)npcIndex);
            packet.Send(toClient: -1, ignoreClient: ignoreClient);
        }

        public static void SendAnchorPacket(int npcIndex, Vector2 anchor, int ignoreClient = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer) return;

            ModPacket packet = ModContent.GetInstance<Jojo>().GetPacket();
            packet.Write(Jojo.PacketType_MarionetaSync);
            packet.Write((byte)MarionetaPacketType.SyncAnchor);
            packet.Write((short)npcIndex);
            packet.Write(anchor.X);
            packet.Write(anchor.Y);
            packet.Send(toClient: -1, ignoreClient: ignoreClient);
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(tethered);
            bitWriter.WriteBit(isPrimary);
            binaryWriter.Write((short)tetherPlayer);
            binaryWriter.Write(primaryWhoAmI);
            binaryWriter.Write(tetherTimer);

            binaryWriter.Write((short)standType);
            binaryWriter.Write(maxChainDistance);
            // [CORRECCIÓN]: Se sincroniza la duración propia de este NPC
            binaryWriter.Write(tetherDuration);

            binaryWriter.Write(anchorPosition.X);
            binaryWriter.Write(anchorPosition.Y);
            binaryWriter.Write(relativeOffsetToPrimary.X);
            binaryWriter.Write(relativeOffsetToPrimary.Y);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            tethered = bitReader.ReadBit();
            isPrimary = bitReader.ReadBit();
            tetherPlayer = binaryReader.ReadInt16();
            primaryWhoAmI = binaryReader.ReadInt32();
            tetherTimer = binaryReader.ReadInt32();

            standType = binaryReader.ReadInt16();
            maxChainDistance = binaryReader.ReadSingle();
            tetherDuration = binaryReader.ReadInt32();

            anchorPosition = new Vector2(binaryReader.ReadSingle(), binaryReader.ReadSingle());
            relativeOffsetToPrimary = new Vector2(binaryReader.ReadSingle(), binaryReader.ReadSingle());
        }
    }
}