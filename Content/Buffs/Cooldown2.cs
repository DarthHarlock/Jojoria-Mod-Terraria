using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs
{
    public class Cooldown2 : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // ✔ Mostrar tiempo restante en pantalla
            Main.buffNoTimeDisplay[Type] = false;

            // 🔥 CLAVE: Evita que el jugador se lo pueda quitar manualmente con click derecho
            Main.debuff[Type] = true;

            // ✔ Evita guardar el buff al salir del mundo (anti-exploit)
            Main.buffNoSave[Type] = true;

            // ✔ Compatible con PvP
            Main.pvpBuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Buff vacío: solo controla el tiempo de espera de la habilidad G
        }
    }
}