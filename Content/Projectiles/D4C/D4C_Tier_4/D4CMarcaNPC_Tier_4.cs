using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Jojo.Content.Projectiles.D4C.D4C_Tier_4
{
    public class D4CGlobalNPC_Tier_4 : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public static readonly Dictionary<int, int> ObjetivoMarcadoPorJugador = new();

        public int d4cMarkTimer;
        public int OwnerIndex = -1;

        int frameAnimTimer;
        int currentFrame;

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(d4cMarkTimer);
            binaryWriter.Write((sbyte)OwnerIndex);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            d4cMarkTimer = binaryReader.ReadInt32();
            OwnerIndex = binaryReader.ReadSByte();
        }

        public override void PostAI(NPC npc)
        {
            // Si ya no tiene la marca, pero sigue registrado en el diccionario, lo limpiamos.
            if (d4cMarkTimer <= 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && OwnerIndex >= 0 && OwnerIndex < Main.maxPlayers)
                {
                    if (ObjetivoMarcadoPorJugador.TryGetValue(OwnerIndex, out int marcado) && marcado == npc.whoAmI)
                    {
                        ObjetivoMarcadoPorJugador[OwnerIndex] = -1;
                        OwnerIndex = -1;
                    }
                }
                return;
            }

            // Ya no restamos d4cMarkTimer-- para que la marca dure infinitamente hasta que muera o se cambie.

            // Animación de la marca
            if (++frameAnimTimer >= 8)
            {
                frameAnimTimer = 0;
                currentFrame = (currentFrame + 1) % 4;
            }
        }

        public override void OnKill(NPC npc)
        {
            // Limpiamos el objetivo del diccionario permanentemente si el enemigo muere
            if (d4cMarkTimer > 0 && OwnerIndex >= 0 && OwnerIndex < Main.maxPlayers)
            {
                if (ObjetivoMarcadoPorJugador.TryGetValue(OwnerIndex, out int marcado) && marcado == npc.whoAmI)
                {
                    ObjetivoMarcadoPorJugador[OwnerIndex] = -1;
                }
            }
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (d4cMarkTimer <= 0) return;

            Texture2D tex = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/D4C/D4C_Tier_4/D4C_Marca_Tier_4").Value;
            Rectangle r = new Rectangle(0, currentFrame * 28, 22, 28);
            Vector2 origin = new Vector2(11, 14);

            spriteBatch.Draw(tex, npc.Center - screenPos, r, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
        }
    }
}