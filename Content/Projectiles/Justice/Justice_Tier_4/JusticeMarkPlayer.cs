using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace Jojo.Content.Projectiles.Justice.Justice_Tier_4
{
    public class JusticeMarkPlayer : ModPlayer
    {
        public override void PostUpdate()
        {
            // Solo ejecutar en el cliente del jugador local
            if (Player.whoAmI != Main.myPlayer) return;

            // Comprobar si el jugador tiene activo a Justice Tier 4
            bool hasJusticeActive = false;
            foreach (Projectile proj in Main.projectile)
            {
                if (proj.active && proj.owner == Player.whoAmI && proj.type == ModContent.ProjectileType<JUSTICESTAND_Tier_4>())
                {
                    hasJusticeActive = true;
                    break;
                }
            }

            if (!hasJusticeActive) return;

            // Detectar el clic derecho
            if (Main.mouseRight && Main.mouseRightRelease)
            {
                Vector2 mousePos = Main.MouseWorld;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && !npc.friendly && !npc.dontTakeDamage && npc.lifeMax > 5)
                    {
                        JusticeGlobalNPC jNpc = npc.GetGlobalNPC<JusticeGlobalNPC>();

                        // Si el ratón está sobre el NPC y este NO está bajo control mental
                        if (!jNpc.bajoControlMental && npc.Hitbox.Contains(mousePos.ToPoint()))
                        {
                            // Limpiar cualquier otra marca existente en el mapa (solo 1 objetivo a la vez)
                            for (int j = 0; j < Main.maxNPCs; j++)
                            {
                                if (Main.npc[j].active)
                                {
                                    Main.npc[j].GetGlobalNPC<JusticeGlobalNPC>().justiceMarkTimer = 0;
                                }
                            }

                            // Aplicar la marca por 4 segundos (240 ticks a 60 FPS)
                            jNpc.justiceMarkTimer = 240;
                            break;
                        }
                    }
                }
            }
        }
    }
}