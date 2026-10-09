using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Buffs.WeatherReport_Buffs;

namespace Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_4.Arcoiris_Tier_4
{
    public class ArcoirisEfectoCaracol_GlobalNPC : GlobalNPC
    {
        // Instanciamos esto por cada enemigo, aunque no guardamos variables globales esta vez,
        // es una buena práctica por si en el futuro necesitas añadirle algún estado al NPC.
        public override bool InstancePerEntity => true;

        // 1. INMUNIDAD TOTAL PARA LOS CARACOLES (Corregido a bool?)
        public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
        {
            if (npc.type == NPCID.Snail)
            {
                if (EsArcoirisWeatherReport(projectile))
                {
                    return false; // El caracol vainilla es absolutamente inmune a este ataque
                }
            }
            return base.CanBeHitByProjectile(npc, projectile);
        }

        // 2. LÓGICA DE INSTA-KILL (40% enemigos, 5% Bosses)
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (EsArcoirisWeatherReport(projectile))
            {
                bool esBoss = npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type];
                float vidaPorcentaje = (float)npc.life / npc.lifeMax;

                if (esBoss)
                {
                    // Si es jefe y tiene 5% o menos, muere instantáneamente
                    if (vidaPorcentaje <= 0.05f)
                    {
                        modifiers.SetInstantKill();
                    }
                }
                else
                {
                    // Si es enemigo normal y tiene 40% o menos, muere instantáneamente
                    if (vidaPorcentaje <= 0.40f)
                    {
                        modifiers.SetInstantKill();
                    }
                }
            }
        }

        // 3. TRANSFORMACIÓN EN CARACOL SI MUERE POR EL ARCOÍRIS
        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (EsArcoirisWeatherReport(projectile))
            {
                // Si la vida es <= 0 aquí, significa que el ataque lo acaba de matar 
                // (ya sea por el Insta-Kill del 40%/5% o porque el arcoíris lo mató de un golpe limpio).
                if (npc.life <= 0)
                {
                    TransformarEnCaracol(npc);
                }
            }
        }

        // --- MÉTODOS DE UTILIDAD --- //

        private void TransformarEnCaracol(NPC npc)
        {
            // Validamos que solo el Servidor/Host sea el que cree el NPC para evitar duplicados en Multijugador
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Spawneamos un caracol vainilla de Terraria justo en la posición en la que murió el enemigo
                int caracolIndex = NPC.NewNPC(npc.GetSource_Death(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.Snail);

                // Sincronizamos en multijugador para que el resto de jugadores también vean al caracol
                if (caracolIndex >= 0 && caracolIndex < Main.maxNPCs && Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, caracolIndex);
                }
            }
        }

        private bool EsArcoirisWeatherReport(Projectile projectile)
        {
            // Detectamos si es el arcoíris Vanilla (que usa tu habilidad)
            if (projectile.type == ProjectileID.RainbowFront || projectile.type == ProjectileID.RainbowBack)
            {
                Player p = Main.player[projectile.owner];
                // Comprobamos que el dueño del proyectil está vivo y tiene tu buff activo
                if (p != null && p.active && p.HasBuff(ModContent.BuffType<DuracionArcoiris>()))
                {
                    return true;
                }
            }
            return false;
        }
    }
}