using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_4.BitesTheDust
{
    // Sincroniza por red el "rebobinado" de Bites The Dust: que TODOS los
    // clientes vean a los NPCs volver a su posición guardada, y que TODOS
    // escuchen el mismo sonido, no solo quien activó la habilidad.
    public static class BitesTheDustNet
    {
        // Byte identificador de este paquete DENTRO de tu propio mod.
        // Si ya usás el valor 200 en otro paquete tuyo, cambialo por otro libre.
        public const byte PacketId = 200;

        static readonly SoundStyle RewindSound = new("Jojo/Content/Sonidos/KillerQueenBomba2");

        // IMPORTANTE: "Jojo" debe ser el NOMBRE INTERNO real de tu mod
        // (el campo "name" de tu build.txt). Si es distinto, cambialo acá.
        static Mod JojoMod => ModLoader.GetMod("Jojo");

        // Llamado desde el cliente dueño del jugador al activar la habilidad.
        public static void RequestRewind(Vector2 soundPosition, List<int> npcIds, List<Vector2> positions, List<Rectangle> frames, List<int> spriteDirs)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                ApplyRewind(npcIds, positions, frames, spriteDirs);
                PlayGlobalRewindSound();
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = BuildPacket(soundPosition, npcIds, positions, frames, spriteDirs);
                packet.Send(); // al servidor, que decide y reenvía a todos
            }
            else if (Main.netMode == NetmodeID.Server)
            {
                // Caso defensivo, normalmente no debería pasar (los jugadores
                // no corren en el server), pero por seguridad lo tratamos igual.
                ApplyRewind(npcIds, positions, frames, spriteDirs);
                BroadcastToClients(soundPosition, npcIds, positions, frames, spriteDirs);
            }
        }

        public static void HandlePacket(BinaryReader reader, int whoAmI)
        {
            Vector2 soundPosition = new(reader.ReadSingle(), reader.ReadSingle());
            short count = reader.ReadInt16();
            List<int> npcIds = new(count);
            List<Vector2> positions = new(count);
            List<Rectangle> frames = new(count);
            List<int> spriteDirs = new(count);

            for (int i = 0; i < count; i++)
            {
                npcIds.Add(reader.ReadInt16());
                positions.Add(new Vector2(reader.ReadSingle(), reader.ReadSingle()));
                int fx = reader.ReadInt32(), fy = reader.ReadInt32(), fw = reader.ReadInt32(), fh = reader.ReadInt32();
                frames.Add(new Rectangle(fx, fy, fw, fh));
                spriteDirs.Add(reader.ReadSByte());
            }

            if (Main.netMode == NetmodeID.Server)
            {
                // El servidor tiene la autoridad real sobre los NPCs: aplica
                // y reenvía a TODOS los clientes, incluido quien lo pidió.
                ApplyRewind(npcIds, positions, frames, spriteDirs);
                BroadcastToClients(soundPosition, npcIds, positions, frames, spriteDirs);
            }
            else
            {
                // Somos un cliente recibiendo la orden ya validada del servidor.
                ApplyRewind(npcIds, positions, frames, spriteDirs);
                PlayGlobalRewindSound();
            }
        }

        static void BroadcastToClients(Vector2 soundPosition, List<int> npcIds, List<Vector2> positions, List<Rectangle> frames, List<int> spriteDirs)
        {
            ModPacket packet = BuildPacket(soundPosition, npcIds, positions, frames, spriteDirs);
            packet.Send(-1); // a todos los clientes, incluido el que lo originó
        }

        static ModPacket BuildPacket(Vector2 soundPosition, List<int> npcIds, List<Vector2> positions, List<Rectangle> frames, List<int> spriteDirs)
        {
            ModPacket packet = JojoMod.GetPacket();
            packet.Write(PacketId);
            packet.Write(soundPosition.X);
            packet.Write(soundPosition.Y);
            packet.Write((short)npcIds.Count);
            for (int i = 0; i < npcIds.Count; i++)
            {
                packet.Write((short)npcIds[i]);
                packet.Write(positions[i].X);
                packet.Write(positions[i].Y);
                packet.Write(frames[i].X);
                packet.Write(frames[i].Y);
                packet.Write(frames[i].Width);
                packet.Write(frames[i].Height);
                packet.Write((sbyte)spriteDirs[i]);
            }
            return packet;
        }

        static void ApplyRewind(List<int> npcIds, List<Vector2> positions, List<Rectangle> frames, List<int> spriteDirs)
        {
            for (int i = 0; i < npcIds.Count; i++)
            {
                int id = npcIds[i];
                if (id < 0 || id >= Main.npc.Length) continue;
                NPC npc = Main.npc[id];
                if (!npc.active) continue;

                npc.position = positions[i] - (npc.Size / 2f);
                npc.frame = frames[i];
                npc.spriteDirection = spriteDirs[i];
                npc.netUpdate = true; // el server re-propaga esta posición
            }
        }

        // FIX: Antes se llamaba SoundEngine.PlaySound(RewindSound, soundPosition),
        // lo que hace que Terraria aplique atenuación por distancia y paneo
        // estéreo (por eso se escuchaba cada vez menos al alejarse).
        //
        // Al no pasar ninguna posición, el sonido se reproduce como un sonido
        // "global" (sin caída de volumen por distancia). Como cada cliente
        // ejecuta esta línea localmente al recibir el paquete, el resultado
        // es que TODOS los jugadores lo escuchan siempre a volumen completo,
        // sin importar dónde estén parados.
        static void PlayGlobalRewindSound()
        {
            SoundEngine.PlaySound(RewindSound);
        }
    }
}