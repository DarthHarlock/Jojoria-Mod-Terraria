using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs
{
    public class CD : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // ✔ mostrar tiempo
            Main.buffNoTimeDisplay[Type] = false;

            // 🔥 CLAVE: NO se puede quitar manualmente
            Main.debuff[Type] = true;

            // ✔ no se guarda al salir (evita exploits)
            Main.buffNoSave[Type] = true;

            // ✔ pvp ok
            Main.pvpBuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Buff vacío, solo controla cooldown global
        }
    }
}