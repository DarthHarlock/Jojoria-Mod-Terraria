using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.SilverChariotRequiem_Buffs
{
    // Buff marcador que controla la duración real de la habilidad G (SCR_Aura2).
    // Funciona exactamente igual que SuenoProfundo para la habilidad F:
    // mientras el jugador tenga este buff, el aura vive; cuando se acaba,
    // el aura muere con él.
    public class AuraCaoticaBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false; // buff propio del jugador, no un debufo negativo
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = true; // no se guarda si sales del mundo
        }

        // No necesitamos Update, el juego ya maneja el tiempo del buff automáticamente.
    }
}