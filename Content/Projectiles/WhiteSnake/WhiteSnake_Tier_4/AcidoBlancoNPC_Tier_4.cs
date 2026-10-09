using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Buffs.WhiteSnake_Buffs;

namespace Jojo.Content.Buffs
{
    /// <summary>Stats del ácido blanco. Las define cada stand.</summary>
    public struct AcidoBlancoStats
    {
        /// <summary>Duración en ticks (60 = 1s).</summary>
        public int Duracion;
        /// <summary>Frecuencia: golpes por segundo = LifeRegen / 2 (16 = 8 golpes/s).</summary>
        public int LifeRegen;
        /// <summary>Daño REAL de CADA golpe (ignora defensa).</summary>
        public int Damage;
        /// <summary>Fracción de velocidad que se quita (1 = parado del todo).</summary>
        public float Slow;
    }

    /// <summary>Lo implementa cada stand que lance ácido (propiedad Acido con SUS stats).</summary>
    public interface IAcidoStand
    {
        AcidoBlancoStats Acido { get; }
    }

    public class AcidoBlancoNPC_Tier_4 : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool tieneAcidoBlanco;

        // Stats GRABADAS en este NPC por quien aplicó el ácido
        public int damageAcido;
        public int lifeRegenAcido;
        public float slowAcido;
        int timerGolpe;

        /// <summary>Respaldo: stats del stand con ácido más fuerte activo (solo si el NPC no tiene stats grabadas).</summary>
        public static bool ObtenerStats(out AcidoBlancoStats stats)
        {
            stats = default;
            bool encontrado = false;

            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.ModProjectile is IAcidoStand stand)
                {
                    AcidoBlancoStats s = stand.Acido;
                    if (!encontrado || s.Damage * s.LifeRegen > stats.Damage * stats.LifeRegen)
                    {
                        stats = s;
                        encontrado = true;
                    }
                }
            }
            return encontrado;
        }

        /// <summary>Lo llaman rastros y otras cosas: busca el stand del dueño y aplica SUS stats.</summary>
        public static bool AplicarDesdeProyectil(NPC npc, Projectile proyectil)
        {
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p.owner == proyectil.owner && p.ModProjectile is IAcidoStand stand)
                {
                    Aplicar(npc, stand.Acido);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Graba las stats en el NPC y le pone el debuff.</summary>
        public static void Aplicar(NPC npc, AcidoBlancoStats stats)
        {
            int buffType = ModContent.BuffType<AcidoBlancoDebuff_Tier_4>();
            var g = npc.GetGlobalNPC<AcidoBlancoNPC_Tier_4>();

            // Si ya tenía ácido, solo se sobreescribe si el nuevo es igual o más fuerte
            bool yaTenia = npc.HasBuff(buffType) && g.damageAcido > 0;
            if (!yaTenia || stats.Damage * stats.LifeRegen >= g.damageAcido * g.lifeRegenAcido)
            {
                g.damageAcido = stats.Damage;
                g.lifeRegenAcido = stats.LifeRegen;
                g.slowAcido = stats.Slow;
            }

            npc.AddBuff(buffType, stats.Duracion);
            npc.netUpdate = true;
        }

        public override void ResetEffects(NPC npc)
        {
            tieneAcidoBlanco = false;
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (!tieneAcidoBlanco) return;

            // Solo impide la regeneración. El daño real lo hace PostAI.
            if (npc.lifeRegen > 0) npc.lifeRegen = 0;

            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.Cloud, 0f, -1f, 100, Color.White, 1.2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.5f;
            }
        }

        public override void PostAI(NPC npc)
        {
            if (!tieneAcidoBlanco)
            {
                timerGolpe = 0;
                damageAcido = 0; // sin debuff = sin stats grabadas
                lifeRegenAcido = 0;
                slowAcido = 0f;
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (damageAcido <= 0 || lifeRegenAcido <= 0) return;

            // golpes/s = LifeRegen/2  ->  un golpe cada 120/LifeRegen ticks
            int intervalo = Math.Max(1, 120 / lifeRegenAcido);

            if (++timerGolpe < intervalo) return;
            timerGolpe = 0;

            NPC.HitInfo hit = new NPC.HitInfo
            {
                Damage = damageAcido,
                SourceDamage = damageAcido,
                Knockback = 0f,
                HitDirection = 0,
                Crit = false
            };

            npc.StrikeNPC(hit, false, true);

            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendStrikeNPC(npc, hit);
        }
    }
}