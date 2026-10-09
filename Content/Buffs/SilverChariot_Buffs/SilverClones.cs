using Terraria;
using Terraria.ModLoader;
using Jojo.Content.UI;

namespace Jojo.Content.Buffs.SilverChariot_Buffs
{
    public class SilverClones : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false; // Queremos ver el contador
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Buscamos qué Stand debería tener el jugador activo
            int expectedStandType = StandSlotSystem.GetStandProjectileTypeFor(player);
            bool standActive = false;

            // Si tiene un Stand equipado, comprobamos si el proyectil está vivo en el mundo
            if (expectedStandType != 0)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active && proj.owner == player.whoAmI && proj.type == expectedStandType)
                    {
                        standActive = true;
                        break;
                    }
                }
            }

            // Si el jugador despawneó al Stand (el proyectil ya no existe), 
            // nos deshacemos de este buff inmediatamente. Al eliminarse el buff,
            // los clones vinculados a él también morirán o dejarán de spawnear.
            if (!standActive)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}