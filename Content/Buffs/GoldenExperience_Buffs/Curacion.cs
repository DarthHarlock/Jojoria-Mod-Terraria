using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.GoldenExperience_Buffs
{
    // Buff GENÉRICO de cooldown para la habilidad de curación (Click derecho).
    // Se comparte entre TODOS los Stands que tengan esta misma habilidad (el actual
    // y los 4 futuros): evitamos crear un Curacion_Tier_1, Curacion_Tier_2... por cada uno.
    // Cada Stand decide su propia cantidad de vida y duración de cooldown al llamar a
    // CuracionManager.AplicarCuracion(), este buff solo actúa como temporizador y como
    // bandera visual (mientras está activo, el Stand muestra el sprite dorado).
    public class Curacion : ModBuff
    {
        // IMPORTANTE: necesitas un ícono de buff en esta ruta (32x32 aprox), igual que
        // tendrás para Cooldown1/Cooldown2/Cooldown3. Si no existe el archivo, el build
        // fallará o tModLoader usará una textura de error.
        public override string Texture => "Jojo/Content/Buffs/GoldenExperience_Buffs/Curacion";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;   // No es perjudicial, es solo un cooldown
            Main.buffNoSave[Type] = true; // No persiste al guardar/cargar el personaje
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // No hace nada por sí mismo: solo bloquea la reactivación de la habilidad
            // mientras esté presente.
        }

        // --- IMPIDE QUITARSE EL COOLDOWN A MANO ---
        // Por defecto, cualquier buff "bueno" (no debuff) se puede cancelar haciendo
        // click derecho en su icono de la barra de buffs. Como este buff es en
        // realidad el cooldown de la habilidad de curación, NO tiene sentido que el
        // jugador pueda quitárselo así sin más (se saltaría el cooldown entero).
        // Devolviendo false aquí, tModLoader ignora el click derecho sobre este buff.
        public override bool RightClick(int buffIndex)
        {
            return false;
        }
    }
}