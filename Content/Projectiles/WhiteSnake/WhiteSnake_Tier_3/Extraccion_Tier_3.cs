using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.WhiteSnake_Buffs;
using System.Collections.Generic;

namespace Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_3
{
    public static class ExtraccionLogica
    {
        public enum EstadoExtraccion { Cancelado, Buscando, Acercandose, Extrayendo }

        const float VelocidadAcercamiento = 20f;
        const float UmbralLlegada = 10f;

        public static EstadoExtraccion Actualizar(Projectile stand, Player player, ref NPC target, float rangoMaximo, bool yaEnganchado)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                if (!Main.mouseRight || Main.mouseLeft) return EstadoExtraccion.Cancelado;
            }

            Vector2 cursor = Main.MouseWorld;
            bool targetCambio = false;

            // ARREGLO 1: Comprobar también el cooldown del target actual por si es un gusano cuyo líder tiene cooldown.
            bool targetEnCooldown = false;
            if (target != null && target.active)
            {
                targetEnCooldown = target.HasBuff(ModContent.BuffType<AmnesiaDebuff>()) ||
                                   target.HasBuff(ModContent.BuffType<AmnesiaCooldown>());

                // Verificamos al líder si es que el target es un segmento
                if (target.realLife >= 0 && target.realLife < Main.maxNPCs)
                {
                    NPC liderTarget = Main.npc[target.realLife];
                    targetEnCooldown = targetEnCooldown ||
                                       liderTarget.HasBuff(ModContent.BuffType<AmnesiaDebuff>()) ||
                                       liderTarget.HasBuff(ModContent.BuffType<AmnesiaCooldown>());
                }
            }

            if (target == null || !target.active || targetEnCooldown)
            {
                target = BuscarEnemigoEnCursor(cursor);
                targetCambio = true;
            }

            if (target != null)
            {
                if (Vector2.Distance(player.Center, target.Center) > rangoMaximo + 100f)
                {
                    return EstadoExtraccion.Cancelado;
                }

                stand.velocity = Vector2.Zero;
                stand.rotation = 0f;

                if (yaEnganchado && !targetCambio)
                {
                    stand.Center = target.Center;
                    return EstadoExtraccion.Extrayendo;
                }

                Vector2 haciaObjetivo = target.Center - stand.Center;
                float distancia = haciaObjetivo.Length();

                if (distancia <= UmbralLlegada)
                {
                    stand.Center = target.Center;
                    return EstadoExtraccion.Extrayendo;
                }

                Vector2 direccion = haciaObjetivo / distancia;
                float avance = MathHelper.Min(VelocidadAcercamiento, distancia);
                stand.Center += direccion * avance;

                return EstadoExtraccion.Acercandose;
            }
            else
            {
                Vector2 dir = cursor - player.Center;
                if (dir.Length() > rangoMaximo)
                {
                    cursor = player.Center + Vector2.Normalize(dir) * rangoMaximo;
                }

                stand.Center = Vector2.Lerp(stand.Center, cursor, 0.15f);
                stand.rotation = 0f;

                return EstadoExtraccion.Buscando;
            }
        }

        private static NPC BuscarEnemigoEnCursor(Vector2 cursor)
        {
            int amnesiaType = ModContent.BuffType<AmnesiaDebuff>();
            int cooldownType = ModContent.BuffType<AmnesiaCooldown>();

            NPC objetivoMasCercano = null;
            float distanciaMinima = 40f;

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && !npc.dontTakeDamage && npc.life > 0)
                {
                    // ARREGLO 2: La búsqueda del cursor ahora esquiva los segmentos si su "líder" tiene cooldown.
                    bool enCooldown = npc.HasBuff(amnesiaType) || npc.HasBuff(cooldownType);
                    if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs)
                    {
                        NPC lider = Main.npc[npc.realLife];
                        enCooldown = enCooldown || lider.HasBuff(amnesiaType) || lider.HasBuff(cooldownType);
                    }

                    if (enCooldown) continue;

                    float distanciaActual = Vector2.Distance(cursor, npc.Center);

                    if (npc.Hitbox.Contains(cursor.ToPoint())) return npc;

                    if (distanciaActual < distanciaMinima)
                    {
                        distanciaMinima = distanciaActual;
                        objetivoMasCercano = npc;
                    }
                }
            }
            return objetivoMasCercano;
        }

        public static void AplicarAmnesia(NPC npc)
        {
            int debuffType = ModContent.BuffType<AmnesiaDebuff>();
            int cooldownType = ModContent.BuffType<AmnesiaCooldown>();

            const int Segundos = 60;

            // ARREGLO 3: Determinamos si el NPC es un jefe, o si su LÍDER es un jefe.
            bool esJefe = npc.boss || (npc.realLife >= 0 && Main.npc[npc.realLife].boss);

            int duracionSueno = esJefe ?
                6 * Segundos :  // Boss dormido
                9 * Segundos;  // Enemigo dormido

            int duracionCooldown = esJefe ?
                28 * Segundos : // Boss dormido COOLDOWN 
                24 * Segundos;  // Enemigo normal COOLDOWN

            // Gracias a tu método 'ObtenerSegmentosGusano', si 'esJefe' es true,
            // CADA fragmento del gusano se comerá los 60 segundos completos de cooldown.
            List<NPC> segmentos = AmnesiaGlobalNPC.ObtenerSegmentosGusano(npc);
            foreach (NPC seg in segmentos)
            {
                seg.AddBuff(debuffType, duracionSueno);
                seg.AddBuff(cooldownType, duracionCooldown);
            }
        }
    }
}