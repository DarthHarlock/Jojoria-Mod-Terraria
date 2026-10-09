using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.KingCrimson_Buffs
{
    public class PrediccionKingCrimson : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Toda la lógica de detección de daño y esquive 
            // se maneja de forma segura en EpitaphPlayer.cs
        }
    }
}