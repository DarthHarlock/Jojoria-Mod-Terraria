using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Clases
{
    public static class StandCritSystem
    {
        // =========================================
        // CALCULA CRIT FINAL (BASE + MODIFICADORES)
        // =========================================
        public static int GetFinalCritChance(Player player, int baseCrit)
        {
            int crit = baseCrit;

            // suma buffs, pociones y accesorios globales
            crit += (int)player.GetCritChance(DamageClass.Generic);

            // clamp
            if (crit > 100)
                crit = 100;

            if (crit < 0)
                crit = 0;

            return crit;
        }

        // =========================================
        // APLICAR CRIT REAL AL GOLPE
        // =========================================
        public static void ApplyCrit(Projectile proj, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[proj.owner];

            int critChance = GetFinalCritChance(player, proj.CritChance);

            if (Main.rand.Next(100) < critChance)
            {
                modifiers.SetCrit();
            }
        }
    }
}