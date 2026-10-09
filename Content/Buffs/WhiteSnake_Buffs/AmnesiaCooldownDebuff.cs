using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.WhiteSnake_Buffs
{
    public class AmnesiaCooldownDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // No es visible en la interfaz del jugador porque se aplica a NPCs,
            // pero si usas mods como "Boss Checklist" o mirillas de enemigos, verán que es inmune.
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }
    }
}