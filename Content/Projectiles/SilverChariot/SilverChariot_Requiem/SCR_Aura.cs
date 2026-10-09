using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.SilverChariotRequiem_Buffs;


namespace Jojo.Content.Projectiles.SilverChariot.SilverChariot_Requiem
{
    public class SCR_Aura : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        const float AuraRadius = 350f;

        public override void SetDefaults()
        {
            Projectile.width = (int)(AuraRadius * 2);
            Projectile.height = (int)(AuraRadius * 2);
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.hide = true;
        }

        // >>> FIX: el aura no debe cortar césped, flores ni vides.
        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            Player dueño = Main.player[Projectile.owner];

            if (!dueño.HasBuff(ModContent.BuffType<SuenoProfundo>()))
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;

            Projectile stand = Main.projectile[(int)Projectile.ai[0]];
            if (!stand.active || stand.type != ModContent.ProjectileType<SILVERCHARIOTSTAND_Requiem>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = stand.Center;

            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(AuraRadius, AuraRadius);
                Dust ringDust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.PurpleCrystalShard, Vector2.Zero, 150, Color.Black, 1.5f);
                ringDust.noGravity = true;
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(AuraRadius, AuraRadius);
                Dust innerDust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Granite, new Vector2(0f, -1.5f), 100, Color.DarkOrchid, 1.2f);
                innerDust.noGravity = true;
            }

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && !npc.dontTakeDamage)
                {
                    if (Vector2.Distance(npc.Center, Projectile.Center) <= AuraRadius)
                    {
                        List<NPC> todosLosSegmentos = SuenoProfundoGlobalNPC.ObtenerSegmentosGusano(npc);
                        foreach (NPC segmento in todosLosSegmentos)
                        {
                            segmento.GetGlobalNPC<SuenoProfundoGlobalNPC>().lastAuraHit = Main.GameUpdateCount;
                        }
                    }
                }
            }
        }
    }
}