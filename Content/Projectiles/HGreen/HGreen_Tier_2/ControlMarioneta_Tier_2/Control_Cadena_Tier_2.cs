using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_2;
using Jojo.Content.Systems;

namespace Jojo.Content.Projectiles.HGreen.HGreen_Tier_2.ControlMarioneta_Tier_2
{
    public class Control_Cadena_Tier_2 : ModProjectile
    {
        public const int Segments = 5;
        public const float FixedSegmentLength = 65f;

        public static float MaxChainDistance => (Segments - 1) * FixedSegmentLength;

        // [CORRECCIÓN]: Duración propia del tether para este tier (súbela respecto al Tier 1 si quieres)
        public const int TetherDuration = 240; // 4 segundos

        const int SimIterations = 4;
        static readonly Vector2 Gravity = new(0f, 0.45f);
        static readonly Color NeonGreen = new Color(60, 255, 90);

        Vector2[] points;
        Vector2[] prevPoints;
        float segmentLength;
        bool init;
        int graceTimer;

        bool isRetracting;
        float retractProgress;
        Vector2 lastTargetPos;

        NPC Target
        {
            get
            {
                int idx = (int)Projectile.ai[0];
                if (idx >= 0 && idx < Main.maxNPCs)
                    return Main.npc[idx];
                return null;
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            // [CORRECCIÓN]: Usa la duración propia de este tier
            Projectile.timeLeft = TetherDuration + 30;
        }

        void InitPoints(Vector2 start, Vector2 end)
        {
            points = new Vector2[Segments];
            prevPoints = new Vector2[Segments];

            for (int i = 0; i < Segments; i++)
            {
                points[i] = Vector2.Lerp(start, end, i / (float)(Segments - 1));
                prevPoints[i] = points[i];
            }

            segmentLength = FixedSegmentLength;
        }

        Projectile FindOwnerStand()
        {
            int standType = ModContent.ProjectileType<HGREENSTAND_Tier_2>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == Projectile.owner && proj.type == standType)
                    return proj;
            }
            return null;
        }

        public override void AI()
        {
            Projectile stand = FindOwnerStand();
            if (stand == null)
            {
                if (Projectile.owner == Main.myPlayer)
                {
                    Projectile.Kill();
                    return;
                }
                else
                {
                    graceTimer++;
                    if (graceTimer > 20)
                    {
                        Projectile.Kill();
                        return;
                    }
                    return;
                }
            }
            graceTimer = 0;

            Vector2 start = stand.Center;
            Vector2 end = start;
            NPC target = Target;

            bool shouldRetract = false;
            if (target == null || !target.active)
            {
                shouldRetract = true;
            }
            else
            {
                var marioneta = target.GetGlobalNPC<MarionetaGlobalNPC>();
                if (!marioneta.tethered || marioneta.tetherPlayer != Projectile.owner)
                {
                    shouldRetract = true;
                }
            }

            if (shouldRetract && !isRetracting)
            {
                isRetracting = true;
                if (points != null)
                    lastTargetPos = points[Segments - 1];
                else
                    lastTargetPos = start;
            }

            if (!isRetracting)
            {
                segmentLength = FixedSegmentLength;

                Vector2 targetOffset = Vector2.Zero;
                float offX = target.width * 0.45f;
                float offY = target.height * 0.45f;

                if (Projectile.ai[1] == 0)
                    targetOffset = new Vector2(0, -offY);
                else if (Projectile.ai[1] == 1)
                    targetOffset = new Vector2(-offX, offY);
                else if (Projectile.ai[1] == 2)
                    targetOffset = new Vector2(offX, offY);

                end = target.Center + targetOffset;
                lastTargetPos = end;
            }
            else
            {
                retractProgress += 0.08f;
                if (retractProgress >= 1f)
                {
                    Projectile.Kill();
                    return;
                }
                end = Vector2.Lerp(lastTargetPos, start, retractProgress);
                segmentLength = Vector2.Distance(start, end) / (Segments - 1);
            }

            if (!init)
            {
                init = true;
                InitPoints(start, end);
            }

            SimulateRope(start, end);
            SpawnChainDust();

            Projectile.Center = end;
            Projectile.timeLeft = 2;
        }

        void SimulateRope(Vector2 start, Vector2 end)
        {
            for (int i = 1; i < Segments - 1; i++)
            {
                Vector2 vel = (points[i] - prevPoints[i]) * 0.98f;
                prevPoints[i] = points[i];
                points[i] += vel + Gravity;
            }

            points[0] = start;
            points[Segments - 1] = end;
            prevPoints[0] = start;
            prevPoints[Segments - 1] = end;

            for (int iter = 0; iter < SimIterations; iter++)
            {
                for (int i = 0; i < Segments - 1; i++)
                {
                    Vector2 a = points[i];
                    Vector2 b = points[i + 1];
                    Vector2 delta = b - a;
                    float dist = delta.Length();
                    if (dist < 0.0001f) continue;

                    float diff = (dist - segmentLength) / dist;
                    Vector2 correction = delta * 0.5f * diff;

                    if (i != 0) points[i] += correction;
                    if (i + 1 != Segments - 1) points[i + 1] -= correction;
                }

                points[0] = start;
                points[Segments - 1] = end;
            }
        }

        void SpawnChainDust()
        {
            if (Main.netMode == NetmodeID.Server || points == null) return;
            if (!Main.rand.NextBool(3)) return;

            int idx = Main.rand.Next(1, Segments - 1);
            Vector2 pos = points[idx];

            Dust dust = Dust.NewDustDirect(pos, 2, 2, DustID.GreenFairy, 0f, 0f, 100, default, 0.65f);
            dust.noGravity = true;
            dust.color = NeonGreen;
            dust.velocity *= 0.3f;
            dust.fadeIn = 0.4f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (points == null) return false;

            Texture2D tex = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/HGreen/HGreen_Tier_2/ControlMarioneta_Tier_2/Control_Cadena_Tier_2").Value;
            Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);

            for (int i = 0; i < Segments - 1; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[i + 1];
                Vector2 mid = (a + b) / 2f - Main.screenPosition;

                float rot = (b - a).ToRotation() - MathHelper.PiOver2;
                float length = Vector2.Distance(a, b);

                Vector2 scale = new Vector2(1f, length / tex.Height);

                Main.EntitySpriteDraw(
                    tex,
                    mid,
                    null,
                    Color.White,
                    rot,
                    origin,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }

            return false;
        }
    }
}