using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Jojo.Content.Habilidades;
using Jojo.Content.Systems;
using System.Collections.Generic;

namespace Jojo.Content.NPCs.StandsNpc.TheWorld
{
    public class TimeStopAreaProj : ModProjectile
    {
        public override string Texture => "Terraria/Images/Item_0";

        public float radius = 350f; // Ajustado a 350 para coincidir con tu aura SCR

        // Diccionarios para rastrear el tiempo exacto que cada entidad pasa dentro del área
        private Dictionary<int, int> playerTicksInside = new Dictionary<int, int>();
        private Dictionary<int, int> npcTicksInside = new Dictionary<int, int>();

        public override void SetDefaults()
        {
            Projectile.width = (int)(radius * 2);
            Projectile.height = (int)(radius * 2);
            Projectile.timeLeft = 120; // 2 segundos (60 ticks * 2)
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.alpha = 255;
            Projectile.hide = true;
        }

        public override bool? CanCutTiles() => false;

        private TheWorldNpcProj GetParentStand()
        {
            int parentIndex = (int)Projectile.ai[0];
            if (parentIndex < 0 || parentIndex >= Main.maxProjectiles) return null;

            Projectile parent = Main.projectile[parentIndex];
            if (parent == null || !parent.active) return null;
            if (parent.type != ModContent.ProjectileType<TheWorldNpcProj>()) return null;

            return parent.ModProjectile as TheWorldNpcProj;
        }

        public override void AI()
        {
            TheWorldNpcProj standProj = GetParentStand();

            if (standProj == null || standProj.OwnerNpc == null)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = standProj.Projectile.Center;

            if (TimeStopSystem.timeStopped)
            {
                Projectile.Kill();
                return;
            }

            // Efectos visuales del aura (Estilo SCR pero en Amarillo/Dorado The World)
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                Dust ringDust = Dust.NewDustPerfect(Projectile.Center + offset, 43, Vector2.Zero, 150, Color.Yellow, 1.5f);
                ringDust.noGravity = true;
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(radius, radius);
                Dust innerDust = Dust.NewDustPerfect(Projectile.Center + offset, 57, new Vector2(0f, -1.5f), 100, Color.Gold, 1.2f);
                innerDust.noGravity = true;
            }

            // Lógica de rastreo: Verificar quién está dentro en este tick
            float radiusSq = radius * radius;
            bool ownerIsHostile = !standProj.OwnerNpc.friendly && !standProj.OwnerNpc.townNPC;

            if (ownerIsHostile)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p.active && !p.dead && Vector2.DistanceSquared(Projectile.Center, p.Center) <= radiusSq)
                    {
                        if (!playerTicksInside.ContainsKey(i)) playerTicksInside[i] = 0;
                        playerTicksInside[i]++;
                    }
                }
            }
            else
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.active && !n.friendly && n.damage > 0 && Vector2.DistanceSquared(Projectile.Center, n.Center) <= radiusSq)
                    {
                        if (!npcTicksInside.ContainsKey(i)) npcTicksInside[i] = 0;
                        npcTicksInside[i]++;
                    }
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            // Solo actuar si expiró naturalmente por tiempo
            if (timeLeft > 1) return;

            // Solo el servidor o el jugador en SinglePlayer debería lanzar el comando de TimeStop
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            TheWorldNpcProj standProj = GetParentStand();
            if (standProj == null) return;

            NPC ownerNpc = standProj.OwnerNpc;
            if (ownerNpc == null || !ownerNpc.active) return;

            bool caughtTarget = false;
            bool ownerIsHostile = !ownerNpc.friendly && !ownerNpc.townNPC;

            // Requerimos que la entidad haya estado dentro al menos 100 ticks (aprox 1.6 segundos)
            // Esto da el margen de "esquivar" si escapan rápido del círculo.
            int requiredTicks = 100;

            if (ownerIsHostile)
            {
                foreach (var kvp in playerTicksInside)
                {
                    if (kvp.Value >= requiredTicks)
                    {
                        caughtTarget = true;
                        break;
                    }
                }
            }
            else
            {
                foreach (var kvp in npcTicksInside)
                {
                    if (kvp.Value >= requiredTicks)
                    {
                        caughtTarget = true;
                        break;
                    }
                }
            }

            if (caughtTarget)
            {
                standProj.ActivarEnfriamientoTimeStop();
                TimeStop_TW_NPC.Use(ownerNpc, standProj.Projectile);
            }
        }
    }
}