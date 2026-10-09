using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Systems;
using System.IO;

namespace Jojo.Content.Projectiles.StarPlatinum.StarPlatinum_Tier_3
{
    public class SP_BarragePunch_Tier_3 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        public bool ignoreTimeStop = true;

        private const int TimeLeftMax = 30;
        private bool offsetRead = false;
        public static float FadeSpeed = 35f;

        // Variables de red seguras para multijugador (Misma lógica Tier 1)
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

            // Almacenamos si mira a la derecha en el momento de nacer
            goRight = rawAi1Int <= 9;
            punchVariant = goRight ? rawAi1Int : rawAi1Int - 10;

            punchOffset = Projectile.velocity;
        }

        public override void AI()
        {
            // Búsqueda a prueba de fallos del Stand en multijugador (Ajustado al Tier 4)
            int standIndex = (int)Projectile.ai[0];
            Projectile stand = null;

            if (standIndex >= 0 && standIndex < Main.maxProjectiles)
            {
                Projectile p = Main.projectile[standIndex];
                if (p.active && p.owner == Projectile.owner && p.type == ModContent.ProjectileType<STARPLATINUMSTAND_Tier_3>())
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
                    if (p.active && p.owner == Projectile.owner && p.type == ModContent.ProjectileType<STARPLATINUMSTAND_Tier_3>())
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

            // Posicionamiento y rotación basados en las variables protegidas del Stand Tier 4
            Projectile.Center = stand.Center + punchOffset + (dir * progress);
            Projectile.rotation = stand.rotation;

            if (progress >= BarrageSystem.PunchDistance)
            {
                Projectile.alpha += (int)FadeSpeed;
                if (Projectile.alpha >= 255) Projectile.Kill();
            }
            else Projectile.alpha = 0;
        }

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            InitializeData();

            string punchNum = punchVariant switch
            {
                1 => "2",
                2 => "3",
                _ => "1"
            };

            Player player = Main.player[Projectile.owner];

            bool ovaSkin = UI.StandSlotSystem.HasOVASkinFor(player);
            bool redSkin = UI.StandSlotSystem.HasStarPlatinumRedSkinFor(player);
            bool greenSkin = UI.StandSlotSystem.HasStarPlatinumGreenSkinFor(player);
            bool blueSkin = UI.StandSlotSystem.HasStarPlatinumBlueSkinFor(player);

            string path;

            if (ovaSkin)
                path = "Jojo/Content/Projectiles/Skins/StarPlatinum/Star_Platinum_OVA/SP_BarragePunch_" + punchNum + "_OVA";
            else if (redSkin)
                path = "Jojo/Content/Projectiles/Skins/StarPlatinum/Star_Platinum_Red/SP_BarragePunch_" + punchNum + "_Red";
            else if (greenSkin)
                path = "Jojo/Content/Projectiles/Skins/StarPlatinum/Star_Platinum_Green/SP_BarragePunch_" + punchNum + "_Green";
            else if (blueSkin)
                path = "Jojo/Content/Projectiles/Skins/StarPlatinum/Star_Platinum_Blue/SP_BarragePunch_" + punchNum + "_Blue";
            else
                path = "Jojo/Content/Projectiles/StarPlatinum/StarPlatinum_Tier_3/SP_BarragePunch_" + punchNum;

            Texture2D tex = ModContent.Request<Texture2D>(path).Value;
            Vector2 origin = tex.Size() / 2f;

            // Flip guiado 100% por red como el Tier 1
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