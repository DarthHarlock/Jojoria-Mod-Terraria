using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using System.Collections.Generic;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.Tusk.Tusk_Tier_1.Disparo
{
    public class TuskBala : ModProjectile
    {
        public override string Texture => "Jojo/Content/Projectiles/Tusk/Tusk_Tier_1/Disparo/Bala";

        // Spritesheet horizontal 240x28 = 4 frames de 60x28
        const int FrameCount = 4;
        const int FrameW = 60;
        const int FrameH = 28;
        const int FrameSpeed = 4;

        // ---------- ESTELA (modificable) ----------
        // Duración de cada trozo antes de desaparecer (ticks, 60 = 1 s)
        public const int EstelaDuracionNormal = 18;
        public const int EstelaDuracionPesada = 32;   // la pesada dura más

        // Tamaño de la estela (antes 1 y 1.4)
        public const float EstelaEscalaNormal = 0.7f;
        public const float EstelaEscalaPesada = 1.0f;

        // Cuánto crece EstelaRing mientras se desvanece (1 = no crece)
        public const float RingCrecimiento = 1.6f;

        // Cada cuántas actualizaciones se crea un trozo (1 = estela continua)
        const int EstelaCadaN = 1;

        // ---------- EXPLOSIÓN AL IMPACTAR (modificable) ----------
        const int ExplosionPartículasNormal = 14;
        const int ExplosionPartículasPesada = 28;
        const float ExplosionVelocidadNormal = 4.5f;
        const float ExplosionVelocidadPesada = 7f;

        // Rutas de los sprites (en la carpeta Disparo)
        public const string RutaEstela = "Jojo/Content/Projectiles/Tusk/Tusk_Tier_1/Disparo/Estela";
        public const string RutaEstelaRing = "Jojo/Content/Projectiles/Tusk/Tusk_Tier_1/Disparo/EstelaRing";

        int trailCounter;
        bool nextIsRing; // alterna Estela / EstelaRing

        // ai[0] == 1 -> bala pesada
        bool Heavy => Projectile.ai[0] == 1f;

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = 0;
            Projectile.extraUpdates = 1;
            Projectile.ArmorPenetration = 1000;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;

                if (Heavy)
                {
                    Vector2 c = Projectile.Center;
                    Projectile.width = 40;
                    Projectile.height = 40;
                    Projectile.Center = c;

                    Projectile.scale = 1.5f;
                    Projectile.penetrate = 3;
                    Projectile.usesLocalNPCImmunity = true;
                    Projectile.localNPCHitCooldown = -1;
                }
            }

            // Animación del spritesheet
            if (++Projectile.frameCounter >= FrameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameCount;
            }

            SpawnTrail();

            if (Heavy && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch, Vector2.Zero, 100, default, 1.1f);
                d.noGravity = true;
                d.velocity *= 0.2f;
            }
        }

        // La estela es solo visual: cada cliente la genera por su cuenta
        void SpawnTrail()
        {
            if (Main.netMode == NetmodeID.Server) return;
            if (++trailCounter < EstelaCadaN) return;
            trailCounter = 0;

            TuskEstelaSystem.Add(new TuskEstelaSystem.Segment
            {
                Pos = Projectile.Center,
                Rot = Projectile.velocity.ToRotation(),
                Age = 0,
                Dur = Heavy ? EstelaDuracionPesada : EstelaDuracionNormal,
                Scale = Heavy ? EstelaEscalaPesada : EstelaEscalaNormal,
                Ring = nextIsRing
            });

            nextIsRing = !nextIsRing; // intercala Estela / EstelaRing
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return true;
        }

        // Explosión de partículas celestes al impactar (bloque, enemigo o fin de vida)
        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server) return;

            bool heavy = Heavy;
            int count = heavy ? ExplosionPartículasPesada : ExplosionPartículasNormal;
            float speed = heavy ? ExplosionVelocidadPesada : ExplosionVelocidadNormal;
            Vector2 center = Projectile.Center;

            for (int i = 0; i < count; i++)
            {
                // Reparto circular con algo de variación
                float ang = MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.2f, 0.2f);
                Vector2 vel = ang.ToRotationVector2() * speed * Main.rand.NextFloat(0.4f, 1f);

                int type = Main.rand.NextBool(3) ? DustID.IceTorch : DustID.BlueTorch;
                Dust d = Dust.NewDustPerfect(center, type, vel, 100, default, heavy ? 1.6f : 1.2f);
                d.noGravity = true;
                d.fadeIn = 0.4f;
            }

            // Destello central claro
            for (int i = 0; i < (heavy ? 6 : 3); i++)
            {
                Dust d = Dust.NewDustPerfect(center, DustID.Electric, Main.rand.NextVector2Circular(1.5f, 1.5f), 100, default, heavy ? 1.2f : 0.9f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle src = new Rectangle(Projectile.frame * FrameW, 0, FrameW, FrameH);
            Vector2 origin = new Vector2(FrameW / 2f, FrameH / 2f);

            float rot = Projectile.velocity.ToRotation();
            SpriteEffects fx = SpriteEffects.None;

            // La bala mira a la derecha en el sprite: si va a la izquierda, se espeja
            if (Projectile.velocity.X < 0f)
            {
                rot += MathHelper.Pi;
                fx = SpriteEffects.FlipHorizontally;
            }

            Main.EntitySpriteDraw(
                tex,
                Projectile.Center - Main.screenPosition,
                src,
                Color.White,
                rot,
                origin,
                Projectile.scale,
                fx,
                0f
            );

            return false;
        }
    }

    // ------------------------------------------------------------------
    // Gestor de la estela (en este mismo archivo).
    // ------------------------------------------------------------------
    public class TuskEstelaSystem : ModSystem
    {
        public class Segment
        {
            public Vector2 Pos;
            public float Rot;
            public int Age, Dur;
            public float Scale;
            public bool Ring;
        }

        const int MaxSegments = 600;
        static readonly List<Segment> segments = new();

        public static void Add(Segment s)
        {
            if (segments.Count >= MaxSegments) segments.RemoveAt(0);
            segments.Add(s);
        }

        public override void PostUpdateEverything()
        {
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                segments[i].Age++;
                if (segments[i].Age >= segments[i].Dur)
                    segments.RemoveAt(i);
            }
        }

        public override void OnWorldUnload()
        {
            segments.Clear();
        }

        public override void Unload()
        {
            segments.Clear();
        }

        public override void PostDrawTiles()
        {
            if (segments.Count == 0) return;

            Texture2D texEstela = ModContent.Request<Texture2D>(TuskBala.RutaEstela).Value;
            Texture2D texRing = ModContent.Request<Texture2D>(TuskBala.RutaEstelaRing).Value;

            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            foreach (Segment s in segments)
            {
                float life = MathHelper.Clamp(1f - s.Age / (float)s.Dur, 0f, 1f); // 1 -> 0

                float rot = s.Rot;
                SpriteEffects fx = SpriteEffects.None;

                if (Math.Cos(rot) < 0)
                {
                    rot += MathHelper.Pi;
                    fx = SpriteEffects.FlipHorizontally;
                }

                Texture2D tex = s.Ring ? texRing : texEstela;
                Vector2 scale;

                if (s.Ring)
                {
                    float grow = MathHelper.Lerp(TuskBala.RingCrecimiento, 1f, life);
                    scale = new Vector2(s.Scale * grow);
                }
                else
                {
                    scale = new Vector2(s.Scale, s.Scale * (0.5f + 0.5f * life));
                }

                Main.spriteBatch.Draw(
                    tex,
                    s.Pos - Main.screenPosition,
                    null,
                    Color.White * life,
                    rot,
                    tex.Size() / 2f,
                    scale,
                    fx,
                    0f
                );
            }

            Main.spriteBatch.End();
        }
    }
}