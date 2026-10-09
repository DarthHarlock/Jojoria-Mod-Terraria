using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.NPCs.ScaryMonsters.MinionDinosaurios;

namespace Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4.Marca
{
    /// <summary>
    /// Sistema de marca de Scary Monsters.
    /// El Stand escribe aquí el NPC marcado de su dueño (sincronizado por SendExtraAI del Stand),
    /// y los dinos (MiniDino / TransformedDinoNPC) lo leen.
    /// </summary>
    public static class ScaryMarca
    {
        public static float RangoMaximo = 1800f; // Si el marcado se aleja más del dueño, los dinos lo ignoran

        // Índice = jugador dueño, valor = whoAmI del NPC marcado (-1 = ninguno)
        public static readonly int[] ObjetivoPorJugador = Crear();

        static int[] Crear()
        {
            int[] a = new int[Main.maxPlayers];
            for (int i = 0; i < a.Length; i++) a[i] = -1;
            return a;
        }

        public static NPC GetObjetivo(int owner)
        {
            if (owner < 0 || owner >= Main.maxPlayers) return null;

            int idx = ObjetivoPorJugador[owner];
            if (idx < 0 || idx >= Main.maxNPCs) return null;

            NPC n = Main.npc[idx];
            if (!n.active || n.life <= 0) return null;

            Player p = Main.player[owner];
            if (!p.active || p.dead) return null;

            if (Vector2.DistanceSquared(n.Center, p.Center) > RangoMaximo * RangoMaximo)
                return null;

            return n;
        }

        public static bool EsMarcado(int owner, NPC npc)
        {
            NPC m = GetObjetivo(owner);
            return m != null && m.whoAmI == npc.whoAmI;
        }

        /// <summary>NPC bajo el cursor (cualquier tipo: hostil, pacífico, neutral, critter...).</summary>
        public static NPC BuscarBajoCursor()
        {
            Vector2 mouse = Main.MouseWorld;
            NPC mejor = null;
            float mejorDist = float.MaxValue;
            int dinoTipo = ModContent.NPCType<TransformedDinoNPC>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!n.active || n.life <= 0 || n.type == dinoTipo) continue;

                Rectangle hb = n.Hitbox;
                hb.Inflate(8, 8); // margen para critters pequeños
                if (!hb.Contains(mouse.ToPoint())) continue;

                float d = Vector2.DistanceSquared(n.Center, mouse);
                if (d < mejorDist)
                {
                    mejorDist = d;
                    mejor = n;
                }
            }
            return mejor;
        }
    }

    /// <summary>Dibuja el icono de marca sobre el NPC marcado.</summary>
    public class ScaryMarcaGlobalNPC : GlobalNPC
    {
        const string RutaTextura = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_4/Marca/ScaryMarca";
        const int Frames = 4;
        const int FrameAncho = 22;
        const int FrameAlto = 28;

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            bool marcado = false;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (ScaryMarca.ObjetivoPorJugador[i] == npc.whoAmI && Main.player[i].active)
                {
                    marcado = true;
                    break;
                }
            }
            if (!marcado) return;

            if (!ModContent.HasAsset(RutaTextura)) return;
            Texture2D tex = ModContent.Request<Texture2D>(RutaTextura).Value;

            int frame = (int)(Main.GameUpdateCount / 8 % Frames);
            Rectangle r = new Rectangle(0, frame * FrameAlto, FrameAncho, FrameAlto);
            Vector2 origin = new Vector2(FrameAncho / 2f, FrameAlto / 2f);

            spriteBatch.Draw(tex, npc.Center - screenPos, r, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
        }
    }
}