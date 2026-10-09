// Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Requiem/ReturnZero/ReturnZeroEffects.cs
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem.ReturnZero
{
    public static class ReturnZeroEffects
    {
        public static int cantidadClones = 12;
        const int MIN_CLONES_POR_SEGMENTO = 3;

        static readonly SoundStyle SonidoRewind = SoundID.Item29 with { Pitch = -0.4f, Volume = 0.8f };

        public static void EjecutarEfecto(Player player, List<NPC> segmentos)
        {
            if (segmentos == null || segmentos.Count == 0) return;

            SoundEngine.PlaySound(SonidoRewind, segmentos[0].Center);

            // Si el grupo comparte un único pool de vida (gusanos como el Destructor),
            // solo el segmento "real" inflige daño; el resto rebobina de forma puramente visual.
            // Si no comparten vida (p. ej. puños/manos, agrupados solo para sincronizar el cooldown),
            // cada uno conserva su comportamiento original de dañar por su cuenta.
            bool vidaCompartida = ReturnZeroGlobalNPC.EsVidaCompartida(segmentos);
            NPC representante = vidaCompartida ? ReturnZeroGlobalNPC.ObtenerRepresentanteDeVida(segmentos) : null;

            int clonesPorSegmento = segmentos.Count > 1
                ? System.Math.Max(MIN_CLONES_POR_SEGMENTO, cantidadClones / segmentos.Count)
                : cantidadClones;

            foreach (NPC segmento in segmentos)
            {
                var data = segmento.GetGlobalNPC<ReturnZeroGlobalNPC>();
                List<NpcSnapshot> ruta = data.IniciarRewind(segmento);
                if (ruta.Count < 2) continue;

                bool esteSegmentoDaña = !vidaCompartida || segmento.whoAmI == representante.whoAmI;

                SpawnClones(segmento, ruta, clonesPorSegmento, esteSegmentoDaña);
            }
        }

        static void SpawnClones(NPC npc, List<NpcSnapshot> ruta, int cantidad, bool puedeDañar)
        {
            if (cantidad <= 1)
            {
                SpawnClonUnico(npc, ruta[0], puedeDañar);
                return;
            }

            for (int i = 0; i < cantidad; i++)
            {
                float t = i / (float)(cantidad - 1);
                int idx = (int)((ruta.Count - 1) * t);
                SpawnClonUnico(npc, ruta[idx], puedeDañar);
            }
        }

        static void SpawnClonUnico(NPC npc, NpcSnapshot snap, bool puedeDañar)
        {
            Projectile proj = Projectile.NewProjectileDirect(
                npc.GetSource_FromThis(),
                snap.Position,
                Vector2.Zero,
                ModContent.ProjectileType<ReturnZeroAfterimage>(),
                0,
                0f,
                Main.myPlayer
            );

            if (proj.ModProjectile is ReturnZeroAfterimage clon)
            {
                clon.npcType = npc.type;
                clon.frame = snap.Frame;
                clon.direccion = snap.SpriteDirection;
                clon.rotacion = snap.Rotation;
                clon.escalaNpc = npc.scale;
                clon.npcWhoAmI = npc.whoAmI;
                clon.puedeDañar = puedeDañar;
            }
        }
    }
}