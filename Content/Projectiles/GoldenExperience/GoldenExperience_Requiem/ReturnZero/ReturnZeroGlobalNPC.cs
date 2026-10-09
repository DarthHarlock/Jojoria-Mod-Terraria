using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem.ReturnZero
{
    public class ReturnZeroGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        const int MAX_HISTORIAL = 10 * 60;
        public int velocidadRebobinado = 2;
        public int rewindCooldown = 0;

        public readonly Queue<NpcSnapshot> historial = new();

        public bool rewindeando = false;
        List<NpcSnapshot> rutaRewind;
        int rewindIndex;

        int vecesMuerto = 0;
        const int MUERTES_TOTALES = 4;
        bool cicloMuerteActivo = false;
        int timerProximaMuerte = 0;
        const int RETRASO_ENTRE_MUERTES = 20;

        public override void ResetEffects(NPC npc)
        {
            if (rewindCooldown > 0) rewindCooldown--;
        }

        public override bool PreAI(NPC npc)
        {
            if (cicloMuerteActivo)
            {
                ActualizarCicloMuerte(npc);
                return false;
            }

            if (rewindeando)
            {
                EjecutarPasoDeRewind(npc);
                return false;
            }

            return true;
        }

        public override void PostAI(NPC npc)
        {
            if (rewindeando || cicloMuerteActivo) return;

            // Almacenamos TODA la memoria del NPC y su velocidad
            historial.Enqueue(new NpcSnapshot(
                npc.Center, npc.velocity, npc.rotation, npc.spriteDirection, npc.frame,
                npc.ai[0], npc.ai[1], npc.ai[2], npc.ai[3],
                npc.localAI[0], npc.localAI[1], npc.localAI[2], npc.localAI[3],
                npc.target
            ));

            if (historial.Count > MAX_HISTORIAL)
                historial.Dequeue();
        }

        static List<NpcSnapshot> ConDireccionesCorregidas(List<NpcSnapshot> cronologico)
        {
            var corregidos = new List<NpcSnapshot>(cronologico.Count);
            if (cronologico.Count == 0) return corregidos;

            const float UMBRAL = 0.05f;
            int direccionActual = cronologico[0].SpriteDirection != 0 ? cronologico[0].SpriteDirection : 1;

            corregidos.Add(cronologico[0].ConDireccion(direccionActual));

            for (int i = 1; i < cronologico.Count; i++)
            {
                float deltaX = cronologico[i].Position.X - cronologico[i - 1].Position.X;
                if (deltaX > UMBRAL) direccionActual = 1;
                else if (deltaX < -UMBRAL) direccionActual = -1;
                corregidos.Add(cronologico[i].ConDireccion(direccionActual));
            }

            return corregidos;
        }

        public static int IdentidadGrupo(NPC npc)
        {
            if (npc.realLife >= 0) return npc.realLife;

            if (npc.type == NPCID.MoonLordHand || npc.type == NPCID.MoonLordHead)
                return (int)npc.ai[3];
            if (npc.type == NPCID.SkeletronHand)
                return (int)npc.ai[1];
            if (npc.type == NPCID.PrimeCannon || npc.type == NPCID.PrimeLaser || npc.type == NPCID.PrimeSaw || npc.type == NPCID.PrimeVice)
                return (int)npc.ai[1];

            if (npc.type == NPCID.GolemFistLeft || npc.type == NPCID.GolemFistRight || npc.type == NPCID.GolemHead)
            {
                int cuerpoId = BuscarCuerpoDeGolemCercano(npc);
                if (cuerpoId >= 0) return cuerpoId;

                if (npc.type != NPCID.GolemHead)
                    return (int)npc.ai[0];
            }

            return npc.whoAmI;
        }

        static int BuscarCuerpoDeGolemCercano(NPC parte)
        {
            int mejorIndice = -1;
            float distanciaMinima = float.MaxValue;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC posibleCuerpo = Main.npc[i];
                if (!posibleCuerpo.active || posibleCuerpo.type != NPCID.Golem) continue;

                float distancia = Vector2.Distance(posibleCuerpo.Center, parte.Center);
                if (distancia < distanciaMinima)
                {
                    distanciaMinima = distancia;
                    mejorIndice = posibleCuerpo.whoAmI;
                }
            }

            return mejorIndice;
        }

        public static List<NPC> ObtenerSegmentosActivosDelGrupo(NPC origen)
        {
            int grupoId = IdentidadGrupo(origen);
            var lista = new List<NPC>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!n.active) continue;

                if (IdentidadGrupo(n) == grupoId) lista.Add(n);
            }

            return lista;
        }

        public static bool EsVidaCompartida(List<NPC> segmentos)
        {
            foreach (NPC n in segmentos)
            {
                if (n.realLife >= 0) return true;
            }
            return false;
        }

        public static NPC ObtenerRepresentanteDeVida(List<NPC> segmentos)
        {
            int grupoId = IdentidadGrupo(segmentos[0]);
            foreach (NPC n in segmentos)
            {
                if (n.whoAmI == grupoId) return n;
            }
            return segmentos[0];
        }

        public List<NpcSnapshot> IniciarRewind(NPC npc)
        {
            if (rewindeando || historial.Count < 2)
                return new List<NpcSnapshot>();

            var cronologico = new List<NpcSnapshot>(historial);

            bool perteneceAGrupoMultiSegmento = ObtenerSegmentosActivosDelGrupo(npc).Count > 1;
            if (perteneceAGrupoMultiSegmento)
            {
                const int MAX_TICKS_GUSANOS = 90;
                if (cronologico.Count > MAX_TICKS_GUSANOS)
                {
                    cronologico = cronologico.GetRange(cronologico.Count - MAX_TICKS_GUSANOS, MAX_TICKS_GUSANOS);
                }
            }

            var corregidos = ConDireccionesCorregidas(cronologico);

            rutaRewind = new List<NpcSnapshot>(corregidos);
            rutaRewind.Reverse();
            rewindIndex = 0;
            rewindeando = true;
            rewindCooldown = 120;

            vecesMuerto = 0;
            cicloMuerteActivo = false;
            timerProximaMuerte = 0;

            historial.Clear();
            npc.netUpdate = true;

            return corregidos;
        }

        void EjecutarPasoDeRewind(NPC npc)
        {
            if (rutaRewind == null || rewindIndex >= rutaRewind.Count)
            {
                FinalizarRewind(npc);
                return;
            }

            for (int paso = 0; paso < velocidadRebobinado && rewindIndex < rutaRewind.Count; paso++)
                rewindIndex++;

            int idxSeguro = Math.Min(rewindIndex, rutaRewind.Count - 1);
            NpcSnapshot snap = rutaRewind[idxSeguro];

            npc.Center = snap.Position;
            npc.rotation = snap.Rotation;
            npc.spriteDirection = snap.SpriteDirection;
            npc.frame = snap.Frame;
            // Durante la animación de rebobinar, anulamos la velocidad física para que no interfiera
            npc.velocity = Vector2.Zero;
            npc.netUpdate = true;

            if (rewindIndex >= rutaRewind.Count)
                FinalizarRewind(npc);
        }

        void FinalizarRewind(NPC npc)
        {
            if (rutaRewind != null && rutaRewind.Count > 0)
            {
                int idxSeguro = Math.Min(rewindIndex, rutaRewind.Count - 1);
                NpcSnapshot snap = rutaRewind[idxSeguro];

                // ¡AQUÍ ESTÁ LA MAGIA! Le devolvemos exactamente lo que estaba pensando y haciendo
                // en ese instante del pasado. La Abeja Reina continuará su dash perfectamente.
                npc.ai[0] = snap.AI0;
                npc.ai[1] = snap.AI1;
                npc.ai[2] = snap.AI2;
                npc.ai[3] = snap.AI3;
                npc.localAI[0] = snap.LocalAI0;
                npc.localAI[1] = snap.LocalAI1;
                npc.localAI[2] = snap.LocalAI2;
                npc.localAI[3] = snap.LocalAI3;
                npc.target = snap.Target;
                npc.velocity = snap.Velocity;
            }
            else
            {
                npc.velocity = Vector2.Zero;
            }

            rewindeando = false;
            rutaRewind = null;
            npc.netUpdate = true;
            EliminarSombrasDe(npc.whoAmI);
        }

        public override bool CheckDead(NPC npc)
        {
            if (!rewindeando && !cicloMuerteActivo)
                return true;

            vecesMuerto++;
            EfectoMuerteVisual(npc);
            npc.NPCLoot();

            if (vecesMuerto >= MUERTES_TOTALES)
            {
                vecesMuerto = 0;
                cicloMuerteActivo = false;
                rewindeando = false;
                return true;
            }

            npc.life = 1;
            npc.netUpdate = true;
            cicloMuerteActivo = true;
            timerProximaMuerte = RETRASO_ENTRE_MUERTES;

            return false;
        }

        void ActualizarCicloMuerte(NPC npc)
        {
            if (timerProximaMuerte-- > 0) return;
            npc.life = 0;
            npc.checkDead();
        }

        static void EfectoMuerteVisual(NPC npc)
        {
            for (int i = 0; i < 20; i++)
            {
                Dust.NewDust(npc.position, npc.width, npc.height, DustID.GemEmerald, 0f, 0f, 100, default, 1.6f);
            }
            SoundEngine.PlaySound(SoundID.NPCDeath1, npc.Center);
        }

        public override void OnKill(NPC npc)
        {
            EliminarSombrasDe(npc.whoAmI);
        }

        public static void EliminarSombrasDe(int npcWhoAmI)
        {
            foreach (Projectile proj in Main.projectile)
            {
                if (!proj.active) continue;
                if (proj.type != ModContent.ProjectileType<ReturnZeroAfterimage>()) continue;

                if (proj.ModProjectile is ReturnZeroAfterimage clon && clon.npcWhoAmI == npcWhoAmI)
                {
                    proj.Kill();
                }
            }
        }
    }
}