using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_4.Vomito;
using Jojo.Content.Clases; //  IMPORTANTE: Necesitamos esto para llamar a la ClaseStand

namespace Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_4
{
    public class WhitesnakeVomitSystem_Tier_4 : ModSystem
    {
        public static void UpdateVomitAttack(Player player, Projectile projectile, ref int vomitTimer, ref int vomitFrame, out Vector2 vomitAimDir)
        {
            vomitTimer++;

            // Calculamos la dirección y nos protegemos del (0,0) ANTES de normalizar
            vomitAimDir = Main.MouseWorld - player.Center;
            if (vomitAimDir == Vector2.Zero)
            {
                vomitAimDir = new Vector2(player.direction, 0);
            }

            if (vomitTimer % 10 == 0)
            {
                vomitFrame++;
                if (vomitFrame > 4)
                {
                    vomitFrame = 0;
                }
            }

            if (Main.myPlayer == projectile.owner && vomitTimer % 6 == 0)
            {
                int numeroDeGotas = Main.rand.Next(3, 6);

                //  AQUÍ CONFIGURAS EL DAÑO INDEPENDIENTE DE LAS GOTAS 
                int danoBaseGotas = 10; // Modifica este número para cambiar el daño base del vómito

                // QUÍ LE APLICAMOS LOS BUFFOS DE TUS ACCESORIOS STAND AL DAÑO BASE 
                int danoGotasBuffado = (int)player.GetDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(danoBaseGotas);

                for (int i = 0; i < numeroDeGotas; i++)
                {
                    float velocidadAleatoria = Main.rand.NextFloat(6f, 14f);

                    // Ahora es 100% seguro hacer Normalize porque ya aseguramos que nunca es Zero
                    Vector2 velocity = Vector2.Normalize(vomitAimDir) * velocidadAleatoria;

                    velocity = velocity.RotatedByRandom(MathHelper.ToRadians(30));

                    if (Main.rand.NextBool(2))
                    {
                        int d = Dust.NewDust(projectile.Center + (Vector2.Normalize(vomitAimDir) * 15f), 10, 10, DustID.Web, velocity.X * 0.1f, velocity.Y * 0.1f, 100, Color.White, Main.rand.NextFloat(0.8f, 1.2f));
                        Main.dust[d].noGravity = true;
                        Main.dust[d].velocity *= 0.2f;
                    }

                    int proj = Projectile.NewProjectile(
                        projectile.GetSource_FromThis(),
                        projectile.Center,
                        velocity,
                        ModContent.ProjectileType<Whitesnake_GotaVomito_Tier_4>(),
                        danoGotasBuffado, //  AHORA LE PASAMOS EL DAÑO INDEPENDIENTE CON LOS BUFFOS APLICADOS
                        projectile.knockBack,
                        projectile.owner
                    );

                    Main.projectile[proj].timeLeft = Main.rand.Next(40, 90);
                }
            }
        }
    }
}