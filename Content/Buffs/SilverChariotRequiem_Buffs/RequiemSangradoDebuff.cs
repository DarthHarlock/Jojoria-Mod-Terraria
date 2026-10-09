using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace Jojo.Content.Buffs.SilverChariotRequiem_Buffs
{
    public class RequiemSangradoDebuff : ModBuff
    {
        public const int DanoPorTick = 80;
        // Frecuencia bajada a 10 para que expulse sangre muy seguido
        public const int FrecuenciaParticulas = 20;
        public const int CantidadParticulasPorTick = 4;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            if (npc.lifeRegen > 0)
                npc.lifeRegen = 0;

            npc.lifeRegen -= DanoPorTick;

            if (Main.GameUpdateCount % FrecuenciaParticulas == 0)
            {
                for (int i = 0; i < CantidadParticulasPorTick; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, 0f, 0f, 0, default, 1.5f);
                    // Ahora la sangre sale DISPARADA en direcciones aleatorias
                    Main.dust[d].velocity.X = Main.rand.NextFloat(-4f, 4f);
                    Main.dust[d].velocity.Y = Main.rand.NextFloat(-4f, 4f);
                    Main.dust[d].noGravity = false;
                }
            }
        }
    }
}