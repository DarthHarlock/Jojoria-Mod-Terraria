using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.SilverChariotRequiem_Buffs;

namespace Jojo.Content.Projectiles.SilverChariot.SilverChariot_Requiem
{
    public class SCR_Aura2 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        const float AuraRadius = 500f;

        public override void SetDefaults()
        {
            Projectile.width = (int)(AuraRadius * 2);
            Projectile.height = (int)(AuraRadius * 2);
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60; // ya da igual, lo controla el buff
            Projectile.hide = true;
        }

        // El aura no debe cortar césped, flores ni vides.
        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            // 1. REVISAR EL BUFF DEL JUGADOR (mismo patrón que SCR_Aura)
            Player dueño = Main.player[Projectile.owner];

            if (!dueño.HasBuff(ModContent.BuffType<AuraCaoticaBuff>()))
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;

            // 2. REVISAR EL STAND
            Projectile stand = Main.projectile[(int)Projectile.ai[0]];
            if (!stand.active || stand.type != ModContent.ProjectileType<SILVERCHARIOTSTAND_Requiem>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = stand.Center;

            // Iluminar el centro del aura (luz roja intensa)
            Lighting.AddLight(Projectile.Center, 1.5f, 0.1f, 0.1f);

            // Borde del aura en Carmesí (más brillante)
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(AuraRadius, AuraRadius);
                Dust ringDust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Crimson, Vector2.Zero, 0, Color.Red, 2f);
                ringDust.noGravity = true;

                Lighting.AddLight(ringDust.position, 0.6f, 0.05f, 0.05f);
            }

            // Interior del aura también en tonos sangre (brillantes)
            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(AuraRadius, AuraRadius);
                Dust innerDust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Blood, new Vector2(0f, -1.5f), 0, Color.Red, 1.5f);
                innerDust.noGravity = true;

                Lighting.AddLight(innerDust.position, 0.4f, 0.05f, 0.05f);
            }

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && !npc.dontTakeDamage)
                {
                    if (Vector2.Distance(npc.Center, Projectile.Center) <= AuraRadius)
                    {
                        npc.GetGlobalNPC<EfectoAleatorioGlobalNPC>().lastEfectoAuraHit = Main.GameUpdateCount;
                    }
                }
            }
        }
    }
}