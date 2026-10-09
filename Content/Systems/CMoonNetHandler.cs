using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Systems
{
    public static class CMoonNetHandler
    {
        public enum MessageType : byte
        {
            SetNPCVelocity,
            AddNPCVelocity
        }

        public static void SendNPCVelocity(int npcIndex, Vector2 velocity, bool overrideVelocity = false)
        {
            if (Main.netMode == NetmodeID.SinglePlayer) return;

            // Conseguimos el paquete del Mod
            ModPacket packet = ModContent.GetInstance<Jojo>().GetPacket();

            // 1. ESCRIBIMOS EL ID DEL SISTEMA PARA QUE JOJO.CS NO LO IGNORE (VITAL)
            packet.Write(Jojo.PacketType_CMoonNetHandler);

            // 2. ESCRIBIMOS EL TIPO DE MENSAJE INTERNO
            packet.Write((byte)(overrideVelocity ? MessageType.SetNPCVelocity : MessageType.AddNPCVelocity));

            // 3. ESCRIBIMOS LOS DATOS RELEVANTES CON TIPOS ESTRICTOS (int y float)
            packet.Write((int)npcIndex);
            packet.Write((float)velocity.X);
            packet.Write((float)velocity.Y);

            // Si somos el cliente, enviamos al servidor para que él aplique la velocidad y la sincronice al resto
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                packet.Send();
            }
        }

        public static void HandlePacket(BinaryReader reader, int sender)
        {
            // Leemos los datos exactamente en el mismo orden que los escribimos
            MessageType msgType = (MessageType)reader.ReadByte();
            int npcIndex = reader.ReadInt32();
            float velX = reader.ReadSingle();
            float velY = reader.ReadSingle();

            if (npcIndex >= 0 && npcIndex < Main.maxNPCs)
            {
                NPC npc = Main.npc[npcIndex];
                if (npc.active)
                {
                    // Aplicamos la velocidad según lo solicitado
                    if (msgType == MessageType.SetNPCVelocity)
                        npc.velocity = new Vector2(velX, velY);
                    else if (msgType == MessageType.AddNPCVelocity)
                        npc.velocity += new Vector2(velX, velY);

                    // Si somos el servidor, forzamos a que el sistema oficial de Terraria 
                    // le diga a todos los clientes dónde está este NPC y qué velocidad tiene
                    if (Main.netMode == NetmodeID.Server)
                    {
                        npc.netUpdate = true;
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIndex);
                    }
                }
            }
        }
    }
}