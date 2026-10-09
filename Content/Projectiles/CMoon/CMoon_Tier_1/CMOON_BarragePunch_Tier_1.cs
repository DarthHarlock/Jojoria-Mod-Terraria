using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Systems;
using System.IO;

namespace Jojo.Content.Projectiles.CMoon.CMoon_Tier_1
{
    public class CMOON_BarragePunch_Tier_1 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public bool ignoreTimeStop = true;
        public static float FadeSpeed = 35f;

        private const int TimeLeftMax = 30;
        private bool offsetRead = false;

        // Variables de red seguras para multijugador (Ahora incluye goRight)
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

        // Sincronización del Host hacia los Clientes (J2)
        public override void SendExtraAI(BinaryWriter writer)
        {
            InitializeData();
            writer.Write(punchOffset.X);
            writer.Write(punchOffset.Y);
            writer.Write(punchAngle);
            writer.Write(punchVariant);
            writer.Write(goRight); // Sincronizado
        }

        // Recepción en el Cliente (J2) para evitar pérdida de decimales y offset
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            punchOffset.X = reader.ReadSingle();
            punchOffset.Y = reader.ReadSingle();
            punchAngle = reader.ReadSingle();
            punchVariant = reader.ReadInt32();
            goRight = reader.ReadBoolean(); // Sincronizado
            initialized = true;
        }

        private void InitializeData()
        {
            if (initialized) return;
            initialized = true;

            // Decodificación inicial segura en el Host
            int rawAi1Int = (int)Projectile.ai[1];
            float decimalPart = Projectile.ai[1] - rawAi1Int;
            punchAngle = (decimalPart * (MathHelper.TwoPi + 0.01f)) - MathHelper.Pi;

            // Almacenamos si mira a la derecha en el momento de nacer
            goRight = rawAi1Int <= 9;
            punchVariant = goRight ? rawAi1Int : rawAi1Int - 10;

            punchOffset = Projectile.velocity;
        }

        public override void AI()
        {
            // Búsqueda a prueba de fallos del Stand en multijugador (por si el índice whoAmI cambia)
            int standIndex = (int)Projectile.ai[0];
            Projectile stand = null;

            if (standIndex >= 0 && standIndex < Main.maxProjectiles)
            {
                Projectile p = Main.projectile[standIndex];
                if (p.active && p.owner == Projectile.owner && p.type == ModContent.ProjectileType<CMOONSTAND_Tier_1>())
                {
                    stand = p;
                }
            }

            // Fallback si el índice directo falla en el cliente
            if (stand == null)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.owner == Projectile.owner && p.type == ModContent.ProjectileType<CMOONSTAND_Tier_1>())
                    {
                        stand = p;
                        Projectile.ai[0] = i;
                        break;
                    }
                }
            }

            // Si el Stand ya no existe, el puño desaparece
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
            }

            InitializeData();

            Vector2 dir = punchAngle.ToRotationVector2();
            int elapsed = TimeLeftMax - Projectile.timeLeft;
            float progress = elapsed * BarrageSystem.PunchSpeed;

            // Posicionamiento basado en variables protegidas
            Projectile.Center = stand.Center + punchOffset + (dir * progress);
            Projectile.rotation = stand.rotation;

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

            string path = punchVariant switch
            {
                1 => "Jojo/Content/Projectiles/CMoon/CMoon_Tier_1/CMOON_BarragePunch_2",
                2 => "Jojo/Content/Projectiles/CMoon/CMoon_Tier_1/CMOON_BarragePunch_3",
                _ => "Jojo/Content/Projectiles/CMoon/CMoon_Tier_1/CMOON_BarragePunch_1",
            };

            Texture2D tex = ModContent.Request<Texture2D>(path).Value;
            Vector2 origin = tex.Size() / 2f;

            // FIX: Ya no buscamos al Player ni al Stand aquí para hacer cálculos físicos.
            // Usamos directamente 'goRight', la variable congelada al nacer el proyectil.
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