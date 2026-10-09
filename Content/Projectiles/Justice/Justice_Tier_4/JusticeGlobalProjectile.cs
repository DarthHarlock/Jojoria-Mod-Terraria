    using Terraria;
    using Terraria.ModLoader;
    using Terraria.DataStructures;

    namespace Jojo.Content.Projectiles.Justice.Justice_Tier_4
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
                        // Convertir el proyectil del monstruo poseído en un ataque aliado legítimo
                        projectile.hostile = false;
                        projectile.friendly = true;

                        // IMPORTANTE: Buscamos al jugador dueño para transferirle la propiedad del proyectil.
                        // Al hacer esto, el motor de colisiones de Terraria le permitirá dañar a otros NPCs hostiles.
                        Player owner = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)];
                        if (owner != null && owner.active)
                        {
                            projectile.owner = owner.whoAmI;
                        }
                    }
                }
            }
        }
    }