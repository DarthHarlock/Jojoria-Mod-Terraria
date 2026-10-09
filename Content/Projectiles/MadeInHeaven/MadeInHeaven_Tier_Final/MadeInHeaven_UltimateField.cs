using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using System;
using Jojo.Content.Buffs.MadeInHeaven_Buffs;
using Jojo.Content.Buffs;

namespace Jojo.Content.Projectiles.MadeInHeaven.MadeInHeaven_Tier_Final
{
    public class MadeInHeavenTimeSystem : ModSystem
    {
        public static bool IsActive
        {
            get
            {
                if (Main.gameMenu) return false;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p.active && !p.dead && p.HasBuff(ModContent.BuffType<ResetUniversal>()))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public static double OriginalTime;
        public static bool OriginalDayTime;
        private static bool wasActive = false;

        // AQUÍ ESTÁ LA VARIABLE QUE HACÍA CRASHEAR TU STAND. DEVUELTA Y FUNCIONAL.
        public static float TotalCooldown1ReductionPercent = 0f;

        private const float StartTimeRate = 10f;
        private const float MaxTimeRate = 2200f;
        private static float currentTimeRate = StartTimeRate;
        private static bool lastDayTimeState;

        private static bool cachedBloodMoon;
        private static bool cachedEclipse;
        private static bool cachedPumpkinMoon;
        private static bool cachedSnowMoon;
        private static bool cachedSlimeRain;
        private static bool cachedRaining;
        private static float cachedMaxRaining;
        private static bool cachedDownedBoss2;

        public override void ModifyTimeRate(ref double timeRate, ref double tileUpdateRate, ref double eventUpdateRate)
        {
            if (!IsActive) return;
            // Congelamos el tiempo nativo del motor de terraria
            timeRate = 0;
            tileUpdateRate = 0;
            eventUpdateRate = 0;
        }

        // IMPORTANTE: PreUpdateEntities corre en CLIENTE y SERVIDOR. Aquí es donde se arregla el sol del Multi.
        public override void PreUpdateEntities()
        {
            if (IsActive && !wasActive)
            {
                OriginalTime = Main.time;
                OriginalDayTime = Main.dayTime;
                lastDayTimeState = Main.dayTime;

                cachedBloodMoon = Main.bloodMoon;
                cachedEclipse = Main.eclipse;
                cachedPumpkinMoon = Main.pumpkinMoon;
                cachedSnowMoon = Main.snowMoon;
                cachedSlimeRain = Main.slimeRain;
                cachedRaining = Main.raining;
                cachedMaxRaining = Main.maxRaining;

                cachedDownedBoss2 = NPC.downedBoss2;
                NPC.downedBoss2 = false;

                currentTimeRate = StartTimeRate;
                TotalCooldown1ReductionPercent = 0f;
                wasActive = true;
            }
            else if (IsActive)
            {
                currentTimeRate = Math.Min(currentTimeRate * 1.02f, MaxTimeRate);

                // AVANCE MANUAL DEL SOL EN LAS PANTALLAS DE TODOS LOS JUGADORES (Fix Multijugador)
                Main.time += currentTimeRate;

                // Ciclo Día -> Noche
                if (Main.dayTime && Main.time >= 54000.0)
                {
                    Main.time -= 54000.0;
                    Main.dayTime = false;
                }
                // Ciclo Noche -> Día
                else if (!Main.dayTime && Main.time >= 32400.0)
                {
                    Main.time -= 32400.0;
                    Main.dayTime = true;
                }

                // Detectamos el cambio de ciclo visualmente
                if (Main.dayTime != lastDayTimeState)
                {
                    lastDayTimeState = Main.dayTime;
                    TriggerCycleChange();
                }

                // El servidor envía un pulso para asegurar que nadie se desincroniza (2 veces por segundo)
                if (Main.netMode == NetmodeID.Server && Main.GameUpdateCount % 30 == 0)
                {
                    NetMessage.SendData(MessageID.WorldData);
                }
            }
            else if (!IsActive && wasActive)
            {
                RestoreWorldState();
                TotalCooldown1ReductionPercent = 0f;
                wasActive = false;
            }
        }

        private static void TriggerCycleChange()
        {
            if (Main.dayTime == OriginalDayTime)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    SoundEngine.PlaySound(new SoundStyle("Jojo/Content/Sonidos/MihAceleration"));
                }
            }

            // Actualizamos la variable para que tu Stand reduzca su delay
            if (TotalCooldown1ReductionPercent < 3.0f)
            {
                TotalCooldown1ReductionPercent += 0.04f;
                if (TotalCooldown1ReductionPercent > 3.0f) TotalCooldown1ReductionPercent = 3.0f;
            }

            // Quitamos cooldown activo masivamente al jugador cada vez que pasa un ciclo
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.active && p.HasBuff(ModContent.BuffType<ResetUniversal>()))
                {
                    int buffIndex = p.FindBuffIndex(ModContent.BuffType<Cooldown1>());
                    if (buffIndex != -1)
                    {
                        // 1200 ticks = 20 segundos reales menos por cada día/noche que pase
                        p.buffTime[buffIndex] -= 1200;
                        if (p.buffTime[buffIndex] <= 0) p.DelBuff(buffIndex);
                    }
                }
            }
        }

        public override void PostUpdateWorld()
        {
            if (IsActive)
            {
                Main.bloodMoon = false;
                Main.eclipse = false;
                Main.pumpkinMoon = false;
                Main.snowMoon = false;
                Main.slimeRain = false;
                Main.raining = false;
                Main.maxRaining = 0f;
            }
        }

        public override void PreSaveAndQuit()
        {
            ResetEverything();
        }

        public override void OnWorldUnload()
        {
            ResetEverything();
        }

        private void RestoreWorldState()
        {
            Main.time = OriginalTime;
            Main.dayTime = OriginalDayTime;

            Main.bloodMoon = cachedBloodMoon;
            Main.eclipse = cachedEclipse;
            Main.pumpkinMoon = cachedPumpkinMoon;
            Main.snowMoon = cachedSnowMoon;
            Main.slimeRain = cachedSlimeRain;
            Main.raining = cachedRaining;
            Main.maxRaining = cachedMaxRaining;

            NPC.downedBoss2 = cachedDownedBoss2;

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.WorldData);
            }
        }

        private void ResetEverything()
        {
            if (wasActive)
            {
                RestoreWorldState();
            }
            currentTimeRate = StartTimeRate;
            TotalCooldown1ReductionPercent = 0f;
            wasActive = false;
        }
    }

    public class MadeInHeaven_UltimateField : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 99999;
            Projectile.hide = true;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            if (Main.myPlayer == Projectile.owner)
            {
                if (!player.HasBuff(ModContent.BuffType<ResetUniversal>()) || !player.active || player.dead)
                {
                    Projectile.Kill();
                    return;
                }
                Projectile.timeLeft = 2;
            }
            else
            {
                Projectile.timeLeft = 2;
                if (!player.active || player.dead) Projectile.Kill();
            }

            Projectile.Center = player.Center;

            Rectangle visualField = new Rectangle((int)player.Center.X - 1200, (int)player.Center.Y - 700, 2400, 1400);

            if (Main.rand.NextBool(2))
            {
                Vector2 spawnPos = player.Center + Main.rand.NextVector2Circular(1100f, 650f);
                if (visualField.Contains(spawnPos.ToPoint()))
                {
                    Dust speedDust = Dust.NewDustPerfect(spawnPos, DustID.Cloud, new Vector2(Main.rand.NextFloat(4f, 9f) * player.direction, 0f), 100, Color.White, 1.3f);
                    speedDust.noGravity = true;
                }
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.type != NPCID.TargetDummy && npc.lifeMax > 5)
                {
                    if (visualField.Intersects(npc.getRect()))
                    {
                        npc.GetGlobalNPC<MIHLevitationNPC>().ActivateLevitation();
                    }
                }
            }
        }
    }

    public class MIHLevitationNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public int levitationTimer = 0;
        public bool wasInLevitation = false;
        public bool cachedNoGravity = false;

        private double fieldTimeCache;
        private bool fieldDayTimeCache;
        private bool trickedTime = false;

        public void ActivateLevitation() => levitationTimer = 5;

        public override bool PreAI(NPC npc)
        {
            if (MadeInHeavenTimeSystem.IsActive)
            {
                fieldTimeCache = Main.time;
                fieldDayTimeCache = Main.dayTime;
                Main.time = MadeInHeavenTimeSystem.OriginalTime;
                Main.dayTime = MadeInHeavenTimeSystem.OriginalDayTime;
                trickedTime = true;
            }

            if (levitationTimer > 0)
            {
                if (!wasInLevitation)
                {
                    cachedNoGravity = npc.noGravity;
                    wasInLevitation = true;
                }

                levitationTimer--;

                if (npc.type == NPCID.WallofFlesh)
                {
                    RestoreTimeHack();
                    return base.PreAI(npc);
                }

                npc.noGravity = true;
                npc.velocity.X *= 0.90f;
                npc.velocity.Y = -0.5f;

                if (float.IsNaN(npc.velocity.X) || float.IsNaN(npc.velocity.Y)) npc.velocity = Vector2.Zero;
                npc.rotation += (npc.direction * 0.015f);

                RestoreTimeHack();
                return false;
            }
            else if (wasInLevitation)
            {
                npc.noGravity = cachedNoGravity;
                if (npc.type != NPCID.WallofFlesh)
                {
                    npc.rotation = 0f;
                    npc.velocity.X *= 0.5f;
                }
                wasInLevitation = false;
                if (Main.netMode != NetmodeID.SinglePlayer) npc.netUpdate = true;
            }

            return base.PreAI(npc);
        }

        public override void PostAI(NPC npc)
        {
            RestoreTimeHack();
        }

        private void RestoreTimeHack()
        {
            if (trickedTime)
            {
                Main.time = fieldTimeCache;
                Main.dayTime = fieldDayTimeCache;
                trickedTime = false;
            }
        }

        public override bool CheckActive(NPC npc)
        {
            if (MadeInHeavenTimeSystem.IsActive) return false;
            return base.CheckActive(npc);
        }
    }
}