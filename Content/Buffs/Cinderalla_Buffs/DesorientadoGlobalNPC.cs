using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace Jojo.Content.Buffs.Cinderalla_Buffs
{
    public class DesorientadoGlobalNPC : GlobalNPC
    {
        // ---- Ajustes (por si quieres afinarlo) ----
        const int DuracionBloque = 40;      // ticks entre cambios de rumbo (60 = 1 s)
        const float DistanciaMin = 900f;      // a qué distancia se coloca el señuelo
        const float DistanciaMax = 1400f;
        const float ProbHuida = 0.5f;         // 0.5 = mitad huye de ti, mitad gira en seco
        const float AperturaHuida = 0.6f;     // dispersión (radianes) al huir

        // Si lo pones en true, el boss desorientado tampoco te hace daño por contacto
        static readonly bool EvitarDanoContacto = false;

        // Estado temporal del intercambio de posiciones (se usa dentro de un mismo tick)
        static readonly Vector2[] posOriginal = new Vector2[Main.maxPlayers];
        static readonly bool[] intercambiado = new bool[Main.maxPlayers];
        static bool intercambioActivo;

        // ¿Cuenta como boss?
        public static bool EsJefe(NPC npc)
        {
            NPC d = Duenio(npc);
            return d.boss || NPCID.Sets.ShouldBeCountedAsBoss[d.type] || npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type];
        }

        // Los bosses de segmentos (Destructor, etc.) comparten vida/buffs con su segmento principal
        public static NPC Duenio(NPC npc)
        {
            if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs && Main.npc[npc.realLife].active)
                return Main.npc[npc.realLife];
            return npc;
        }

        public static bool EstaDesorientado(NPC npc)
        {
            return Duenio(npc).HasBuff(ModContent.BuffType<Desorientado>());
        }

        // Garantiza que el boss pueda recibir el buff (algunos marcan todos los buffs como inmunes)
        public override void ResetEffects(NPC npc)
        {
            if (EsJefe(npc))
            {
                npc.buffImmune[ModContent.BuffType<Desorientado>()] = false;
            }
        }

        // ANTES de la IA del boss: te "teletransporta" (solo para su IA) al punto señuelo
        public override bool PreAI(NPC npc)
        {
            if (!EsJefe(npc) || !EstaDesorientado(npc)) return true;

            Player referencia = JugadorMasCercano(npc);
            if (referencia == null) return true;

            Vector2 senuelo = CalcularSenuelo(npc, referencia);

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                intercambiado[i] = false;

                Player p = Main.player[i];
                if (p == null || !p.active) continue;

                posOriginal[i] = p.position;
                intercambiado[i] = true;
                p.Center = senuelo;
            }

            intercambioActivo = true;
            return true;
        }

        // DESPUÉS de la IA del boss: devuelve a cada jugador a su posición real
        public override void PostAI(NPC npc)
        {
            if (!intercambioActivo) return;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (!intercambiado[i]) continue;

                Player p = Main.player[i];
                if (p != null) p.position = posOriginal[i];
                intercambiado[i] = false;
            }

            intercambioActivo = false;
        }

        static Player JugadorMasCercano(NPC npc)
        {
            Player mejor = null;
            float mejorDist = float.MaxValue;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active || p.dead) continue;

                float d = Vector2.DistanceSquared(p.Center, npc.Center);
                if (d < mejorDist)
                {
                    mejorDist = d;
                    mejor = p;
                }
            }

            return mejor;
        }

        // Punto falso al que el boss cree que estás. Determinista: cliente y servidor calculan lo mismo.
        static Vector2 CalcularSenuelo(NPC npc, Player referencia)
        {
            Vector2 haciaJugador = referencia.Center - npc.Center;
            if (haciaJugador == Vector2.Zero) haciaJugador = Vector2.UnitX;
            float anguloJugador = haciaJugador.ToRotation();

            int bloque = (int)((Main.GameUpdateCount + (uint)(npc.whoAmI * 13)) / (uint)DuracionBloque);
            UnifiedRandom rng = new UnifiedRandom(npc.whoAmI * 7919 + bloque * 104729 + 17);

            float angulo;
            if (rng.NextFloat() < ProbHuida)
            {
                // Huir de ti (en dirección opuesta, con algo de dispersión)
                angulo = anguloJugador + MathHelper.Pi + rng.NextFloat(-AperturaHuida, AperturaHuida);
            }
            else
            {
                // Giro en seco: cualquier dirección entre 90° y 270° respecto a ti (nunca hacia ti)
                angulo = anguloJugador + rng.NextFloat(MathHelper.PiOver2, MathHelper.Pi * 1.5f);
            }

            Vector2 dir = angulo.ToRotationVector2();

            // Bosses con gravedad: solo se mueven en horizontal
            if (!npc.noGravity)
                dir = new Vector2(dir.X >= 0f ? 1f : -1f, 0f);

            float distancia = rng.NextFloat(DistanciaMin, DistanciaMax);
            Vector2 senuelo = npc.Center + dir * distancia;

            // Mantener el señuelo dentro del mundo
            float margen = 320f;
            senuelo.X = MathHelper.Clamp(senuelo.X, margen, Main.maxTilesX * 16f - margen);
            senuelo.Y = MathHelper.Clamp(senuelo.Y, margen, Main.maxTilesY * 16f - margen);

            return senuelo;
        }

        // Opcional: que un boss desorientado no haga daño por contacto
        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (EvitarDanoContacto && EsJefe(npc) && EstaDesorientado(npc))
                return false;

            return true;
        }

        // Signos de interrogación flotando sobre la cabeza del boss
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!EsJefe(npc)) return;

            // En jefes tipo gusano, solo dibujar en el segmento principal
            if (npc.realLife >= 0 && npc.realLife != npc.whoAmI) return;
            if (npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail) return;

            if (!EstaDesorientado(npc)) return;

            float t = Main.GlobalTimeWrappedHourly;
            float separacion = MathHelper.Clamp(npc.width * 0.2f, 26f, 90f);
            Vector2 baseCabeza = npc.Top - screenPos + new Vector2(0f, -24f);

            for (int i = 0; i < 3; i++)
            {
                float offsetX = (i - 1) * separacion;
                float bob = (float)Math.Sin(t * 4f + i * 1.7f) * 6f;
                float pulso = 0.5f + 0.5f * (float)Math.Sin(t * 5f + i * 2f);
                float escala = 1.1f + 0.25f * pulso;
                Color color = Color.Lerp(Color.HotPink, Color.White, pulso);

                Utils.DrawBorderString(
                    spriteBatch,
                    "?",
                    baseCabeza + new Vector2(offsetX, bob),
                    color,
                    escala,
                    0.5f,
                    0.5f
                );
            }
        }
    }
}