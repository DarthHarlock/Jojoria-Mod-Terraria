using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Jojo.Content.Clases;
using Jojo.Content.Systems; // Asegúrate de incluir el namespace donde esté UI.StandSlotSystem
using Jojo.Content.Buffs.MagiciansRed_Buffs;

namespace Jojo.Content.Buffs.MagiciansRed_Buffs
{
    public class MagiciansFire_Tier_2 : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // AQUÍ: Actualizado al nuevo nombre de la clase
            npc.GetGlobalNPC<MagiciansFireNPC_Tier_2>().magiciansFire = true;
        }
    }

    // AQUÍ: Cambiamos el nombre para que no choque con el MagiciansFire original
    public class MagiciansFireNPC_Tier_2 : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool magiciansFire;
        public int damageTimer;

        // --- VARIABLE DE DAÑO BASE ---
        public int dañoBaseQuemadura = 20;

        public override void ResetEffects(NPC npc)
        {
            magiciansFire = false;
        }

        public override void PostAI(NPC npc)
        {
            if (magiciansFire && !npc.friendly && npc.life > 0)
            {
                damageTimer++;

                if (damageTimer >= 60)
                {
                    damageTimer = 0;

                    // Encontramos al jugador más cercano para leer sus estadísticas
                    Player player = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)];

                    // Escalamos el daño base (50) con el daño de la ClaseStand del jugador
                    int dañoEscalado = (int)player.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(dañoBaseQuemadura);

                    // Le aplicamos la variación orgánica de Terraria (+/- 15%)
                    int dañoFinal = Main.DamageVar(dañoEscalado);

                    NPC.HitInfo hit = new NPC.HitInfo
                    {
                        Damage = dañoFinal,
                        DamageType = ModContent.GetInstance<ClaseStand>(),
                        Knockback = 0,
                        HitDirection = 0
                    };

                    npc.StrikeNPC(hit);
                }
            }
            else
            {
                damageTimer = 0;
            }
        }

        // --- Helper para definir partículas y el tinte del NPC según la skin ---
        private void GetSkinVisuals(Player p, out int mainDust, out int sparkDust, out Color tintColor)
        {
            if (UI.StandSlotSystem.HasMagiciansRedPinkSkinFor(p))
            {
                mainDust = DustID.PinkTorch;
                sparkDust = DustID.PinkTorch; // Las chispas también serán rosas
                tintColor = new Color(255, 100, 200); // Tinte rosado
            }
            else if (UI.StandSlotSystem.HasMagiciansRedGreenSkinFor(p))
            {
                mainDust = DustID.CursedTorch;
                sparkDust = DustID.CursedTorch;
                tintColor = new Color(100, 255, 100); // Tinte verde
            }
            else if (UI.StandSlotSystem.HasMagiciansRedBlueSkinFor(p))
            {
                mainDust = DustID.IceTorch;
                sparkDust = DustID.IceTorch;
                tintColor = new Color(100, 150, 255); // Tinte azul
            }
            else
            {
                mainDust = DustID.Torch;
                sparkDust = DustID.SolarFlare;
                tintColor = new Color(255, 100, 50); // Tinte naranja original
            }
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (magiciansFire)
            {
                // Obtenemos al jugador local para ver qué skin tiene equipada visualmente
                Player localPlayer = Main.LocalPlayer;
                GetSkinVisuals(localPlayer, out int mainDust, out int sparkDust, out Color tintColor);

                // Aplicamos el tinte al NPC
                drawColor = tintColor;

                // MÁS cantidad de partículas, pero TAMAÑO estándar (1.4f) adaptado a la skin
                for (int i = 0; i < 2; i++)
                {
                    Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, mainDust, 0f, -2f, 100, default, 1.4f);
                    dust.noGravity = true;
                    dust.velocity.Y -= 1.5f;
                }

                // Chispas de fuego extra adaptadas a la skin
                if (Main.rand.NextBool(2))
                {
                    Dust spark = Dust.NewDustDirect(npc.position, npc.width, npc.height, sparkDust, 0f, 0f, 100, default, 1.5f);
                    spark.noGravity = true;
                    spark.velocity *= 2f;
                }
            }
        }
    }
}