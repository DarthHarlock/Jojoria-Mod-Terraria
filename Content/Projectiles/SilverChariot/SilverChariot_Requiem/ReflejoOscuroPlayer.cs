using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using System;

namespace Jojo.Content.Projectiles.SilverChariot.SilverChariot_Requiem
{
    public class ReflejoOscuroPlayer : ModPlayer
    {
        public bool reflejoActivo;

        public override void ResetEffects()
        {
            reflejoActivo = false;
        }

        public override void PostUpdate()
        {
            if (reflejoActivo)
            {
                // Genera una gran cantidad de partículas oscuras, esta vez más cerca del cuerpo
                int cantidadOscuras = Main.rand.Next(3, 6); // De 3 a 5 partículas por frame
                for (int i = 0; i < cantidadOscuras; i++)
                {
                    // Punto medio: ni tan lejos ni tan pegado, justo envolviendo tu personaje
                    Vector2 offset = Main.rand.NextVector2Circular(Player.width, Player.height);

                    int d = Dust.NewDust(Player.Center + offset, 4, 4, DustID.Granite, 0f, -1f, 150, Color.Black, 1.2f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 0.2f;
                }

                // Partículas Moradas
                if (Main.rand.NextBool(5))
                {
                    Vector2 offset = Main.rand.NextVector2Circular(Player.width, Player.height);

                    int d = Dust.NewDust(Player.Center + offset, 4, 4, DustID.PurpleCrystalShard, 0f, -1f, 100, default, 1f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 0.2f;
                }
            }
        }

        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            if (reflejoActivo)
            {
                // Toma el daño base puro del NPC (ignora tu armadura/defensa) y lo multiplica por 2
                int dañoReflejado = npc.damage * 1;

                NPC.HitInfo hit = new NPC.HitInfo
                {
                    Damage = dañoReflejado,
                    Knockback = 2,
                    HitDirection = Math.Sign(npc.Center.X - Player.Center.X)
                };
                npc.StrikeNPC(hit);

                // Empuje al personaje
                Vector2 direccionEmpuje = npc.Center.DirectionTo(Player.Center);
                Player.velocity = direccionEmpuje * 10f;

                // Anula el daño al jugador
                modifiers.SetMaxDamage(0);
            }
        }

        public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers)
        {
            if (reflejoActivo)
            {
                // Toma el daño puro original del proyectil y lo multiplica por 2
                int dañoReflejado = proj.damage * 2;

                NPC objetivo = null;
                float distanciaMinima = 1500f;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.active && !n.friendly && !n.dontTakeDamage)
                    {
                        float distancia = Vector2.Distance(Player.Center, n.Center);
                        if (distancia < distanciaMinima)
                        {
                            distanciaMinima = distancia;
                            objetivo = n;
                        }
                    }
                }

                if (objetivo != null)
                {
                    NPC.HitInfo hit = new NPC.HitInfo
                    {
                        Damage = dañoReflejado,
                        Knockback = 0,
                        HitDirection = Math.Sign(objetivo.Center.X - Player.Center.X)
                    };
                    objetivo.StrikeNPC(hit);
                }

                // Empuje desde la posición del proyectil
                Vector2 direccionEmpuje = proj.Center.DirectionTo(Player.Center);
                Player.velocity = direccionEmpuje * 10f;

                // Anula el daño que recibe el jugador
                modifiers.SetMaxDamage(0);
            }
        }
    }
}