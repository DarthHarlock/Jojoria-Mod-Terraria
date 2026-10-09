using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Clases
{
    public class ClaseStand : DamageClass
    {
        public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
        {
            // ✔ hereda TODO el daño global tipo vanilla
            if (damageClass == DamageClass.Generic)
                return StatInheritanceData.Full;

            return StatInheritanceData.None;
        }

        public override bool GetEffectInheritance(DamageClass damageClass)
        {
            // ✔ permite efectos globales (buffs, pociones)
            return damageClass == DamageClass.Generic;
        }

        public override bool UseStandardCritCalcs => true;
    }
}