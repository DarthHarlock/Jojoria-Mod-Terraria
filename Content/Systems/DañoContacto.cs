using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Systems
{
    // REGLA ANTIGUA ELIMINADA:
    // Antes cancelaba el daño de todos los stands cuando el JcJ estaba activo y
    // no se pulsaba el clic izquierdo (player.hostile && !Main.mouseLeft).
    // Eso rompía el modo AutoStand con JcJ activo, y también los stands de otros
    // jugadores, porque Main.mouseLeft solo refleja el ratón del cliente local.
    //
    // Ya no hace falta:
    //  - Cada stand pone Projectile.friendly = canAttack, así que solo hace daño
    //    mientras ataca.
    //  - CanHitPvp / CanHitPlayer devuelven false, así que un stand nunca daña a
    //    jugadores, ni por contacto ni de ninguna otra forma.
    //
    // La clase se deja vacía a propósito para no romper referencias existentes.
    public class DañoContacto : GlobalProjectile
    {
    }
}