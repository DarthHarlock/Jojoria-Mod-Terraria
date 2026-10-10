using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Players
{
    public class StandStatsPlayer : ModPlayer
    {
        public float standSpeed = 0f;

        // Reducciones exclusivas por habilidad
        public float standCooldown1Reduction = 0f;
        public float standCooldown2Reduction = 0f;
        public float standCooldown3Reduction = 0f;

        // --- NUEVO: referencia al proyectil del Stand actualmente activo ---
        // No se resetea en ResetEffects porque necesita persistir entre ticks.
        public Projectile activeStand;

        public override void ResetEffects()
        {
            standSpeed = 0f;
            standCooldown1Reduction = 0f;
            standCooldown2Reduction = 0f;
            standCooldown3Reduction = 0f;
        }

        public float GetClampedSpeed()
        {
            if (standSpeed >= 100f)
                return 100f;

            return standSpeed;
        }
    }
}