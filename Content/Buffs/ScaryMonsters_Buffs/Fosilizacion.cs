using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4.Transformacion.ModoDinoFull;

namespace Jojo.Content.Buffs.ScaryMonsters_Buffs
{
    public class Fosilizacion : ModBuff
    {
        // Textura: Fosilizacion.png en esta misma carpeta (32x32)

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.mount.SetMount(ModContent.MountType<FromaDino>(), player);
            player.buffTime[buffIndex] = 10;
        }
    }
}