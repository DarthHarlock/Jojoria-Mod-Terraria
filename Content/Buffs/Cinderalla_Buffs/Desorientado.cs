using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.Cinderalla_Buffs
{
    // Debuff de Cinderella para bosses (los bosses son inmunes a Confused).
    // El movimiento errático y los "?" se manejan en DesorientadoGlobalNPC.
    public class Desorientado : ModBuff
    {
        // Reutiliza el icono de "Confused" de Terraria.
        // Si más adelante quieres un icono propio: pon Desorientado.png en esta carpeta y borra esta línea.
        public override string Texture => "Terraria/Images/Buff_" + BuffID.Confused;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
        }
    }
}