using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Clases;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_3.LluviaDeRanas_Tier_3;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_4.LluviaDeRanas_Tier_4;

namespace Jojo.Content.Buffs.WeatherReport_Buffs
{
    public class Veneno_Tier_4 : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // Buscamos al jugador más cercano para usar sus stats de daño
            Player p = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)];

            // Calculamos el LifeRegen a restar (base 35) escalado con la ClaseStand
            int regenReduccion = (int)p.GetDamage<ClaseStand>().ApplyTo(35f);

            // 1. DAÑO
            if (npc.lifeRegen > 0)
            {
                npc.lifeRegen = 0;
            }
            npc.lifeRegen -= regenReduccion;

            // 2. EFECTO VISUAL
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.PurpleTorch);
                Main.dust[dust].velocity.X = 0f;
                Main.dust[dust].velocity.Y = 3f;
                Main.dust[dust].noGravity = true;
                Main.dust[dust].scale = 1.5f;
            }

            // 3. CONTAGIO SIMULADO
            Rectangle particulasCayendo = new Rectangle(
                (int)npc.position.X,
                (int)npc.position.Y + npc.height,
                npc.width,
                150
            );

            int tipoRana3 = ModContent.NPCType<RanaVenenosa_Tier_3>();
            int tipoRana4 = ModContent.NPCType<RanaVenenosa_Tier_4>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC target = Main.npc[i];

                // Nunca contagiar a NINGUNA rana (ni Tier_3 ni Tier_4)
                if (target.active && !target.friendly && target.whoAmI != npc.whoAmI &&
                    target.type != tipoRana3 && target.type != tipoRana4)
                {
                    if (!target.HasBuff(Type) && target.Hitbox.Intersects(particulasCayendo))
                    {
                        target.buffImmune[Type] = false;
                        target.AddBuff(Type, 600);
                    }
                }
            }
        }
    }
}