using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.CMoon_Buffs
{
    public class GravedadDesactivada : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // Nombre y descripción del buff (se pueden sobreescribir en tus archivos .hjson)
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false; // Es un buff beneficioso (indicador) para el jugador
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Opcional: Podrías añadir efectos visuales extra al jugador aquí si quisieras
        }
    }
}