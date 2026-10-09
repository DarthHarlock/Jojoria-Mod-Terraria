using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Requiem; // <-- Aquí está la ruta corregida

namespace Jojo.Content.Buffs.SilverChariotRequiem_Buffs
{
    public class ReflejoOscuroBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<ReflejoOscuroPlayer>().reflejoActivo = true;
        }
    }
}