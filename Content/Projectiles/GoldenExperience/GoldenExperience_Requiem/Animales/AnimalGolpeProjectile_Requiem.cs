using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem.Animales
{
    // =========================================================================
    // Proyectil "hitbox invisible" compartido por Pájaro, Rana y Mariposa de
    // Requiem. Existe solo para canalizar el daño a través del sistema de
    // colisión/red normal de Terraria (el mismo mecanismo que usa el golpe
    // del clon D4C Melee y la Mariposa de Tier 4), en vez de aplicar el daño
    // directo desde código, que no se sincroniza a los clientes.
    // =========================================================================
    public class AnimalGolpeProjectile_Requiem : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_1";

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;       // solo golpea a un único enemigo
            Projectile.timeLeft = 3;        // vive lo justo para registrar el impacto
            Projectile.hide = true;         // es un "hitbox invisible", no se dibuja
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.ArmorPenetration = 1000;
            Projectile.knockBack = 0f;
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}