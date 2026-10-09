using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Projectiles.StandsNpc;

namespace Jojo.Content.NPCs.StandsNpc
{
    public class StandBossDamageNerf : GlobalProjectile
    {
        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            // Los stands que heredan de BaseStandNpcProjectile ya se nerfean ahí: evitamos el doble nerf.
            if (projectile.ModProjectile is BaseStandNpcProjectile) return;

            bool esStandDeNpc = projectile.ModProjectile is IStandNpcProjectile
                             || IAStandNpc.TryGetStandOwner(projectile, out _);
            if (!esStandDeNpc) return;

            // Mismo criterio: si hay un boss vivo, el stand pega al 10%.
            if (BaseStandNpcProjectile.HayBossVivo())
                modifiers.FinalDamage *= 0.10f;
        }
    }
}