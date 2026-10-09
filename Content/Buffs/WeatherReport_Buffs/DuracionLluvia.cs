using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.WeatherReport_Buffs
{
    /// <summary>
    /// Buff genérico de temporizador para cualquier habilidad tipo "lluvia" (ranas, fuego, lo que sea a futuro).
    /// No hace nada por sí solo: cada habilidad revisa cuánto tiempo le queda a este buff
    /// (Player.buffTime[buffIndex]) para saber cuánto debe durar su propio efecto.
    /// Cuando el buff se acaba, Terraria lo quita solo y el efecto se detiene.
    /// </summary>
    public class DuracionLluvia : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true; // Es un efecto de habilidad, no debe persistir al guardar/salir
            Main.debuff[Type] = false;
        }
    }
}