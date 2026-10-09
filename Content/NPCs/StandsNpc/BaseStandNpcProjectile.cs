using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.StandsNpc
{
    public abstract class BaseStandNpcProjectile : ModProjectile
    {
        // Tabla de Cinderella. Interruptores independientes: gana el más alto activo.
        public static int GetProgressionDamage()
        {
            int damage = 5;

            bool muroDeCarne = Main.hardMode;
            bool mecanicos = NPC.downedMechBossAny;
            bool golem = NPC.downedGolemBoss;

            if (muroDeCarne) damage = Math.Max(damage, 15);
            if (mecanicos) damage = Math.Max(damage, 20);
            if (golem) damage = Math.Max(damage, 30);

            return damage;
        }

        public static int GetAdjustedDamage()
        {
            return GetProgressionDamage();
        }

        public static bool HayBossVivo()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n != null && n.active && (n.boss || NPCID.Sets.ShouldBeCountedAsBoss[n.type]))
                    return true;
            }
            return false;
        }

        // DAÑO CONTRA JUGADORES: base = Projectile.damage propio de cada proyectil, +-10% natural.
        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            int baseDmg = Projectile.damage;
            if (baseDmg <= 0) return;

            float variacion = Main.rand.NextFloat(0.90f, 1.10f);
            int dañoFinal = Math.Max(1, (int)Math.Round(baseDmg * variacion));

            modifiers.SourceDamage = new StatModifier(1f, 0f, dañoFinal, 0f);
            modifiers.FinalDamage = StatModifier.Default;

            modifiers.ArmorPenetration += Projectile.ArmorPenetration;
        }

        // NERF POR BOSS contra NPCs: con un boss vivo, pegan al 10%.
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (HayBossVivo())
                modifiers.FinalDamage *= 0.10f;
        }
    }
}