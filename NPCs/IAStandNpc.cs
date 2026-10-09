using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Jojo.Content.Items;
using Jojo.Content.NPCs.StandsNpc.StarPlatinum;
using Jojo.Content.NPCs.StandsNpc.TheWorld;

namespace Jojo.Content.NPCs.StandsNpc
{
    /// <summary>
    /// Reglas de bando e IA compartidas para NPCs con Stand.
    /// </summary>
    public static class IAStandNpc
    {
        public enum Faction
        {
            NeverFights,
            Ally,
            Hostile,
            Pacific
        }

        public static bool NeverFights(NPC n)
        {
            if (n == null) return false;
            if (n.townNPC) return false;
            if (n.type == NPCID.TargetDummy) return true;
            if (NPCID.Sets.ProjectileNPC[n.type]) return true;
            return false;
        }

        public static bool IsPassiveOwner(NPC n) => NeverFights(n);

        public static bool IsPacific(NPC n)
        {
            if (n == null) return false;
            if (NeverFights(n)) return false;
            if (n.townNPC || n.friendly) return false;
            return n.damage <= 0 && !n.boss;
        }

        public static Faction GetFaction(NPC n)
        {
            if (n == null || NeverFights(n)) return Faction.NeverFights;
            if (n.townNPC || n.friendly) return Faction.Ally;
            if (IsPacific(n)) return Faction.Pacific;
            return Faction.Hostile;
        }

        public static bool AreEnemies(NPC a, NPC b)
        {
            if (a == null || b == null) return false;

            Faction fa = GetFaction(a);
            Faction fb = GetFaction(b);

            if (fa == Faction.NeverFights || fb == Faction.NeverFights) return false;
            if (fa == fb) return false;
            if ((fa == Faction.Ally && fb == Faction.Pacific) || (fa == Faction.Pacific && fb == Faction.Ally)) return false;

            return true;
        }

        public static bool TryGetStandOwner(Projectile p, out NPC standOwner)
        {
            standOwner = null;
            if (p == null || !p.active) return false;

            if (p.ModProjectile is StarPlatinumNpcProj sp) standOwner = sp.OwnerNpc;
            else if (p.ModProjectile is TheWorldNpcProj tw) standOwner = tw.OwnerNpc;

            return standOwner != null && standOwner.active;
        }

        public static Entity FindTarget(NPC owner, Projectile projectile, float threatRange, float detectionRange)
        {
            if (owner == null || NeverFights(owner)) return null;

            bool ownerIsPacific = IsPacific(owner);

            Entity bestTarget = null;
            float closestDistance = threatRange;

            // 1) PRIORIDAD: stand del enemigo
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || p.whoAmI == projectile.whoAmI) continue;

                if (TryGetStandOwner(p, out NPC otherOwner)
                    && otherOwner.whoAmI != owner.whoAmI
                    && AreEnemies(owner, otherOwner))
                {
                    float dist = Vector2.Distance(owner.Center, p.Center);
                    if (dist < closestDistance && dist <= detectionRange)
                    {
                        closestDistance = dist;
                        bestTarget = p;
                    }
                }
            }

            if (bestTarget != null) return bestTarget;

            // 2) Jugadores
            if (!ownerIsPacific)
            {
                GlobalStandSpawner ownerGlobal = owner.GetGlobalNPC<GlobalStandSpawner>();

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p.active && !p.dead)
                    {
                        // Se considera blanco si:
                        // 1. El dueño es hostil (no es friendly)
                        // 2. El jugador lleva NpcKiller equipado
                        // 3. El jugador es el agresor directo registrado en el GlobalNPC
                        bool isAggressor = ownerGlobal != null && ownerGlobal.currentAggressor == p;
                        bool canTargetPlayer = !owner.friendly || p.GetModPlayer<NpcKillerPlayer>().canKillTownNPCs || isAggressor;

                        if (canTargetPlayer)
                        {
                            float dist = Vector2.Distance(owner.Center, p.Center);
                            if (dist < closestDistance)
                            {
                                closestDistance = dist;
                                bestTarget = p;
                            }
                        }
                    }
                }
            }

            if (bestTarget != null) return bestTarget;

            // 3) NPCs enemigos
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n.active && n.life > 0 && !n.dontTakeDamage && n.whoAmI != owner.whoAmI)
                {
                    if (AreEnemies(owner, n))
                    {
                        float dist = Vector2.Distance(owner.Center, n.Center);
                        if (dist < closestDistance)
                        {
                            closestDistance = dist;
                            bestTarget = n;
                        }
                    }
                }
            }
            return bestTarget;
        }
    }
}