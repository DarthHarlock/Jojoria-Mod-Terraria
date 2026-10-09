using Terraria;
using Terraria.ID;
using Terraria.Audio;
using Terraria.ModLoader;
using Jojo.Content.Buffs.GoldenExperience_Buffs;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_2
{
    public static class CuracionManager
    {
        // Sonido VANILLA: el chime de reforjar en el yunque.
        static readonly SoundStyle SonidoCuracion = SoundID.Item4 with { Volume = 0.7f };

        // --- SOLO LA PARTE "VISTOSA" (sonido + partículas) ---
        public static void ReproducirEfectosVisuales(Player p)
        {
            SoundEngine.PlaySound(SonidoCuracion, p.Center);

            // Subido de 20 a 45 partículas. Cambiado a CursedTorch para un efecto mágico VERDE brillante.
            for (int i = 0; i < 45; i++)
            {
                int polvo = Dust.NewDust(p.position, p.width, p.height, DustID.CursedTorch, 0f, 0f, 100, default, 1.5f);
                Main.dust[polvo].noGravity = true;
                Main.dust[polvo].velocity *= 1.2f;
            }
        }

        // --- CURACIÓN REAL (vida + buff) ---
        public static void AplicarCuracion(Player p, float cantidadVida, int duracionCooldown)
        {
            if (p.whoAmI != Main.myPlayer) return;

            int curacionFinal = (int)cantidadVida;
            p.statLife = System.Math.Min(p.statLifeMax2, p.statLife + curacionFinal);
            p.HealEffect(curacionFinal);

            // Aplicamos el cooldown compartido
            p.AddBuff(ModContent.BuffType<Curacion>(), duracionCooldown);

            // El dueño reproduce sus propios efectos visuales aquí mismo
            ReproducirEfectosVisuales(p);
        }
    }
}