using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.GoldenExperienceRequiem_Buffs
{
    // Buff puramente "marcador de tiempo": mientras dure, la habilidad
    // "Vuelta a Zero" está activa. Toda la lógica real (shader, detección
    // de golpes, rebobinado de NPCs) vive en la carpeta de la habilidad
    // (Content/Habilidades/GoldenExperience_Requiem/ReturnZero), este buff
    // solo existe para que el juego sepa "cuánto dura la habilidad".
    public class ReturnToZero : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = false;
            Main.pvpBuff[Type] = true;
        }

        // No hace falta Update: ReturnZeroPlayer.PostUpdateBuffs ya chequea
        // HasBuff cada tick para prender/apagar el shader.
    }
}