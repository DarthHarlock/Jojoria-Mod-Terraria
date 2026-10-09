using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Systems;
using System.IO;

namespace Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_2
{
    public class WhiteSnake_BarragePunch_Tier_2 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public bool ignoreTimeStop = true;
        public static float FadeSpeed = 35f;

        private const int TimeLeftMax = 30;
        private bool offsetRead = false;

        // FIX visual (NO es de red): rotación congelada en el momento del spawn,
        // en vez de copiar la del stand cada frame. Se mantiene igual que antes.
        private float frozenRotation;

        // ---- Variables de red seguras para multijugador (mismo patrón que RIKA_BarragePunch) ----
        public Vector2 punchOffset;
        public float punchAngle;
        public int punchVariant;
        public bool goRight;
        private bool initialized = false;

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;

            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.damage = 0;

            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.timeLeft = TimeLeftMax;
            Projectile.alpha = 0;
        }

        // Sincronización del Host hacia los Clientes (incluye Late-Join)
        public override void SendExtraAI(BinaryWriter writer)
        {
            InitializeData();
            writer.Write(punchOffset.X);
            writer.Write(punchOffset.Y);
            writer.Write(punchAngle);
            writer.Write(punchVariant);
            writer.Write(goRight);
        }

        // Recepción en el Cliente: evita la pérdida del offset por el "Velocity Hack"
        // (Problema 2) y la corrupción del ángulo por compresión de floats (Problema 3).
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            punchOffset.X = reader.ReadSingle();
            punchOffset.Y = reader.ReadSingle();
            punchAngle = reader.ReadSingle();
            punchVariant = reader.ReadInt32();
            goRight = reader.ReadBoolean();
            initialized = true;
        }

        // Decodificación segura de ai[1], hecha una única vez en el Host.
        private void InitializeData()
        {
            if (initialized) return;
            initialized = true;

            int rawAi1Int = (int)Projectile.ai[1];
            float decimalPart = Projectile.ai[1] - rawAi1Int;
            punchAngle = (decimalPart * (MathHelper.TwoPi + 0.01f)) - MathHelper.Pi;

            goRight = rawAi1Int <= 9;
            punchVariant = goRight ? rawAi1Int : rawAi1Int - 10;

            punchOffset = Projectile.velocity;
        }

        public override void AI()
        {
            // Bucle de rescate: soluciona la desincronización de whoAmI cuando un
            // segundo jugador entra tarde a la partida (Problema 1 de la guía).
            int standIndex = (int)Projectile.ai[0];
            Projectile stand = null;

            if (standIndex >= 0 && standIndex < Main.maxProjectiles)
            {
                Projectile p = Main.projectile[standIndex];
                if (p.active && p.owner == Projectile.owner && p.type == ModContent.ProjectileType<WHITESNAKESTAND_Tier_2>())
                {
                    stand = p;
                }
            }

            if (stand == null)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.owner == Projectile.owner && p.type == ModContent.ProjectileType<WHITESNAKESTAND_Tier_2>())
                    {
                        stand = p;
                        Projectile.ai[0] = i; // Corregir el índice localmente para el futuro
                        break;
                    }
                }
            }

            if (stand == null)
            {
                Projectile.Kill();
                return;
            }

            if (!offsetRead)
            {
                offsetRead = true;
                InitializeData();
                Projectile.velocity = Vector2.Zero;

                // FIX visual (NO es de red): se mantiene igual que en el original.
                frozenRotation = stand.rotation;
            }

            InitializeData();

            Vector2 dir = punchAngle.ToRotationVector2();

            int elapsed = TimeLeftMax - Projectile.timeLeft;
            float progress = elapsed * BarrageSystem.PunchSpeed;

            // Posicionamiento basado en variables ya sincronizadas por red.
            Projectile.Center = stand.Center + punchOffset + (dir * progress);

            // FIX visual (NO es de red): rotación congelada, se mantiene igual.
            Projectile.rotation = frozenRotation;

            if (progress >= BarrageSystem.PunchDistance)
            {
                Projectile.alpha += (int)FadeSpeed;

                if (Projectile.alpha >= 255)
                    Projectile.Kill();
            }
            else Projectile.alpha = 0;
        }

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            InitializeData();

            // Antes se leía "(int)Projectile.ai[1]" directo, que en el cliente puede llegar
            // corrompido por la compresión de red. Ahora se usa punchVariant, ya sincronizado.
            string path = punchVariant switch
            {
                1 => "Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_2/WhiteSnake_BarragePunch_2",
                2 => "Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_2/WhiteSnake_BarragePunch_3",
                _ => "Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_2/WhiteSnake_BarragePunch_1",
            };

            Texture2D tex = ModContent.Request<Texture2D>(path).Value;
            Vector2 origin = tex.Size() / 2f;

            // FIX visual (NO es de red): se mantiene la misma lógica que el original,
            // pero usando "goRight" ya sincronizado en vez del ai[1] crudo.
            bool flip = !goRight;

            SpriteEffects fx = flip
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            Main.EntitySpriteDraw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor * (1f - Projectile.alpha / 255f),
                Projectile.rotation,
                origin,
                1f,
                fx,
                0f
            );

            return false;
        }
    }
}