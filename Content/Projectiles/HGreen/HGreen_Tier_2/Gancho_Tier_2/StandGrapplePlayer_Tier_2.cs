using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_2;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_2.Gancho_Tier_2;

namespace Jojo.Content.Projectiles.HGreen.HGreen_Tier_2.Gancho_Tier_2
{
    // AHORA LA CLASE TIENE EL NOMBRE CORRECTO
    public class StandGrapplePlayer_Tier_2 : ModPlayer
    {
        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (PlayerInput.Triggers.JustPressed.Grapple)
            {
                bool isStand4Active = Player.ownedProjectileCounts[ModContent.ProjectileType<HGREENSTAND_Tier_2>()] > 0;

                if (isStand4Active)
                {
                    bool hasVanillaGrapple = false;

                    if (!Player.miscEquips[4].IsAir)
                    {
                        hasVanillaGrapple = true;
                    }
                    else
                    {
                        for (int i = 0; i < 50; i++)
                        {
                            Item item = Player.inventory[i];
                            if (!item.IsAir && item.shoot > 0 && Main.projHook[item.shoot])
                            {
                                hasVanillaGrapple = true;
                                break;
                            }
                        }
                    }

                    if (!hasVanillaGrapple)
                    {
                        int hookType = ModContent.ProjectileType<Gancho_Tier_2_Cabeza>();
                        int hooksOut = Player.ownedProjectileCounts[hookType];

                        // --- FIX: si ya tienes un gancho tuyo activo (enganchado o volando), lo matamos ---
                        if (hooksOut >= Gancho_Tier_2_Cabeza.NumeroDeGanchos)
                        {
                            for (int i = 0; i < Main.maxProjectiles; i++)
                            {
                                Projectile proj = Main.projectile[i];
                                if (proj.active && proj.owner == Player.whoAmI && proj.type == hookType)
                                {
                                    proj.Kill();
                                }
                            }

                            // --- FIX CLAVE: limpiar el array interno de "enganchado" del jugador ---
                            // Sin esto, Terraria reutiliza el MISMO índice de proyectil que acabamos
                            // de matar para el gancho nuevo, y como Player.grappling[] sigue apuntando
                            // a ese índice, el juego cree que SIGUES enganchado desde el frame 0 aunque
                            // el nuevo gancho todavía no ha volado ni tocado nada. Por eso el personaje
                            // no se mueve al relanzar rápido: nunca hay un frame real de "soltado".
                            for (int i = 0; i < Player.grappling.Length; i++)
                            {
                                Player.grappling[i] = -1;
                            }
                            Player.grapCount = 0;
                        }

                        // Dirección hacia el ratón (con fallback por si el ratón está justo encima del jugador)
                        Vector2 velocity = Main.MouseWorld - Player.MountedCenter;
                        if (velocity == Vector2.Zero)
                        {
                            velocity = new Vector2(Player.direction, 0f);
                        }
                        velocity.Normalize();

                        // AQUÍ TOMA LA VELOCIDAD DE DISPARO QUE PUSISTE EN LA CONFIGURACIÓN
                        velocity *= Gancho_Tier_2_Cabeza.VelocidadDisparo;

                        Projectile.NewProjectile(
                            Player.GetSource_Misc("StandGrapple"),
                            Player.MountedCenter,
                            velocity,
                            hookType,
                            0,
                            0f,
                            Player.whoAmI
                        );
                    }
                }
            }
        }
    }
}