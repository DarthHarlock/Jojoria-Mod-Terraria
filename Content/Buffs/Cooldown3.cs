using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs
{
    public class Cooldown3 : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // ✔ mostrar tiempo
            Main.buffNoTimeDisplay[Type] = false;

            // 🔥 CLAVE: no se puede quitar manualmente
            Main.debuff[Type] = true;

            // ✔ evita guardar el buff (anti exploit)
            Main.buffNoSave[Type] = true;

            // ✔ pvp compatible
            Main.pvpBuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Buff vacío, solo cooldown
        }
    }
}