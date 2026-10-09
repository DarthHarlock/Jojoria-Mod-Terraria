using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.GoldenExperienceRequiem_Buffs;
using Jojo.Content.NPCs.StandsNpc.StarPlatinum;
using Jojo.Content.NPCs.StandsNpc.TheWorld;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_4.ControlMarioneta_Tier_4;
using Jojo.Content.Systems;
using Jojo.Content.UI;
using Jojo.Systems;
using Jojo.Content.NPCs.StandsNpc;

namespace Jojo
{
    public class Jojo : Mod
    {
        public const string ShaderName = "Jojo:TimeStop";
        public const string RedShaderName = "Jojo:RedOverlay";
        public const string BlueShaderName = "Jojo:BlueOverlay";
        public const string GreenShaderName = "Jojo:GreenOverlay";

        // IDs en uso: 1 (SkinSync), 10-13 y 20 (TimeErase), 14-17, 199, 200
        public const byte PacketType_ShockwavePush = 14;
        public const byte PacketType_ZeroGravityPush = 15;
        public const byte PacketType_CMoonNetHandler = 16;
        public const byte PacketType_MarionetaSync = 17;
        public const byte PacketType_TimeStopSync = 199;
        public const byte PacketType_GiveStandToNPC = 200;

        public static int CustomBiomeBgIndex = -1;

        public override void Load()
        {
            if (Main.dedServ) return;

            CustomBiomeBgIndex = BackgroundTextureLoader.GetBackgroundSlot(
                "Jojo/Content/Backgrounds/CustomBiomeBg"
            );

            Filters.Scene[ShaderName] = new Filter(
                new ScreenShaderData("FilterMiniTower")
                    .UseColor(0.7f, 0.7f, 0.7f)
                    .UseOpacity(1.2f),
                EffectPriority.Medium
            );

            Filters.Scene[RedShaderName] = new Filter(
                new ScreenShaderData("FilterMiniTower")
                    .UseColor(0.506f, 0.133f, 0.200f)
                    .UseOpacity(0.5f),
                EffectPriority.Medium
            );

            Filters.Scene[BlueShaderName] = new Filter(
                new ScreenShaderData("FilterMiniTower")
                    .UseColor(0.15f, 0.35f, 0.9f)
                    .UseOpacity(0.5f),
                EffectPriority.Medium
            );

            Filters.Scene[GreenShaderName] = new Filter(
                new ScreenShaderData("FilterMiniTower")
                    .UseColor(0.25f, 0.85f, 0.35f)
                    .UseOpacity(0.5f),
                EffectPriority.Medium
            );
        }

        public override void Unload()
        {
            if (!Main.dedServ)
            {
                if (Filters.Scene[ShaderName]?.IsActive() == true) Filters.Scene[ShaderName].Deactivate();
                if (Filters.Scene[RedShaderName]?.IsActive() == true) Filters.Scene[RedShaderName].Deactivate();
                if (Filters.Scene[BlueShaderName]?.IsActive() == true) Filters.Scene[BlueShaderName].Deactivate();
                if (Filters.Scene[GreenShaderName]?.IsActive() == true) Filters.Scene[GreenShaderName].Deactivate();
            }
        }

        public static void ActivarShaderVerde()
        {
            if (Main.dedServ) return;
            if (!Filters.Scene[GreenShaderName].IsActive())
                Filters.Scene.Activate(GreenShaderName);
        }

        public static void DesactivarShaderVerde()
        {
            if (Main.dedServ) return;
            if (Filters.Scene[GreenShaderName].IsActive())
                Filters.Scene[GreenShaderName].Deactivate();
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            byte packetType = reader.ReadByte();

            if (packetType == PacketType_GiveStandToNPC)
            {
                int npcIndex = reader.ReadInt32();

                if (Main.netMode == NetmodeID.Server && npcIndex >= 0 && npcIndex < Main.maxNPCs)
                {
                    NPC target = Main.npc[npcIndex];

                    if (target.active && GlobalStandSpawner.CanHaveStand(target))
                    {
                        GlobalStandSpawner infoNPC = target.GetGlobalNPC<GlobalStandSpawner>();
                        NPC miembroConStand = GlobalStandSpawner.FindLivingGroupMemberWithStand(target);

                        if (!infoNPC.hasStand && miembroConStand == null)
                        {
                            infoNPC.initializedStand = true;
                            infoNPC.hasStand = true;
                            infoNPC.standType = GlobalStandSpawner.GetRandomStandType();
                            infoNPC.standRespawnCooldown = 0;
                            infoNPC.forceSpawn = true;

                            target.netUpdate = true;
                        }
                    }
                }
                return;
            }

            if (packetType == PacketType_MarionetaSync)
            {
                MarionetaGlobalNPC.HandlePacket(reader, whoAmI);
                return;
            }

            // CAMBIO: 14 y 15 unificados. Siempre leen int + float + float.
            // Se leen SIEMPRE antes de validar, para no desalinear el stream.
            if (packetType == PacketType_ShockwavePush || packetType == PacketType_ZeroGravityPush)
            {
                int npcIndex = reader.ReadInt32();
                float velX = reader.ReadSingle();
                float velY = reader.ReadSingle();

                if (!float.IsFinite(velX) || !float.IsFinite(velY)) return;
                if (npcIndex < 0 || npcIndex >= Main.maxNPCs) return;

                velX = MathHelper.Clamp(velX, -30f, 30f);
                velY = MathHelper.Clamp(velY, -30f, 30f);

                NPC target = Main.npc[npcIndex];
                if (target.active)
                {
                    target.velocity += new Vector2(velX, velY);

                    if (Main.netMode == NetmodeID.Server)
                    {
                        target.netUpdate = true;
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, target.whoAmI);
                    }
                }
                return;
            }

            if (packetType == PacketType_CMoonNetHandler)
            {
                CMoonNetHandler.HandlePacket(reader, whoAmI);
                return;
            }

            if (packetType == PacketType_TimeStopSync)
            {
                bool active = reader.ReadBoolean();
                int ownerPlayer = reader.ReadInt32();
                int ownerNPC = reader.ReadInt32();
                int ownerProj = reader.ReadInt32();
                string endSound = reader.ReadString();

                TimeStopSystem.timeStopped = active;
                TimeStopSystem.owner = ownerPlayer;
                TimeStopSystem.ownerNPC = ownerNPC;
                TimeStopSystem.ownerProj = ownerProj;
                TimeStopSystem.endSoundPath = endSound;

                if (active && ownerPlayer >= 0 && ownerPlayer < Main.maxPlayers)
                    Main.player[ownerPlayer].AddBuff(ModContent.BuffType<TimeStoped>(), 60 * 10);

                if (Main.netMode == NetmodeID.Server)
                {
                    ModPacket packet = GetPacket();
                    packet.Write(PacketType_TimeStopSync);
                    packet.Write(active);
                    packet.Write(ownerPlayer);
                    packet.Write(ownerNPC);
                    packet.Write(ownerProj);
                    packet.Write(endSound);
                    packet.Send(-1, whoAmI);
                }
                return;
            }

            if (packetType == StarPlatinumNpcProj.StandClashPacketId)
            {
                StarPlatinumNpcProj.HandleClashPacket(reader);
                return;
            }
            if (packetType == TheWorldNpcProj.StandClashPacketId)
            {
                TheWorldNpcProj.HandleClashPacket(reader);
                return;
            }

            if (TimeEraseNetHandler.HandlePacket(packetType, reader, whoAmI))
                return;

            StandSlotSystem.HandlePacketAfterType(packetType, reader, whoAmI);
        }
    }
}