using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.ScaryMonsters_Buffs
{
    public class DinoVirus : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // Efecto visual sutil de partículas mientras dura el debuff.
            // Al estar en la clase de un Buff, todos los jugadores en multijugador lo verán automáticamente.
            if (Main.rand.NextBool(12))
            {
                Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.GreenTorch, 0f, -1f, 120, default, 1f);
                d.noGravity = true;
                d.velocity *= 0.4f;
            }
        }
    }
}