using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using System.Collections.Generic;

namespace Jojo.Content.Habilidades
{
    public class OrdenCuchillos
    {
        public int Timer;
        public int RafagasRestantes;
        public int DelayEntreRafagas;
        public int CuchillosPorRafaga;
        public float AnguloSeparacion;
        public float Velocidad;
        public int TipoProyectil;
        public int Daño;
        public float Knockback;
    }

    public class SistemaCuchillos : ModPlayer
    {
        private List<OrdenCuchillos> ordenesActivas = new();

        public static void Lanzar(Player player, int tipoProyectil, int daño, int cuchillosPorRafaga = 3, int totalRafagas = 3, int delayEntreRafagas = 15, float anguloSeparacion = 6f, float velocidad = 14f, float knockback = 2f)
        {
            player.direction = (Main.MouseWorld.X > player.Center.X) ? 1 : -1;
            player.ChangeDir(player.direction);
            player.itemAnimation = 12;
            player.itemTime = 12;

            SoundEngine.PlaySound(SoundID.Item1, player.Center);

            var sistema = player.GetModPlayer<SistemaCuchillos>();

            OrdenCuchillos nuevaOrden = new()
            {
                Timer = 0,
                RafagasRestantes = totalRafagas,
                DelayEntreRafagas = delayEntreRafagas,
                CuchillosPorRafaga = cuchillosPorRafaga,
                AnguloSeparacion = anguloSeparacion,
                Velocidad = velocidad,
                TipoProyectil = tipoProyectil,
                Daño = daño,
                Knockback = knockback
            };

            DispararRafaga(player, nuevaOrden);
            nuevaOrden.RafagasRestantes--;

            if (nuevaOrden.RafagasRestantes > 0)
            {
                nuevaOrden.Timer = delayEntreRafagas;
                sistema.ordenesActivas.Add(nuevaOrden);
            }
        }

        public override void PostUpdate()
        {
            if (ordenesActivas.Count == 0) return;

            for (int i = ordenesActivas.Count - 1; i >= 0; i--)
            {
                var orden = ordenesActivas[i];
                orden.Timer--;

                if (orden.Timer <= 0)
                {
                    DispararRafaga(Player, orden);
                    orden.RafagasRestantes--;

                    if (orden.RafagasRestantes <= 0)
                    {
                        ordenesActivas.RemoveAt(i);
                    }
                    else
                    {
                        orden.Timer = orden.DelayEntreRafagas;
                    }
                }
            }
        }

        private static void DispararRafaga(Player player, OrdenCuchillos orden)
        {
            Vector2 dir = Main.MouseWorld - player.Center;
            if (dir.LengthSquared() < 0.01f)
                dir = player.direction == 1 ? Vector2.UnitX : -Vector2.UnitX;
            dir.Normalize();

            float anguloInicial = -((orden.CuchillosPorRafaga - 1) * orden.AnguloSeparacion) / 2f;

            for (int i = 0; i < orden.CuchillosPorRafaga; i++)
            {
                float anguloActual = anguloInicial + (i * orden.AnguloSeparacion);
                Vector2 velocidad = dir.RotatedBy(MathHelper.ToRadians(anguloActual)) * orden.Velocidad;

                Projectile.NewProjectile(
                    player.GetSource_FromThis(),
                    player.Center,
                    velocidad,
                    orden.TipoProyectil,
                    orden.Daño,
                    orden.Knockback,
                    player.whoAmI
                );
            }

            SoundEngine.PlaySound(SoundID.Item1, player.Center);
        }
    }
}