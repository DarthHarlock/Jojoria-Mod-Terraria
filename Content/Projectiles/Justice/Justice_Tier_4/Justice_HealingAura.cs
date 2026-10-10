using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio; // Necesario para reproducir sonidos
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.Justice.Justice_Tier_4
{
    public class Justice_HealingAura : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        // Radio máximo que alcanza el anillo.
        const float MaxRadius = 340f;

        // Ticks que tarda en llegar al radio máximo.
        public const int ExpandTicks = 14;

        // Ticks extra que se queda desvaneciéndose.
        public const int LingerTicks = 6;

        public const int TotalLifeTicks = ExpandTicks + LingerTicks;

        int elapsed = 0;
        float currentRadius = 0f;

        private List<int> npcsCurados = new List<int>();

        // Declaración del sonido custom (Asegúrate de que no tenga la extensión .wav al final del string)
        public static readonly SoundStyle HealSound = new SoundStyle("Jojo/Content/Sonidos/JusticeHealth");

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalLifeTicks;
            Projectile.hide = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            if (!p.active || p.dead) { Projectile.Kill(); return; }

            Projectile.Center = p.Center;

            // Reproducir el custom sound en el primer frame de vida del aura
            if (elapsed == 0)
            {
                SoundEngine.PlaySound(HealSound, Projectile.Center);
            }

            elapsed++;

            float t = MathHelper.Clamp(elapsed / (float)ExpandTicks, 0f, 1f);
            float eased = 1f - (1f - t) * (1f - t);
            currentRadius = eased * MaxRadius;

            // --- EFECTO VISUAL DE PARTÍCULAS ---
            for (int i = 0; i < 10; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(currentRadius, currentRadius);
                Dust ringDust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.ChlorophyteWeapon, Vector2.Zero, 100, Color.LimeGreen, 1.5f);
                ringDust.noGravity = true;
            }

            for (int i = 0; i < 4; i++)
            {
                float innerR = MathHelper.Max(0f, currentRadius - 25f);
                Vector2 offset = Main.rand.NextVector2CircularEdge(innerR, innerR);
                Dust innerDust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.GreenFairy, Vector2.Zero, 100, Color.LimeGreen, 1.3f);
                innerDust.noGravity = true;
            }

            // --- LÓGICA DE CURACIÓN ÚNICA A INFECTADOS ---
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (npc.active && !npcsCurados.Contains(npc.whoAmI))
                {
                    JusticeGlobalNPC globalNPC = npc.GetGlobalNPC<JusticeGlobalNPC>();

                    if (globalNPC.bajoControlMental)
                    {
                        if (Vector2.Distance(Projectile.Center, npc.Center) <= currentRadius)
                        {
                            int curacion = (int)(npc.lifeMax * 0.30f);
                            npc.life += curacion;

                            if (npc.life > npc.lifeMax) npc.life = npc.lifeMax;

                            npc.HealEffect(curacion, true);

                            for (int d = 0; d < 6; d++)
                            {
                                Dust.NewDust(npc.position, npc.width, npc.height, DustID.ChlorophyteWeapon, 0f, -2f, 0, Color.LimeGreen, 1.2f);
                            }

                            npcsCurados.Add(npc.whoAmI);

                            if (Main.netMode != NetmodeID.SinglePlayer)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                            }
                        }
                    }
                }
            }
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 18; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(currentRadius, currentRadius);
                Dust d = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Smoke, Vector2.Zero, 100, default, 1.1f);
                d.noGravity = true;
            }
        }
    }
}