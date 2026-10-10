using Terraria;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace Jojo.Content.Projectiles.Justice.Justice_Tier_4.ControlMental
{
    public class JusticeGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (source is EntitySource_Parent parentSource && parentSource.Entity is NPC npc)
            {
                var globalNPC = npc.GetGlobalNPC<JusticeGlobalNPC>();
                if (globalNPC.bajoControlMental)
                {
                    // Convertir el proyectil del monstruo poseído en un ataque aliado
                    projectile.hostile = false;
                    projectile.friendly = true;

                    // Transferir la propiedad al dueño real del minion (no al jugador más cercano)
                    int dueno = globalNPC.duenoIndex;
                    if (dueno >= 0 && dueno < Main.maxPlayers && Main.player[dueno].active)
                    {
                        projectile.owner = dueno;
                    }
                }
            }
        }
    }
}