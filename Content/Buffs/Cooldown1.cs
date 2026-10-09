using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs
{
    public class Cooldown1 : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // ✔ se muestra el tiempo
            Main.buffNoTimeDisplay[Type] = false;

            // 🔥 CLAVE: no se puede quitar con click derecho
            Main.debuff[Type] = true;

            // ✔ opcional (no se guarda al salir)
            Main.buffNoSave[Type] = true;

            // ✔ pvp compatible
            Main.pvpBuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Sin lógica necesaria aquí
        }
    }
}