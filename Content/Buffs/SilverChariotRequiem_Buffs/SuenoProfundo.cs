using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.SilverChariotRequiem_Buffs
{
    // Este buff sirve como temporizador para la habilidad F
    public class SuenoProfundo : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false; // Falso porque es un buff propio del jugador, no un debufo negativo
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = true; // No se guarda si sales del mundo

            // Nota: El nombre y la descripción real deberás ponerlos en tus archivos .hjson (Localization)
        }

        // No necesitamos método Update, el juego ya maneja el tiempo del buff automáticamente.
    }
}