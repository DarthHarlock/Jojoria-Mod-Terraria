using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Requiem;

namespace Jojo.Content.Buffs.SilverChariotRequiem_Buffs
{
    public class SuenoProfundoDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        // Esta función se ejecuta CADA TICK para cualquier NPC que tenga el buff
        public override void Update(NPC npc, ref int buffIndex)
        {
            // Cada 35 ticks (poco más de medio segundo), spawneamos una Z en su cabeza
            if (Main.GameUpdateCount % 35 == 0)
            {
                Projectile.NewProjectile(
                    npc.GetSource_Buff(buffIndex),
                    npc.Top + new Vector2(0, -10), // Un poco por encima del NPC
                    Vector2.Zero,
                    ModContent.ProjectileType<ZZZ_Visual>(),
                    0,
                    0,
                    Main.myPlayer
                );
            }
        }
    }
}