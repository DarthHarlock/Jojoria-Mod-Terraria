using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.SilverChariotRequiem_Buffs;


namespace Jojo.Content.Projectiles.SilverChariot.SilverChariot_Requiem
{
    public class SuenoProfundoGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public int sleepTimer = 0;
        public uint lastAuraHit = 0;

        private bool wasSleeping = false;
        private bool frozen = false; // Paralizado tras tocar un bloque solido
        private int savedFrame = 0;
        private int savedDir = 1;
        private bool oldNoTileCollide;
        private bool oldNoGravity;

        // NUEVO: guardamos qué whoAmI's pertenecen realmente a ESTE gusano
        // en el momento en que se queda dormido, para no seguir índices
        // reciclados por NPCs que no tienen nada que ver.
        private HashSet<int> segmentosValidos = null;

        // Distancia de seguridad: si el "segmento de adelante" calculado
        // está más lejos que esto, algo está mal (slot reciclado) y no
        // debemos teletransportarnos hacia él.
        private const float MaxDistanciaSeguimiento = 600f;

        // Partes de Moon Lord: núcleo (torso), cabeza y ambas manos.
        // Las agrupamos por TIPO de NPC directamente, sin depender de
        // realLife/ai[], para que dormir cualquiera de sus partes
        // sincronice a las demás sin excepción.
        private static readonly HashSet<int> TiposMoonLord = new HashSet<int>
        {
            NPCID.MoonLordCore,
            NPCID.MoonLordHead,
            NPCID.MoonLordHand
        };

        public override void SetDefaults(NPC npc)
        {
            npc.buffImmune[ModContent.BuffType<SuenoProfundoDebuff>()] = false;
        }

        public override bool PreAI(NPC npc)
        {
            // 1. SISTEMA DE SUEÑO SINCRONIZADO
            if (Main.GameUpdateCount - lastAuraHit <= 2)
            {
                if (!npc.HasBuff(ModContent.BuffType<SuenoProfundoDebuff>()))
                {
                    sleepTimer++;
                    if (sleepTimer >= 120) // 2 segundos
                    {
                        // En vez de dormir SOLO a este NPC, dormimos a todos
                        // los segmentos de su misma "familia" (torso/cabeza/
                        // manos de Moon Lord, gusanos, Muro de Carne, etc.)
                        // apenas cualquiera de ellos cumple el tiempo. Así
                        // no puede pasar que se duerma la cabeza y el resto
                        // del cuerpo siga funcionando.
                        int buffType = ModContent.BuffType<SuenoProfundoDebuff>();
                        List<NPC> segmentosParaDormir = ObtenerSegmentosGusano(npc);
                        foreach (NPC seg in segmentosParaDormir)
                        {
                            if (!seg.HasBuff(buffType))
                                seg.AddBuff(buffType, 1200);
                        }
                        sleepTimer = 0;
                    }
                }
            }
            else
            {
                sleepTimer = 0;
            }

            // 2. COMPORTAMIENTO MIENTRAS DUERME
            if (npc.HasBuff(ModContent.BuffType<SuenoProfundoDebuff>()))
            {
                if (!wasSleeping)
                {
                    savedFrame = npc.frame.Y;
                    savedDir = npc.spriteDirection;
                    oldNoTileCollide = npc.noTileCollide;
                    oldNoGravity = npc.noGravity;
                    wasSleeping = true;
                    frozen = false; // empieza cayendo, no congelado

                    // Capturamos SOLO ahora qué NPCs son realmente parte
                    // de este gusano. Esto evita que, si más tarde un
                    // segmento muere y su slot en Main.npc[] es reciclado
                    // por otro NPC random, sigamos ese índice viejo y nos
                    // "teletransportemos" hacia él.
                    segmentosValidos = null;
                    if (npc.aiStyle == 6)
                    {
                        List<NPC> segs = ObtenerSegmentosGusano(npc);
                        segmentosValidos = new HashSet<int>();
                        foreach (NPC s in segs)
                            segmentosValidos.Add(s.whoAmI);
                    }
                }

                // --- YA ATERRIZÓ: PARALIZADO TOTAL, no se toca nada más ---
                if (frozen)
                {
                    npc.velocity = Vector2.Zero;
                    return false;
                }

                // --- CASO ESPECIAL: MURO DE CARNE Y MOON LORD ---
                // Ambos jefes viven "flotando" (Inframundo / cielo) sin un
                // piso confiable debajo de cada parte. Si les aplicamos
                // gravedad + TileCollision normal, sus partes (cabezas del
                // Muro, o cabeza/manos/torso de Moon Lord) caen cada una
                // hacia donde encuentren suelo, separándose entre sí (esto
                // es lo que causaba la "decapitación" de Moon Lord). Por
                // eso a estos jefes los congelamos tal cual están parados,
                // sin tocar velocidad ni posición.
                bool esMuroDeCarne = npc.type == NPCID.WallofFlesh || npc.type == NPCID.WallofFleshEye;
                bool esMoonLord = TiposMoonLord.Contains(npc.type);

                if (esMuroDeCarne || esMoonLord)
                {
                    npc.velocity = Vector2.Zero;
                    frozen = true;
                    return false;
                }

                // Solo tratamos como "cadena de gusano" a los aiStyle 6 reales.
                // Manos/cabeza de Moon Lord u otras partes con solo realLife NO entran aquí,
                // porque su ai[0] no significa "segmento de adelante" y rompía todo.
                bool esGusanoEncadenado = npc.aiStyle == 6;

                if (esGusanoEncadenado)
                {
                    // --- FÍSICAS DE GUSANO SEGURO ---
                    npc.velocity.X = 0;
                    if (npc.velocity.Y < 22f)
                        npc.velocity.Y += 0.9f; // Gravedad muy rápida

                    float velYAntes = npc.velocity.Y;
                    npc.velocity = Collision.TileCollision(npc.position, npc.velocity, npc.width, npc.height, false, false, 1);

                    // Detectamos aterrizaje real (la colisión frenó la caída)
                    if (velYAntes > 1f && npc.velocity.Y <= 0.15f)
                    {
                        frozen = true;
                        npc.velocity = Vector2.Zero;
                        npc.noGravity = true;
                        return false;
                    }

                    // Control de eslabones para que caiga y se amolde al suelo sin romperse.
                    // IMPORTANTE: ahora validamos que el "segmento de adelante" sea
                    // realmente parte de este gusano, no un índice reciclado.
                    bool esSegmentoSeguidor = false;
                    NPC segmentoAdelante = null;

                    if (npc.ai[0] >= 0 && npc.ai[0] < Main.maxNPCs)
                    {
                        int idAdelante = (int)npc.ai[0];
                        NPC ahead = Main.npc[idAdelante];

                        bool esValido = ahead.active
                            && ahead.whoAmI != npc.whoAmI
                            && ahead.type == npc.type
                            && (segmentosValidos == null || segmentosValidos.Contains(ahead.whoAmI));

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

                        // Segunda barrera de seguridad: si por lo que sea la
                        // distancia es absurda, ignoramos el seguimiento en
                        // vez de teletransportarnos hacia allá.
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
                    // --- ENEMIGOS NORMALES (incluye manos/cabeza de Moon Lord, Golem, etc.) ---
                    npc.noTileCollide = false;
                    npc.noGravity = false;
                    npc.velocity.X = 0;

                    if (npc.velocity.Y < 22f)
                        npc.velocity.Y += 0.9f; // Caída súper rápida y pesada al suelo

                    float velYAntes = npc.velocity.Y;
                    npc.velocity = Collision.TileCollision(npc.position, npc.velocity, npc.width, npc.height, false, false, 1);

                    // Detectamos aterrizaje real (funciona con bloques Y plataformas,
                    // porque TileCollision ya respeta semisólidos)
                    if (velYAntes > 1f && npc.velocity.Y <= 0.15f)
                    {
                        frozen = true;
                        npc.velocity = Vector2.Zero;
                        npc.noGravity = true;
                    }
                }

                return false; // Bloquea la IA original
            }
            else
            {
                if (wasSleeping)
                {
                    // Al despertar recupera todo perfectamente y sigue moviéndose desde donde quedó
                    npc.noTileCollide = oldNoTileCollide;
                    npc.noGravity = oldNoGravity;
                    wasSleeping = false;
                    frozen = false;
                    segmentosValidos = null;
                }
            }

            return base.PreAI(npc);
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            if (npc.HasBuff(ModContent.BuffType<SuenoProfundoDebuff>()))
            {
                npc.frame.Y = savedFrame;
                npc.spriteDirection = savedDir;
                npc.direction = savedDir;
            }
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            ProcesarDespertar(npc);
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            ProcesarDespertar(npc);
        }

        private void ProcesarDespertar(NPC npc)
        {
            int buffType = ModContent.BuffType<SuenoProfundoDebuff>();
            List<NPC> segmentos = ObtenerSegmentosGusano(npc);

            foreach (NPC seg in segmentos)
            {
                int buffIndex = seg.FindBuffIndex(buffType);
                if (buffIndex != -1 && seg.buffTime[buffIndex] > 300)
                {
                    seg.buffTime[buffIndex] = 300;
                }
            }
        }

        public static List<NPC> ObtenerSegmentosGusano(NPC npc)
        {
            List<NPC> lista = new List<NPC>();
            if (npc == null || !npc.active) return lista;

            // Caso 0: Moon Lord. Lo agrupamos directamente por TIPO de NPC
            // (núcleo, cabeza, mano) en vez de confiar en realLife/ai[],
            // porque esa relación no siempre está armada como uno esperaría
            // y era lo que permitía que se durmiera solo la cabeza mientras
            // el resto seguía funcionando.
            if (TiposMoonLord.Contains(npc.type))
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC o = Main.npc[i];
                    if (o.active && TiposMoonLord.Contains(o.type))
                    {
                        lista.Add(o);
                    }
                }
                return lista;
            }

            // Caso 1: el NPC ES una parte secundaria que apunta a otro NPC via realLife
            // (mano/cabeza de Moon Lord, puños de Golem, miembros de Skeletron Prime, etc.)
            if (npc.realLife >= 0)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC o = Main.npc[i];
                    if (o.active && (o.realLife == npc.realLife || o.whoAmI == npc.realLife))
                    {
                        lista.Add(o);
                    }
                }
                return lista;
            }

            // Caso 2: el NPC ES el "núcleo" del que otras partes dependen
            // (ej: golpeaste el Core de Moon Lord primero, no la mano)
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
                    if (o.active && o.whoAmI != npc.whoAmI && o.realLife == npc.whoAmI)
                    {
                        lista.Add(o);
                    }
                }
                return lista;
            }

            // Caso 3: gusanos encadenados por ai[0]/ai[1] (Devourer, Wyrm, etc.)
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