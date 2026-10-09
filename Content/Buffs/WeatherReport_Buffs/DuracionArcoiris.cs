// UBICACIÓN: Content/Buffs/WeatherReport_Buffs/DuracionArcoiris.cs
// ICONO NECESARIO: Content/Buffs/WeatherReport_Buffs/DuracionArcoiris.png
//   (mismo tamaño que uses para DuracionLluvia.png)

using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.WeatherReport_Buffs
{
    public class DuracionArcoiris : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Este buff SOLO actúa como temporizador para controlar cuánto dura
            // la habilidad de Arcoiris (WeatherReport Tier 4).
            // No aplica ningún efecto directo al jugador.
            //
            // Cuanto tiempo le des a este buff con p.AddBuff(buffType, X), eso es
            // lo que durará la habilidad. Si el buff se quita antes (con /clearbuffs,
            // otra fuente, etc.), la lluvia de arcoiris se detiene inmediatamente,
            // porque ArcoirisSkill_Tier4.PreUpdate() deja de spawnear en cuanto
            // deja de encontrar este buff activo.
        }
    }
}