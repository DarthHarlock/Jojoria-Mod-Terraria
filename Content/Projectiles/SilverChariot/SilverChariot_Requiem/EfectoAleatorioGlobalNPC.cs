using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.SilverChariotRequiem_Buffs;


namespace Jojo.Content.Projectiles.SilverChariot.SilverChariot_Requiem
{
    public class EfectoAleatorioGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public uint lastEfectoAuraHit = 0;

        // REDUCIDO: 60 ticks = 1 segundo. Se aplica rapidísimo.
        private const int TicksParaEfecto = 60;
        private const int DuracionDebuff = 1200;

        private int efectoTimer = 0;

        private static readonly int[] BuffsPosibles = new int[3];
        private static bool inicializado = false;

        private static void InicializarBuffs()
        {
            if (inicializado) return;
            BuffsPosibles[0] = ModContent.BuffType<RequiemDebilidadDebuff>();
            BuffsPosibles[1] = ModContent.BuffType<RequiemLentitudDebuff>();
            BuffsPosibles[2] = ModContent.BuffType<RequiemSangradoDebuff>();
            inicializado = true;
        }

        public override bool PreAI(NPC npc)
        {
            InicializarBuffs();

            if (Main.GameUpdateCount - lastEfectoAuraHit <= 2)
            {
                efectoTimer++;

                if (efectoTimer >= TicksParaEfecto)
                {
                    // Crear una lista solo con los debufos que el NPC NO tiene
                    List<int> buffsDisponibles = new List<int>();
                    for (int i = 0; i < BuffsPosibles.Length; i++)
                    {
                        if (!npc.HasBuff(BuffsPosibles[i]))
                        {
                            buffsDisponibles.Add(BuffsPosibles[i]);
                        }
                    }

                    // Si quedan debufos por aplicar, le metemos uno aleatorio de los que faltan
                    if (buffsDisponibles.Count > 0)
                    {
                        int buffElegido = buffsDisponibles[Main.rand.Next(buffsDisponibles.Count)];
                        npc.AddBuff(buffElegido, DuracionDebuff);
                    }

                    efectoTimer = 0; // Reseteamos el timer para el próximo debufo
                }
            }
            else
            {
                efectoTimer = 0;
            }

            return true;
        }
    }
}