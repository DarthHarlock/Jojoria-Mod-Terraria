using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Systems;
using Jojo.Content.UI;
using System.IO;

namespace Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_3
{
    public class KQ_BarragePunch_Tier_3 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        public bool ignoreTimeStop = true;

        private const int TimeLeftMax = 30;
        private bool offsetRead = false;
        public static float FadeSpeed = 35f;

        // --- ARQUITECTURA STAR PLATINUM: Variables de red seguras para multijugador ---
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

        // Sincronización del Host hacia los Clientes
        public override void SendExtraAI(BinaryWriter writer)
        {
            InitializeData();
            writer.Write(punchOffset.X);
            writer.Write(punchOffset.Y);
            writer.Write(punchAngle);
            writer.Write(punchVariant);
            writer.Write(goRight);
        }

        // Recepción en el Cliente
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            punchOffset.X = reader.ReadSingle();
            punchOffset.Y = reader.ReadSingle();
            punchAngle = reader.ReadSingle();
            punchVariant = reader.ReadInt32();
            goRight = reader.ReadBoolean();
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

            // Almacenamos si mira a la derecha y su variante visual
            goRight = rawAi1Int <= 9;
            punchVariant = goRight ? rawAi1Int : rawAi1Int - 10;

            punchOffset = Projectile.velocity;
        }

        public override void AI()
        {
            // Búsqueda a prueba de fallos del Stand en multijugador
            int standIndex = (int)Projectile.ai[0];
            Projectile stand = null;

            if (standIndex >= 0 && standIndex < Main.maxProjectiles)
            {
                Projectile p = Main.projectile[standIndex];
                if (p.active && p.owner == Projectile.owner && p.type == ModContent.ProjectileType<KILLERQUEENSTAND_Tier_3>())
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
                    if (p.active && p.owner == Projectile.owner && p.type == ModContent.ProjectileType<KILLERQUEENSTAND_Tier_3>())
                    {
                        stand = p;
                        Projectile.ai[0] = i;
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

            Vector2 dir = punchAngle.ToRotationVector2();
            int elapsed = TimeLeftMax - Projectile.timeLeft;
            float progress = elapsed * BarrageSystem.PunchSpeed;

            // Posicionamiento basado en variables protegidas y sincronizadas
            Projectile.Center = stand.Center + punchOffset + (dir * progress);
            Projectile.rotation = stand.rotation;

            if (progress >= BarrageSystem.PunchDistance)
            {
                Projectile.alpha += (int)FadeSpeed;
                if (Projectile.alpha >= 255) Projectile.Kill();
            }
            else
            {
                Projectile.alpha = 0;
            }
        }

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            InitializeData();

            Player player = Main.player[Projectile.owner];

            bool megumin = StandSlotSystem.HasMeguminSkinFor(player);
            bool isRed = StandSlotSystem.HasKillerQueenRedSkinFor(player);
            bool isBlue = StandSlotSystem.HasKillerQueenBlueSkinFor(player);
            bool isGreen = StandSlotSystem.HasKillerQueenGreenSkinFor(player);

            string path;

            if (megumin)
            {
                path = punchVariant switch
                {
                    1 => "Jojo/Content/Projectiles/Skins/KillerQueen/Megumin/KQ_BarragePunch_2_Megumin",
                    2 => "Jojo/Content/Projectiles/Skins/KillerQueen/Megumin/KQ_BarragePunch_3_Megumin",
                    _ => "Jojo/Content/Projectiles/Skins/KillerQueen/Megumin/KQ_BarragePunch_1_Megumin",
                };
            }
            else if (isRed)
            {
                path = punchVariant switch
                {
                    1 => "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Red/KQ_BarragePunch_2_Red",
                    2 => "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Red/KQ_BarragePunch_3_Red",
                    _ => "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Red/KQ_BarragePunch_1_Red",
                };
            }
            else if (isBlue)
            {
                path = punchVariant switch
                {
                    1 => "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Blue/KQ_BarragePunch_2_Blue",
                    2 => "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Blue/KQ_BarragePunch_3_Blue",
                    _ => "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Blue/KQ_BarragePunch_1_Blue",
                };
            }
            else if (isGreen)
            {
                path = punchVariant switch
                {
                    1 => "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Green/KQ_BarragePunch_2_Green",
                    2 => "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Green/KQ_BarragePunch_3_Green",
                    _ => "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Green/KQ_BarragePunch_1_Green",
                };
            }
            else
            {
                path = punchVariant switch
                {
                    1 => "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_3/KQ_BarragePunch_2",
                    2 => "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_3/KQ_BarragePunch_3",
                    _ => "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_3/KQ_BarragePunch_1",
                };
            }

            Texture2D tex = ModContent.Request<Texture2D>(path).Value;
            Vector2 origin = tex.Size() / 2f;

            // FIX: Ya no buscamos al Player ni la posición del Stand en vivo.
            // Usamos "goRight", la misma señal congelada en el momento del golpe
            // (igual que en Tier 1 / WhiteSnake), que no depende del lerp del Stand.
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
                0f
            );

            return false;
        }
    }
}