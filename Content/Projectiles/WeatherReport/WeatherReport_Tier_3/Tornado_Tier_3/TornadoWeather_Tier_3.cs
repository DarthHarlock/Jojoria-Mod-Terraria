using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System;
using System.IO;
using Jojo.Content.Clases;
using Jojo.Content.Systems;

namespace Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_3.Tornado_Tier_3
{
    public class TornadoWeather_Tier_3 : ModProjectile
    {
        private int GetTornadoDust()
        {
            return DustID.Cloud;
        }

        public int GrowTime = 70;
        public int Duration = 420;
        public int DyingTime = 80;
        public int SizeSegments = 12;
        public int TornadoBaseDamage = 70;
        public int TickDamageCooldown = 20;
        public float Scale = 1f;
        public int baseCritChance = 5;
        public float BottomScale = 0.5f;
        public float TopScale = 1.0f;
        public int SpinAnimationDelay = 3;
        public int OpacityPercent = 70;
        public float WavePhaseStep = 0.9f;
        public float WaveSpeed = 0.12f;
        public float WaveAmplitude = 35f;

        const int FrameWidth = 162;
        const int FrameHeight = 44;
        const int TotalFrames = 6;

        const string TexturePath = "Jojo/Content/Projectiles/WeatherReport/WeatherReport_Tier_3/Tornado_Tier_3/TornadoWeather_Tier_3";

        bool init;
        float initialX;
        float anchorX;
        float baseBottomY;
        int firstVisible;
        int lastVisible;
        int spinTimer;
        int spinFrame;

        int prevFirstVisible;
        int prevLastVisible;

        float[] segYOffsets;
        float[] segScales;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(GrowTime);
            writer.Write(Duration);
            writer.Write(DyingTime);
            writer.Write(SizeSegments);
            writer.Write(TornadoBaseDamage);
            writer.Write(TickDamageCooldown);
            writer.Write(Scale);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            GrowTime = reader.ReadInt32();
            Duration = reader.ReadInt32();
            DyingTime = reader.ReadInt32();
            SizeSegments = reader.ReadInt32();
            TornadoBaseDamage = reader.ReadInt32();
            TickDamageCooldown = reader.ReadInt32();
            Scale = reader.ReadSingle();
        }

        public override void SetDefaults()
        {
            Projectile.width = FrameWidth;
            Projectile.height = FrameHeight;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            if (!p.active || p.dead)
            {
                Projectile.Kill();
                return;
            }

            int dustType = GetTornadoDust();

            if (!init)
            {
                init = true;
                initialX = Projectile.Center.X;
                anchorX = initialX;
                baseBottomY = Projectile.Center.Y;
                Projectile.localNPCHitCooldown = Math.Max(1, TickDamageCooldown);
                Projectile.timeLeft = GrowTime + Duration + DyingTime;

                prevFirstVisible = 0;
                prevLastVisible = 0;

                segYOffsets = new float[SizeSegments];
                segScales = new float[SizeSegments];

                float currentY = 0f;
                for (int j = 0; j < SizeSegments; j++)
                {
                    float t = SizeSegments <= 1 ? 1f : j / (float)(SizeSegments - 1);
                    segScales[j] = MathHelper.Lerp(BottomScale, TopScale, t) * Scale;
                    float segH = FrameHeight * segScales[j];

                    segYOffsets[j] = currentY + (segH / 2f);
                    currentY += segH;
                }

                for (int d = 0; d < 45; d++)
                {
                    Vector2 velocity = new Vector2(0, -9f).RotatedBy(MathHelper.ToRadians(d * 8));
                    Dust dust = Dust.NewDustPerfect(new Vector2(anchorX, baseBottomY), dustType, velocity, 100, default, 3.5f);
                    dust.noGravity = true;
                    dust.velocity *= Main.rand.NextFloat(1f, 2f);
                }
            }

            spinTimer++;
            spinFrame = (spinTimer / SpinAnimationDelay) % TotalFrames;

            anchorX = initialX + (float)Math.Sin(spinTimer * 0.015f) * 45f;

            int totalLife = GrowTime + Duration + DyingTime;
            int elapsed = totalLife - Projectile.timeLeft;

            bool growing = elapsed < GrowTime;
            bool dying = elapsed >= GrowTime + Duration;

            if (growing)
            {
                float growProgress = MathHelper.Clamp(elapsed / (float)Math.Max(1, GrowTime), 0f, 1f);
                firstVisible = 0;
                lastVisible = Math.Max(1, (int)Math.Ceiling(growProgress * SizeSegments));
            }
            else if (dying)
            {
                float dyingElapsed = elapsed - (GrowTime + Duration);
                float dyingProgress = MathHelper.Clamp(dyingElapsed / (float)Math.Max(1, DyingTime), 0f, 1f);
                firstVisible = Math.Min(SizeSegments, (int)Math.Floor(dyingProgress * SizeSegments));
                lastVisible = SizeSegments;
            }
            else
            {
                firstVisible = 0;
                lastVisible = SizeSegments;
            }

            int segCount = Math.Max(0, lastVisible - firstVisible);

            if (Main.netMode != NetmodeID.Server)
            {
                if (lastVisible > prevLastVisible)
                {
                    for (int j = prevLastVisible; j < lastVisible; j++)
                    {
                        SpawnSegmentParticles(j, dustType);
                    }
                }
                if (firstVisible > prevFirstVisible)
                {
                    for (int j = prevFirstVisible; j < firstVisible; j++)
                    {
                        SpawnSegmentParticles(j, dustType);
                    }
                }
            }

            prevFirstVisible = firstVisible;
            prevLastVisible = lastVisible;

            Projectile.friendly = segCount > 0;

            float activeHeight = 0f;
            float maxWidth = 0f;
            float topY = baseBottomY;

            for (int j = 0; j < lastVisible; j++)
            {
                float segH = FrameHeight * segScales[j];
                float segW = FrameWidth * segScales[j];
                topY -= segH;

                if (j >= firstVisible)
                {
                    activeHeight += segH;
                    if (segW > maxWidth) maxWidth = segW;
                }
            }

            Projectile.width = Math.Max(2, (int)maxWidth);
            Projectile.height = Math.Max(2, (int)activeHeight);
            Projectile.position.X = anchorX - Projectile.width / 2f;
            Projectile.position.Y = topY;

            if (Projectile.friendly)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.active && !n.friendly && !n.dontTakeDamage && Projectile.Hitbox.Intersects(n.Hitbox))
                    {
                        bool canMove = n.realLife == -1 &&
                                       n.aiStyle != NPCAIStyleID.Worm &&
                                       n.aiStyle != NPCAIStyleID.TheDestroyer;

                        if (n.type == NPCID.WallofFlesh || n.type == NPCID.WallofFleshEye || n.type == NPCID.TargetDummy || n.type == NPCID.EaterofWorldsHead || n.type == NPCID.EaterofWorldsBody || n.type == NPCID.EaterofWorldsTail)
                        {
                            canMove = false;
                        }

                        if (canMove)
                        {
                            n.velocity.X = (float)Math.Sin(Main.GameUpdateCount * 0.4f + n.whoAmI) * 7.5f;

                            float tornadoTop = baseBottomY - activeHeight;
                            float alturaMaximaPermitida = tornadoTop + (activeHeight * 0.2f);

                            if (n.Center.Y > alturaMaximaPermitida)
                            {
                                n.velocity.Y -= 0.6f;
                                if (n.velocity.Y < -8f) n.velocity.Y = -8f;
                            }
                            else
                            {
                                n.velocity.Y += 2f;
                                if (n.velocity.Y > 10f) n.velocity.Y = 10f;
                            }
                        }
                    }
                }
            }

            if (segCount > 0 && Main.rand.NextBool(2))
            {
                for (int j = firstVisible; j < lastVisible; j++)
                {
                    if (Main.rand.NextBool(2))
                    {
                        float wavePhase = (j * WavePhaseStep) - (spinTimer * WaveSpeed);
                        float wave = (float)Math.Sin(wavePhase) * WaveAmplitude * Scale;

                        Vector2 segCenter = new Vector2(anchorX + wave, baseBottomY - segYOffsets[j]);
                        float currentSegWidth = FrameWidth * segScales[j];

                        Vector2 dustPos = segCenter + new Vector2(Main.rand.NextFloat(-currentSegWidth / 2.5f, currentSegWidth / 2.5f), Main.rand.NextFloat(-FrameHeight / 2f, FrameHeight / 2f));

                        Dust dust = Dust.NewDustPerfect(dustPos, dustType, new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-6f, -2f)), 100, default, 1.5f + segScales[j]);
                        dust.noGravity = true;
                    }
                }
            }

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);

            // Escalar el daño base (TornadoBaseDamage) con el multiplicador de ClaseStand
            Projectile.damage = (int)p.GetDamage<ClaseStand>().ApplyTo(TornadoBaseDamage);
        }

        private void SpawnSegmentParticles(int j, int dustType)
        {
            float wavePhase = (j * WavePhaseStep) - (spinTimer * WaveSpeed);
            float wave = (float)Math.Sin(wavePhase) * WaveAmplitude * Scale;
            Vector2 segCenter = new Vector2(anchorX + wave, baseBottomY - segYOffsets[j]);

            for (int d = 0; d < 12; d++)
            {
                Vector2 vel = new Vector2(Main.rand.NextFloat(4f, 9f), Main.rand.NextFloat(-1.5f, 1.5f));
                Dust dust = Dust.NewDustPerfect(segCenter, dustType, vel, 100, default, 2f + (segScales[j] * 0.5f));
                dust.noGravity = true;
            }

            for (int d = 0; d < 12; d++)
            {
                Vector2 vel = new Vector2(Main.rand.NextFloat(-9f, -4f), Main.rand.NextFloat(-1.5f, 1.5f));
                Dust dust = Dust.NewDustPerfect(segCenter, dustType, vel, 100, default, 2f + (segScales[j] * 0.5f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (segYOffsets == null || segScales == null) return false;

            Texture2D tex = ModContent.Request<Texture2D>(TexturePath).Value;
            Vector2 origin = new Vector2(FrameWidth / 2f, FrameHeight / 2f);

            Rectangle src = new Rectangle(0, (TotalFrames - 1 - spinFrame) * FrameHeight, FrameWidth, FrameHeight);

            float alphaMult = MathHelper.Clamp(OpacityPercent / 100f, 0f, 1f);
            Color drawColor = lightColor * alphaMult;

            for (int i = firstVisible; i < lastVisible; i++)
            {
                float baseSegScale = segScales[i];

                float wavePhase = (i * WavePhaseStep) - (spinTimer * WaveSpeed);
                float sinVal = (float)Math.Sin(wavePhase);

                float wave = sinVal * WaveAmplitude * Scale;

                Vector2 drawScale = new Vector2(baseSegScale, baseSegScale);

                Vector2 segPos = new Vector2(
                    Projectile.Center.X + wave,
                    baseBottomY - segYOffsets[i]
                ) - Main.screenPosition;

                Main.EntitySpriteDraw(tex, segPos, src, drawColor, 0f, origin, drawScale, SpriteEffects.None, 0f);
            }

            return false;
        }
    }
}