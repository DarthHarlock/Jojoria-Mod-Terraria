using Microsoft.Xna.Framework;
using Terraria;

namespace Jojo.Content.NPCs.StandsNpc
{
    /// <summary>
    /// Lógica de rotación para los Stands de NPC.
    /// Replica el comportamiento de los Stands de jugador (ej. RIKASTAND):
    /// el stand rota para "mirar" hacia su objetivo mientras ataca y el offset
    /// supera el radio interior, y se aplana (rotation = 0) si el objetivo está
    /// muy cerca o el stand no está atacando.
    /// </summary>
    public static class StandNpcRotationSystem
    {
        /// <summary>
        /// Calcula la rotación en base al offset hacia el objetivo (ya clamped
        /// al radio de ataque, igual que "off" en los stands de jugador).
        /// </summary>
        /// <param name="isAttacking">Si el stand está en estado de ataque.</param>
        /// <param name="offsetToTarget">Vector objetivo - dueño (o dueño - objetivo, según tu convención), clamped al radio de ataque.</param>
        /// <param name="innerRadius">Radio por debajo del cual no tiene sentido rotar (objetivo pegado al stand).</param>
        public static float GetRotation(bool isAttacking, Vector2 offsetToTarget, float innerRadius)
        {
            if (!isAttacking)
                return 0f;

            if (offsetToTarget.Length() <= innerRadius)
                return 0f;

            float rot = offsetToTarget.ToRotation();

            // Mismo ajuste que en los stands de jugador: si el objetivo está
            // a la izquierda, se compensa +180° porque el sprite se dibuja
            // "flipeado" en ese lado (ver standFacing / SpriteEffects en PreDraw).
            if (offsetToTarget.X < 0)
                rot += MathHelper.Pi;

            return rot;
        }

        /// <summary>
        /// Aplica la rotación calculada directamente al proyectil del Stand.
        /// Llamar esto una vez por tick, después de tener el offset final hacia el objetivo.
        /// </summary>
        public static void ApplyRotation(Projectile standProjectile, bool isAttacking, Vector2 offsetToTarget, float innerRadius)
        {
            standProjectile.rotation = GetRotation(isAttacking, offsetToTarget, innerRadius);
        }
    }
}