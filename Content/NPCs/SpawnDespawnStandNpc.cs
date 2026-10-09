using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;

namespace Jojo.Content.NPCs.StandsNpc
{
    /// <summary>
    /// Utilidades compartidas para la aparición (spawn), regreso a posición
    /// de reposo (idle) y desaparición (despawn) de los proyectiles de stand.
    /// No contiene lógica de combate; solo movimiento/transparencia.
    /// </summary>
    public static class SpawnDespawnStandNpc
    {
        /// <summary>
        /// Reproduce el sonido de invocación la primera vez que el proyectil
        /// del stand se actualiza. Usa <paramref name="init"/> como bandera
        /// para no repetir el sonido en frames posteriores.
        /// </summary>
        public static void InitializeSpawn(Projectile projectile, ref bool init, SoundStyle spawnSound)
        {
            if (!init)
            {
                SoundEngine.PlaySound(spawnSound, projectile.Center);
                init = true;
            }
        }

        /// <summary>
        /// Anima la desaparición del stand: lo mueve hacia el dueño mientras
        /// aumenta su transparencia (alpha) rápidamente, y lo destruye
        /// (Projectile.Kill) en cuanto llega a alpha máximo.
        /// </summary>
        /// <returns>true si el proyectil ya terminó de desaparecer (y fue destruido).</returns>
        public static bool ProcessDespawn(Projectile projectile, NPC owner, float despawnMoveSpeed)
        {
            projectile.Center = Vector2.Lerp(projectile.Center, owner.Center, despawnMoveSpeed);

            // Aumenta el alpha muy rápido para que se vuelva transparente en menos de medio segundo
            projectile.alpha += 30;
            if (projectile.alpha >= 255)
            {
                projectile.alpha = 255;
                projectile.Kill();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Devuelve el stand a su posición de reposo junto al dueño (con un
        /// pequeño desfase según la dirección en la que mira el dueño) y
        /// resetea el temporizador de ataque.
        /// </summary>
        public static void ReturnToIdle(Projectile projectile, NPC owner, float idleFollowSpeed, ref int attackTimer)
        {
            attackTimer = 0;
            projectile.rotation = 0f;
            Vector2 idleOffset = new Vector2(-owner.direction * 40, -10);
            Vector2 targetPosition = owner.Center + idleOffset;
            projectile.Center = Vector2.Lerp(projectile.Center, targetPosition, idleFollowSpeed);
        }
    }
}