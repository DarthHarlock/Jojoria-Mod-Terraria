using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Habilidades.Cinderella.Cinderella_Tier_4.HabilidadG
{
    // Buff "marcador": Terraria lo sincroniza a todos los jugadores automáticamente.
    // Cualquier cliente que vea este buff en un jugador le oculta la armadura.
    public class CinderellaArmaduraOcultaBuff : ModBuff
    {
        // Reutiliza el icono vanilla de Invisibilidad (no necesitas crear PNG)
        public override string Texture => "Terraria/Images/Buff_" + BuffID.Invisibility;

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
            Main.debuff[Type] = false;
        }
    }
}