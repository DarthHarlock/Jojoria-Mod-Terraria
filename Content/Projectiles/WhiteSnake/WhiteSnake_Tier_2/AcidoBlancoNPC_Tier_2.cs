using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_2;

namespace Jojo.Content.Buffs
{
    /// <summary>
    /// Ácido blanco de TIER 2. Muchos golpes pequeños mientras el enemigo tiene el debuff.
    /// Los números salen de las constantes ACIDO_* de WHITESNAKESTAND_Tier_2.
    /// </summary>
    public class AcidoBlancoNPC_Tier_2 : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool tieneAcidoBlanco;
        int timerGolpe;

        public override void ResetEffects(NPC npc)
        {
            tieneAcidoBlanco = false;
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (!tieneAcidoBlanco) return;

            // Solo impide la regeneración. El daño real lo hace PostAI.
            if (npc.lifeRegen > 0) npc.lifeRegen = 0;

            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.Cloud, 0f, -1f, 100, Color.White, 1.2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.5f;
            }
        }

        public override void PostAI(NPC npc)
        {
            if (!tieneAcidoBlanco)
            {
                timerGolpe = 0;
                return;
            }

            // Los golpes los calcula el servidor (o el modo un jugador)
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            int dano = WHITESNAKESTAND_Tier_2.ACIDO_DANO_POR_GOLPE;
            int golpesPorSegundo = WHITESNAKESTAND_Tier_2.ACIDO_GOLPES_POR_SEGUNDO;
            if (dano <= 0 || golpesPorSegundo <= 0) return;

            int intervalo = Math.Max(1, 60 / golpesPorSegundo);

            if (++timerGolpe < intervalo) return;
            timerGolpe = 0;

            NPC.HitInfo hit = new NPC.HitInfo
            {
                Damage = dano,
                SourceDamage = dano,
                Knockback = 0f,
                HitDirection = 0,
                Crit = false
            };

            npc.StrikeNPC(hit, false, true);

            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendStrikeNPC(npc, hit);
        }
    }

    /// <summary>Debuff del ácido de Tier 2 (en este mismo archivo, no hace falta uno nuevo).</summary>
    public class AcidoBlancoDebuff_Tier_2 : ModBuff
    {
        // Reutiliza el icono del ácido de Tier 4. Si tu icono está en otra carpeta, cambia esta ruta.
        public override string Texture => "Jojo/Content/Buffs/WhiteSnake_Buffs/AcidoBlancoDebuff_Tier_4";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<AcidoBlancoNPC_Tier_2>().tieneAcidoBlanco = true;

            // 1 = parado del todo. Más de 1 haría retroceder al enemigo, por eso se limita.
            float slow = Math.Clamp(WHITESNAKESTAND_Tier_2.ACIDO_SLOW, 0f, 1f);
            npc.position -= npc.velocity * slow;
        }
    }
}