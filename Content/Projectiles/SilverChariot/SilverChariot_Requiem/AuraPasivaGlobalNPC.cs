using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.SilverChariotRequiem_Buffs;


namespace Jojo.Content.Projectiles.SilverChariot.SilverChariot_Requiem
{
    // Lógica exclusiva del aura PASIVA. Totalmente independiente de
    // SuenoProfundoGlobalNPC y EfectoAleatorioGlobalNPC: no comparte
    // timers ni contadores con las habilidades activas, así que nunca
    // puede "robarle" velocidad de aplicación a F ni a G.
    public class AuraPasivaGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public uint lastPassiveAuraHit = 0;

        private int passiveTimer = 0;

        // Cada cuánto se intenta aplicar un debuff pasivo (ticks)
        private const int TicksParaEfectoPasivo = 1200; // 1.5 segundos

        // Duraciones reducidas frente a las versiones que dan las habilidades activas
        private const int DuracionSuenoPasivo = 180;      // 3s   (activo: 1200)
        private const int DuracionDebilidadPasivo = 240;  // 4s
        private const int DuracionLentitudPasivo = 240;   // 4s
        private const int DuracionSangradoPasivo = 180;   // 3s

        private static readonly int[] BuffsPasivosPosibles = new int[4];
        private static readonly int[] DuracionesPasivas = new int[4];
        private static bool inicializado = false;

        private static void Inicializar()
        {
            if (inicializado) return;

            BuffsPasivosPosibles[0] = ModContent.BuffType<SuenoProfundoDebuff>();
            DuracionesPasivas[0] = DuracionSuenoPasivo;

            BuffsPasivosPosibles[1] = ModContent.BuffType<RequiemDebilidadDebuff>();
            DuracionesPasivas[1] = DuracionDebilidadPasivo;

            BuffsPasivosPosibles[2] = ModContent.BuffType<RequiemLentitudDebuff>();
            DuracionesPasivas[2] = DuracionLentitudPasivo;

            BuffsPasivosPosibles[3] = ModContent.BuffType<RequiemSangradoDebuff>();
            DuracionesPasivas[3] = DuracionSangradoPasivo;

            inicializado = true;
        }

        public override bool PreAI(NPC npc)
        {
            Inicializar();

            if (Main.GameUpdateCount - lastPassiveAuraHit <= 2)
            {
                passiveTimer++;

                if (passiveTimer >= TicksParaEfectoPasivo)
                {
                    // Solo consideramos los debuffs que el NPC todavía no tiene,
                    // para no perder el tiro aleatorio en un buff ya activo.
                    List<int> indicesDisponibles = new List<int>();
                    for (int i = 0; i < BuffsPasivosPosibles.Length; i++)
                    {
                        if (!npc.HasBuff(BuffsPasivosPosibles[i]))
                            indicesDisponibles.Add(i);
                    }

                    if (indicesDisponibles.Count > 0)
                    {
                        int elegido = indicesDisponibles[Main.rand.Next(indicesDisponibles.Count)];
                        npc.AddBuff(BuffsPasivosPosibles[elegido], DuracionesPasivas[elegido]);
                    }

                    passiveTimer = 0;
                }
            }
            else
            {
                passiveTimer = 0;
            }

            // IMPORTANTE: devolvemos true (no bloqueamos la IA). El "congelamiento"
            // por sueño ya lo gestiona SuenoProfundoGlobalNPC.PreAI leyendo el buff,
            // sea quien sea quien lo haya puesto (activa o pasiva).
            return true;
        }
    }
}