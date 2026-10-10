using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Systems;
using System.IO;

namespace Jojo.Content.Projectiles.TheWorld.TheWorld_Tier_4
{
    public class TW_BarragePunch_Tier_4 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        public bool ignoreTimeStop = true;

        private const int TimeLeftMax = 30;
        private bool offsetRead = false;

        public static float FadeSpeed = 35f;

        // ---- Variables de red seguras para multijugador (mismo patrón que RIKA/WhiteSnake) ----
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
        // y la corrupción del ángulo por compresión de floats.
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
            // segundo jugador entra tarde a la partida.
            // TODO: confirma que la clase del Stand se llama THEWORLDSTAND_Tier_4
            // (revisa el .cs del Stand dentro de la carpeta TheWorld_Tier_4).
            int standIndex = (int)Projectile.ai[0];
            Projectile stand = null;

            if (standIndex >= 0 && standIndex < Main.maxProjectiles)
            {
                Projectile p = Main.projectile[standIndex];
                if (p.active && p.owner == Projectile.owner && p.type == ModContent.ProjectileType<THEWORLDSTAND_Tier_4>())
                {
                    stand = p;
                }
            }

            if (stand == null)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.owner == Projectile.owner && p.type == ModContent.ProjectileType<THEWORLDSTAND_Tier_4>())
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
            }

            InitializeData();

            // --- DECODIFICACIÓN 360° (se conserva igual, ahora leída de forma segura) ---
            Vector2 dir = punchAngle.ToRotationVector2();

            int elapsed = TimeLeftMax - Projectile.timeLeft;
            float progress = elapsed * BarrageSystem.PunchSpeed;

            Projectile.Center = stand.Center + punchOffset + dir * progress;
            Projectile.rotation = stand.rotation;

            if (progress >= BarrageSystem.PunchDistance)
            {
                Projectile.alpha += (int)FadeSpeed;
                if (Projectile.alpha >= 255) Projectile.Kill();
            }
            else Projectile.alpha = 0;
        }

        public override bool? CanDamage() => false;

        static (string folder, string suffix) GetColorSkin(Player p)
        {
            if (UI.StandSlotSystem.HasTheWorldBlueSkinFor(p))
                return ("Jojo/Content/Projectiles/Skins/TheWorld/TheWorld_Blue/", "_Blue");

            if (UI.StandSlotSystem.HasTheWorldRedSkinFor(p))
                return ("Jojo/Content/Projectiles/Skins/TheWorld/TheWorld_Red/", "_Red");

            if (UI.StandSlotSystem.HasTheWorldGreenSkinFor(p))
                return ("Jojo/Content/Projectiles/Skins/TheWorld/TheWorld_Green/", "_Green");

            return (null, null);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            InitializeData();

            Player player = Main.player[Projectile.owner];
            bool ovaSkin = UI.StandSlotSystem.HasOVASkinFor(player);
            var (colorFolder, colorSuffix) = GetColorSkin(player);

            string basePath;
            string suffix;

            if (ovaSkin)
            {
                basePath = "Jojo/Content/Projectiles/Skins/TheWorld/The_World_OVA/";
                suffix = "";
            }
            else if (colorFolder != null)
            {
                basePath = colorFolder;
                suffix = colorSuffix;
            }
            else
            {
                basePath = "Jojo/Content/Projectiles/TheWorld/TheWorld_Tier_4/";
                suffix = "";
            }

            // Antes se leía "(int)Projectile.ai[1]" directo (podía llegar corrompido
            // por la compresión de red). Ahora se usa punchVariant, ya sincronizado.
            string path = punchVariant switch
            {
                1 => basePath + "TW_BarragePunch_2" + suffix,
                2 => basePath + "TW_BarragePunch_3" + suffix,
                _ => basePath + "TW_BarragePunch_1" + suffix,
            };

            Texture2D tex = ModContent.Request<Texture2D>(path).Value;
            Vector2 origin = tex.Size() / 2f;

            // FIX: antes se comparaba la posición real del stand (stand.Center.X < player.Center.X),
            // que se desincroniza cuando el stand se queda atrás por inercia al correr (el
            // FollowPlayer con lerp tarda en alcanzarte). Eso hacía que el puñetazo pareciera
            // "girar" hacia el lado contrario justo cuando el stand quedaba rezagado, sobre todo
            // con el ratón cerca del borde de pantalla. Ahora usamos "goRight", la misma señal
            // congelada en el momento del golpe (igual que en WhiteSnake), que no depende de la
            // posición en vivo del stand.
            bool flip = !goRight;

            Main.EntitySpriteDraw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor * (1f - Projectile.alpha / 255f),
                Projectile.rotation,
                origin,
                1f,
                flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                0f);

            return false;
        }
    }
}