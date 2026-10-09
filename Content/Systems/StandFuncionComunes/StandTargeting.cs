using Microsoft.Xna.Framework;
using Terraria;
using Jojo.Content.Players;
using Jojo.Content.Items.Potenciadores; // <-- Añadido para encontrar RangoStandPlayer
using Jojo.Content.Systems;             // <-- Añadido por seguridad

namespace Jojo.Systems.StandFuncionComunes
{
    public static class StandTargeting
    {
        public static NPC FindHostileEnemy(Player p, float autoR)
        {
            NPC best = null;
            float max = autoR;

            // Busca el multiplicador de rango
            if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
            {
                max *= rangoPlayer.multiplicadorRango;
            }

            foreach (NPC n in Main.npc)
            {
                // FILTRO ESTRICTO PARA SOLO HOSTILES:
                // n.active: El NPC existe en el mundo
                // n.CanBeChasedBy(): Filtro interno de Terraria (ignora inmortales y fantasmas)
                // !n.friendly y !n.townNPC: Ignora aldeanos, mascotas y aliados
                // n.lifeMax > 5: Ignora a los animales (conejos, pájaros, pingüinos, etc.)
                // (n.damage > 0 || n.boss): Ignora los Muñecos de Prueba (Target Dummies) que tienen 0 daño, pero nunca ignora a un Jefe.

                if (n.active && n.CanBeChasedBy() && !n.friendly && !n.townNPC && n.lifeMax > 5 && (n.damage > 0 || n.boss))
                {
                    float d = Vector2.Distance(p.Center, n.Center);
                    if (d < max)
                    {
                        max = d;
                        best = n;
                    }
                }
            }

            return best;
        }
    }
}