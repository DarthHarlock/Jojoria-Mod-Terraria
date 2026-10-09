using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.KillerQueen_Buffs
{
    // Solo marca visualmente el tiempo restante para poder detonar.
    // Toda la logica real vive en KQ_BitesTheDustPlayer.
    public class KQ_BitesTheDust_Buff_Tier4 : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Sin logica aqui a proposito, el ModPlayer se encarga de todo.
        }
    }
}