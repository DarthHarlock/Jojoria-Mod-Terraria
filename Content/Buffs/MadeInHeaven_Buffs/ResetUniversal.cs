using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace Jojo.Content.Buffs.MadeInHeaven_Buffs
{
    public class ResetUniversal : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Lógica manejada externamente
        }
    }
}