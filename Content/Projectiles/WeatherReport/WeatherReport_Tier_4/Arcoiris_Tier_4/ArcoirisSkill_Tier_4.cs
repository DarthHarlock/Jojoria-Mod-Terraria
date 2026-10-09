using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Buffs.WeatherReport_Buffs;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_4.Arcoiris_Tier_4
{
    public class ArcoirisSkill_Tier_4 : ModPlayer
    {
        const int DuracionTicks = 1200;
        const int CantidadOleadas = 40;
        const int ArcoirisPorOleada = 2;
        const float RadioHorizontal = 800f;
        const float DamageArcoirisBase = 90f; // Daño actualizado a 40
        const float KnockbackArcoiris = 4f;
        const float AnguloMinimoGrados = 35f;
        const float AnguloMaximoGrados = 75f;
        const float VelocidadCaidaMin = 12f;
        const float VelocidadCaidaMax = 18f;
        const float EscalaMaxima = 0.75f;
        public const float AlturaMinNubes = 300f;
        public const float AlturaMaxNubes = 500f;
        const int CantidadNubesCielo = 105;

        int arcoirisSpawneados;

        public static void Start(Player p)
        {
            int buffType = ModContent.BuffType<DuracionArcoiris>();
            if (p.HasBuff(buffType)) return;

            var skill = p.GetModPlayer<ArcoirisSkill_Tier_4>();
            skill.arcoirisSpawneados = 0;

            p.AddBuff(buffType, DuracionTicks);

            if (p.whoAmI == Main.myPlayer)
            {
                float espacioEntreNubes = 3000f / CantidadNubesCielo;

                for (int i = 0; i < CantidadNubesCielo; i++)
                {
                    float offsetX = -1500f + (i * espacioEntreNubes) + Main.rand.NextFloat(-15f, 15f);
                    float offsetY = Main.rand.NextFloat(AlturaMinNubes, AlturaMaxNubes);
                    float velX = Main.rand.NextFloat(0.1f, 0.45f) * (Main.rand.NextBool() ? 1 : -1);
                    int texturaRandom = Main.rand.Next(1, 5);

                    Projectile.NewProjectile(
                        p.GetSource_Misc("Arcoiris_Tier_4"),
                        p.Center.X + offsetX,
                        p.Center.Y - offsetY,
                        velX, 0f,
                        ModContent.ProjectileType<NubeCielo_Arcoiris_Tier_4>(),
                        0, 0f, p.whoAmI,
                        texturaRandom, offsetY
                    );
                }
            }
        }

        public override void PreUpdate()
        {
            int buffType = ModContent.BuffType<DuracionArcoiris>();
            bool hasBuff = Player.HasBuff(buffType);

            // 1. LÓGICA VISUAL Y DE FÍSICAS (Debe correr en TODOS los clientes para el Multijugador)
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];

                if (proj.active && proj.owner == Player.whoAmI)
                {
                    if (proj.type == ProjectileID.RainbowFront || proj.type == ProjectileID.RainbowBack)
                    {
                        if (!hasBuff)
                        {
                            if (proj.timeLeft > 45) proj.timeLeft = 45;
                        }
                        else
                        {
                            if (proj.type == ProjectileID.RainbowFront)
                            {
                                if (proj.velocity.LengthSquared() < 0.5f && proj.timeLeft > 60)
                                    proj.timeLeft = 60;
                            }

                            if (proj.type == ProjectileID.RainbowBack && proj.timeLeft > 150)
                            {
                                proj.timeLeft = 150;
                            }
                        }

                        if (proj.timeLeft <= 60)
                        {
                            float progreso = 1f - (proj.timeLeft / 60f);
                            proj.alpha = (int)MathHelper.Clamp(progreso * 255f, 0f, 255f);
                        }
                    }
                }
            }

            // 2. LÓGICA DE SPAWNEO (Solo la corre el dueño para no duplicar proyectiles)
            if (Player.whoAmI != Main.myPlayer) return;

            int buffIndex = Player.FindBuffIndex(buffType);
            if (buffIndex < 0)
            {
                arcoirisSpawneados = 0;
                return;
            }

            int tiempoRestante = Player.buffTime[buffIndex];
            int tiempoTranscurrido = DuracionTicks - tiempoRestante;
            int intervalo = Math.Max(1, DuracionTicks / CantidadOleadas);
            int oleadasQueDeberianHaberOcurrido = Math.Min(CantidadOleadas, tiempoTranscurrido / intervalo + 1);
            int arcoirisQueDeberianHaberCaido = oleadasQueDeberianHaberOcurrido * ArcoirisPorOleada;

            while (arcoirisSpawneados < arcoirisQueDeberianHaberCaido)
            {
                SpawnArcoiris();
                arcoirisSpawneados++;
            }
        }

        void SpawnArcoiris()
        {
            Player p = Player;

            float x = p.Center.X + Main.rand.NextFloat(-RadioHorizontal, RadioHorizontal);
            float y = p.Center.Y - Main.rand.NextFloat(AlturaMinNubes, AlturaMaxNubes);
            float signo = Main.rand.NextBool() ? 1f : -1f;
            float anguloGrados = Main.rand.NextFloat(AnguloMinimoGrados, AnguloMaximoGrados);
            float anguloRadianes = MathHelper.ToRadians(anguloGrados);

            Vector2 direccionBase = Vector2.UnitX.RotatedBy(anguloRadianes);
            Vector2 direccion = new Vector2(direccionBase.X * signo, direccionBase.Y);
            float velocidad = Main.rand.NextFloat(VelocidadCaidaMin, VelocidadCaidaMax);
            Vector2 velocidadFinal = direccion * velocidad;

            int damageFinal = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(DamageArcoirisBase);

            int indiceArcoiris = Projectile.NewProjectile(
                p.GetSource_Misc("Arcoiris_Tier_4"),
                x, y,
                velocidadFinal.X, velocidadFinal.Y,
                ProjectileID.RainbowFront,
                damageFinal,
                KnockbackArcoiris,
                p.whoAmI
            );

            if (indiceArcoiris >= 0 && indiceArcoiris < Main.maxProjectiles)
            {
                Projectile nuevoArcoiris = Main.projectile[indiceArcoiris];
                nuevoArcoiris.DamageType = ModContent.GetInstance<ClaseStand>(); // Asigna explícitamente el tipo de daño al proyectil
                nuevoArcoiris.timeLeft = 300;
                nuevoArcoiris.scale = MathHelper.Min(nuevoArcoiris.scale, EscalaMaxima);
                nuevoArcoiris.alpha = 0;
            }
        }
    }
}