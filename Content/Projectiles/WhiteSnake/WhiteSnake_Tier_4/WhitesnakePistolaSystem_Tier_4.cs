using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_4
{
    public static class WhitesnakePistolaSystem_Tier_4
    {
        const int IdleFrameDelay = 9;
        const int RecoilStepDelay = 3;  // frames rápidos (8, 5 y 7)
        const int RecoilPauseDelay = 7; // pausa en el frame 6

        const float BaseBulletDamage = 80f; // escala con Stand y Ranged en Disparar()
        const float BulletSpeed = 16f;

        const float KickbackAmount = 8f;
        const float KickbackDecay = 0.5f;

        static readonly Vector2 MuzzleLocalOffset = new Vector2(0f, -14f);

        public static void UpdatePistolaAttack(
            Player player,
            Projectile projectile,
            bool clickPressed,
            ref int shootFrame,
            ref int shootAnimT,
            ref int shootRecoilStep,
            ref int shootRecoilTimer,
            ref float kickback,
            Vector2 aimDir)
        {
            kickback *= KickbackDecay;
            if (kickback < 0.05f) kickback = 0f;

            // Estado Idle (Quieto, bucle 1 al 4)
            if (shootRecoilStep == 0)
            {
                if (++shootAnimT >= IdleFrameDelay)
                {
                    shootAnimT = 0;
                    shootFrame = (shootFrame + 1) % 4; // Bucle frames 0, 1, 2, 3
                }

                if (clickPressed)
                {
                    Disparar(player, projectile, aimDir);
                    kickback = KickbackAmount;

                    // Iniciamos la animación de disparo
                    shootRecoilStep = 1;
                    shootRecoilTimer = 0;
                    shootFrame = 7; // frame 8 (Extrema derecha, índice 7)
                }

                return;
            }

            shootRecoilTimer++;

            // La pausa más larga ahora ocurre en el step 3 (cuando estamos en el frame 6)
            int currentStepDelay = shootRecoilStep == 3 ? RecoilPauseDelay : RecoilStepDelay;
            if (shootRecoilTimer < currentStepDelay) return;

            shootRecoilTimer = 0;
            shootRecoilStep++;

            // Secuencia de retroceso después del frame 8
            switch (shootRecoilStep)
            {
                case 2:
                    shootFrame = 4; // frame 5
                    break;

                case 3:
                    shootFrame = 5; // frame 6 (Aquí se aplicará el RecoilPauseDelay en el siguiente tick)
                    break;

                case 4:
                    shootFrame = 6; // frame 7
                    break;

                default:
                    // Fin del disparo, vuelve a Idle
                    shootRecoilStep = 0;
                    shootAnimT = 0;
                    shootFrame = 0; // vuelve al frame 1
                    break;
            }
        }

        static void Disparar(Player player, Projectile projectile, Vector2 aimDir)
        {
            SoundEngine.PlaySound(SoundID.Item11, projectile.Center); // sonido vanilla de la Pistola (Handgun)

            if (Main.myPlayer != projectile.owner) return;

            Vector2 dir = aimDir;
            if (dir == Vector2.Zero) dir = new Vector2(player.direction, 0);
            dir.Normalize();

            Vector2 velocity = dir * BulletSpeed;
            Vector2 muzzlePos = projectile.Center + MuzzleLocalOffset.RotatedBy(projectile.rotation);

            float dmg = BaseBulletDamage;
            dmg = player.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(dmg); // daño de Stand
            dmg = player.GetTotalDamage(DamageClass.Ranged).ApplyTo(dmg); // y daño Ranged, ambos escalan

            int index = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                muzzlePos,
                velocity,
                ProjectileID.SilverBullet, // solo visual, el daño real ya está calculado arriba
                (int)dmg,
                1f,
                projectile.owner
            );

            Main.projectile[index].DamageType = DamageClass.Summon;
            Main.projectile[index].friendly = true;
        }
    }
}