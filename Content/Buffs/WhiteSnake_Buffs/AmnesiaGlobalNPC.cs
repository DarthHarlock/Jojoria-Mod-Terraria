using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Jojo.Content.Buffs.WhiteSnake_Buffs;

namespace Jojo.Content.Buffs.WhiteSnake_Buffs
{
    public class AmnesiaGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        private bool wasSleeping = false;
        private bool frozen = false;
        private int savedFrame = 0;
        private int savedDir = 1;
        private bool oldNoTileCollide;
        private bool oldNoGravity;

        private HashSet<int> segmentosValidos = null;
        private const float MaxDistanciaSeguimiento = 600f;

        private static readonly HashSet<int> TiposMoonLord = new HashSet<int>
        {
            NPCID.MoonLordCore,
            NPCID.MoonLordHead,
            NPCID.MoonLordHand
        };

        public override void SetDefaults(NPC npc)
        {
            npc.buffImmune[ModContent.BuffType<AmnesiaDebuff>()] = false;
            npc.buffImmune[ModContent.BuffType<AmnesiaCooldown>()] = false;
        }

        public override bool PreAI(NPC npc)
        {
            // SI TIENE EL DEBUFF ACTIVO
            if (npc.HasBuff(ModContent.BuffType<AmnesiaDebuff>()))
            {
                // Solo capturamos el estado UNA VEZ al inicio exacto del sueño
                if (!wasSleeping)
                {
                    savedFrame = npc.frame.Y;
                    savedDir = npc.spriteDirection;

                    // Capturamos la gravedad real ANTES de alterar nada
                    oldNoTileCollide = npc.noTileCollide;
                    oldNoGravity = npc.noGravity;

                    wasSleeping = true;
                    frozen = false;

                    segmentosValidos = null;
                    if (npc.aiStyle == 6)
                    {
                        List<NPC> segs = ObtenerSegmentosGusano(npc);
                        segmentosValidos = new HashSet<int>();
                        foreach (NPC s in segs)
                            segmentosValidos.Add(s.whoAmI);
                    }
                }

                if (frozen)
                {
                    npc.velocity = Vector2.Zero;
                    return false;
                }

                bool esMuroDeCarne = npc.type == NPCID.WallofFlesh || npc.type == NPCID.WallofFleshEye;
                bool esMoonLord = TiposMoonLord.Contains(npc.type);

                if (esMuroDeCarne || esMoonLord)
                {
                    npc.velocity = Vector2.Zero;
                    frozen = true;
                    return false;
                }

                bool esGusanoEncadenado = npc.aiStyle == 6;

                if (esGusanoEncadenado)
                {
                    npc.velocity.X = 0;
                    if (npc.velocity.Y < 22f) npc.velocity.Y += 0.9f;

                    float velYAntes = npc.velocity.Y;
                    npc.velocity = Collision.TileCollision(npc.position, npc.velocity, npc.width, npc.height, false, false, 1);

                    if (velYAntes > 1f && npc.velocity.Y <= 0.15f)
                    {
                        frozen = true;
                        npc.velocity = Vector2.Zero;
                        npc.noGravity = true;
                        return false;
                    }

                    bool esSegmentoSeguidor = false;
                    NPC segmentoAdelante = null;

                    if (npc.ai[0] >= 0 && npc.ai[0] < Main.maxNPCs)
                    {
                        int idAdelante = (int)npc.ai[0];
                        NPC ahead = Main.npc[idAdelante];

                        bool esValido = ahead.active && ahead.whoAmI != npc.whoAmI && ahead.type == npc.type &&
                                        (segmentosValidos == null || segmentosValidos.Contains(ahead.whoAmI));

                        if (esValido)
                        {
                            esSegmentoSeguidor = true;
                            segmentoAdelante = ahead;
                        }
                    }

                    if (esSegmentoSeguidor && segmentoAdelante != null)
                    {
                        Vector2 dir = segmentoAdelante.Center - npc.Center;
                        float dist = dir.Length();

                        if (dist <= MaxDistanciaSeguimiento)
                        {
                            float targetDist = (npc.width + npc.height) / 2f;
                            if (targetDist <= 0) targetDist = 32f;

                            if (dist > targetDist * 1.1f)
                            {
                                npc.Center = segmentoAdelante.Center - Vector2.Normalize(dir) * targetDist;
                                npc.velocity = Vector2.Zero;
                            }
                            if (dir != Vector2.Zero)
                                npc.rotation = dir.ToRotation() + MathHelper.PiOver2;

                            npc.spriteDirection = segmentoAdelante.spriteDirection;
                            npc.direction = segmentoAdelante.direction;
                        }
                    }
                }
                else
                {
                    // ENEMIGOS NORMALES: Los forzamos a caer
                    npc.noTileCollide = false;
                    npc.noGravity = false;
                    npc.velocity.X = 0;

                    if (npc.velocity.Y < 22f) npc.velocity.Y += 0.9f;

                    float velYAntes = npc.velocity.Y;
                    npc.velocity = Collision.TileCollision(npc.position, npc.velocity, npc.width, npc.height, false, false, 1);

                    if (velYAntes > 1f && npc.velocity.Y <= 0.15f)
                    {
                        frozen = true;
                        npc.velocity = Vector2.Zero;
                        npc.noGravity = true;
                    }
                }

                return false;
            }
            else
            {
                // DESPERTANDO: Restauración limpia
                if (wasSleeping)
                {
                    // Restauramos las propiedades exactas que tenía antes de dormirse
                    npc.noTileCollide = oldNoTileCollide;
                    npc.noGravity = oldNoGravity;

                    // Si era un enemigo volador (noGravity original era true), le damos un empuje
                    // hacia arriba para despegarlo del suelo y reactivar su física de vuelo
                    if (oldNoGravity && npc.velocity.Y == 0f)
                    {
                        npc.velocity.Y -= 4f;
                    }

                    wasSleeping = false;
                    frozen = false;
                    segmentosValidos = null;
                    npc.netUpdate = true;
                }
            }

            return base.PreAI(npc);
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            if (npc.HasBuff(ModContent.BuffType<AmnesiaDebuff>()))
            {
                npc.frame.Y = savedFrame;
                npc.spriteDirection = savedDir;
                npc.direction = savedDir;
            }
        }

        // Unificamos el dibujado aquí mismo para no tener clases separadas
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (npc.HasBuff(ModContent.BuffType<AmnesiaDebuff>()))
            {
                Texture2D tex = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_1/NoDiscoDebuff").Value;
                Vector2 drawPos = npc.Center - screenPos;
                Vector2 origin = tex.Size() / 2f;

                spriteBatch.Draw(tex, drawPos, null, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
            }
        }

        public static List<NPC> ObtenerSegmentosGusano(NPC npc)
        {
            List<NPC> lista = new List<NPC>();
            if (npc == null || !npc.active) return lista;

            if (TiposMoonLord.Contains(npc.type))
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC o = Main.npc[i];
                    if (o.active && TiposMoonLord.Contains(o.type)) lista.Add(o);
                }
                return lista;
            }

            if (npc.realLife >= 0)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC o = Main.npc[i];
                    if (o.active && (o.realLife == npc.realLife || o.whoAmI == npc.realLife)) lista.Add(o);
                }
                return lista;
            }

            bool esNucleo = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC o = Main.npc[i];
                if (o.active && o.whoAmI != npc.whoAmI && o.realLife == npc.whoAmI)
                {
                    esNucleo = true;
                    break;
                }
            }

            if (esNucleo)
            {
                lista.Add(npc);
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC o = Main.npc[i];
                    if (o.active && o.whoAmI != npc.whoAmI && o.realLife == npc.whoAmI) lista.Add(o);
                }
                return lista;
            }

            if (npc.aiStyle == 6)
            {
                HashSet<int> visitados = new HashSet<int>();
                Queue<NPC> colaDeRastreo = new Queue<NPC>();

                colaDeRastreo.Enqueue(npc);
                visitados.Add(npc.whoAmI);

                while (colaDeRastreo.Count > 0)
                {
                    NPC actual = colaDeRastreo.Dequeue();
                    lista.Add(actual);

                    int adelanteID = (int)actual.ai[0];
                    if (adelanteID >= 0 && adelanteID < Main.maxNPCs)
                    {
                        NPC npcAdelante = Main.npc[adelanteID];
                        if (npcAdelante.active && npcAdelante.aiStyle == 6 && !visitados.Contains(adelanteID))
                        {
                            visitados.Add(adelanteID);
                            colaDeRastreo.Enqueue(npcAdelante);
                        }
                    }

                    int atrasID = (int)actual.ai[1];
                    if (atrasID >= 0 && atrasID < Main.maxNPCs)
                    {
                        NPC npcAtras = Main.npc[atrasID];
                        if (npcAtras.active && npcAtras.aiStyle == 6 && !visitados.Contains(atrasID))
                        {
                            visitados.Add(atrasID);
                            colaDeRastreo.Enqueue(npcAtras);
                        }
                    }
                }
                return lista;
            }

            lista.Add(npc);
            return lista;
        }
    }
}