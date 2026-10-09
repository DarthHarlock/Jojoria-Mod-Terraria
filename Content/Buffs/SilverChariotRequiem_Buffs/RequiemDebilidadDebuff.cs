using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.SilverChariotRequiem_Buffs
{
    public class RequiemDebilidadDebuff : ModBuff
    {
        public const float MultiplicadorDefensa = 0.5f;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // Reduce la defensa
            npc.defense = (int)(npc.defense * MultiplicadorDefensa);

            // Genera partículas verdes brillantes (Cursed Flame/Verde) alrededor del enemigo
            if (Main.rand.NextBool(3)) // 1 de cada 3 ticks
            {
                int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.CursedTorch, 0f, 0f, 100, default, 1.2f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.5f; // Movimiento suave
            }
        }
    }
}