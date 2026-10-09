using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using System;
using System.IO;
using Jojo.Content.UI;

namespace Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_1
{
    public class Estocada_Tier_1 : ModProjectile
    {
        private float opacidadVisual = 1f;
        private bool inicializado = false;

        private float escalaLargoBase;
        private float escalaAnchoBase;
        private float desplazamientoParaleloOriginal;
        private float curvaturaArqueada;
        private float factorEstiramiento = 0.1f;
        private float vibracionActual = 0f;

        public bool goRight;
        public float punchAngle;
        private bool offsetRead = false;

        const string PathNormal = "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Tier_1/";
        const string PathGolden = "Jojo/Content/Projectiles/Skins/SilverChariot/Golden/";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = ModContent.GetInstance<Clases.ClaseStand>();
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 6;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            InitializeData();
            writer.Write(punchAngle);
            writer.Write(goRight);
            writer.Write(curvaturaArqueada);
            writer.Write(desplazamientoParaleloOriginal);
            writer.Write(escalaLargoBase);
            writer.Write(escalaAnchoBase);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            punchAngle = reader.ReadSingle();
            goRight = reader.ReadBoolean();
            curvaturaArqueada = reader.ReadSingle();
            desplazamientoParaleloOriginal = reader.ReadSingle();
            escalaLargoBase = reader.ReadSingle();
            escalaAnchoBase = reader.ReadSingle();
            inicializado = true;
        }

        private void InitializeData()
        {
            if (inicializado) return;
            inicializado = true;

            int rawAi1Int = (int)Projectile.ai[1];
            float decimalPart = Projectile.ai[1] - rawAi1Int;
            punchAngle = (decimalPart * (MathHelper.TwoPi + 0.01f)) - MathHelper.Pi;
            goRight = rawAi1Int <= 9;

            curvaturaArqueada = Main.rand.NextFloat(-0.02f, 0.02f);
            desplazamientoParaleloOriginal = Main.rand.NextFloat(-26f, 26f);
            escalaLargoBase = Main.rand.NextFloat(1.3f, 2.0f);
            escalaAnchoBase = Main.rand.NextFloat(0.50f, 0.80f);
        }

        public override void AI()
        {
            if (!offsetRead)
            {
                offsetRead = true;
                InitializeData();
            }

            Player player = Main.player[Projectile.owner];
            Projectile duenoActual = null;

            int standIndex = (int)Projectile.ai[0];
            if (standIndex >= 0 && standIndex < Main.maxProjectiles)
            {
                Projectile p = Main.projectile[standIndex];
                if (p.active && p.owner == Projectile.owner && p.type == ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_1>())
                {
                    duenoActual = p;
                }
            }

            if (duenoActual == null)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.owner == Projectile.owner && p.type == ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_1>())
                    {
                        duenoActual = p;
                        Projectile.ai[0] = i;
                        break;
                    }
                }
            }

            if (duenoActual == null)
            {
                Projectile.Kill();
                return;
            }

            Vector2 direccionFrontal = punchAngle.ToRotationVector2();

            float ratioVida = (6f - Projectile.timeLeft) / 6f;
            float factorArcoElipse = (float)Math.Sin(ratioVida * Math.PI);

            if (Projectile.timeLeft >= 4)
                factorEstiramiento = MathHelper.Lerp(0.3f, 1.3f, (6f - Projectile.timeLeft) / 2f);
            else
            {
                factorEstiramiento = MathHelper.Lerp(1.3f, 0.3f, (3f - Projectile.timeLeft) / 3f);
                opacidadVisual -= 0.35f;
                if (opacidadVisual < 0f) opacidadVisual = 0f;
            }

            vibracionActual = Main.rand.NextFloat(-1.5f, 1.5f);

            float distanciaAlFrente = -59f;
            Vector2 puntoFrontal = duenoActual.Center + (direccionFrontal * distanciaAlFrente);
            Vector2 perpendicularFrontal = direccionFrontal.RotatedBy(MathHelper.PiOver2);

            Projectile.Center = puntoFrontal + (perpendicularFrontal * (desplazamientoParaleloOriginal + vibracionActual));

            float anguloDesfaseArco = curvaturaArqueada * factorArcoElipse;
            Projectile.velocity = direccionFrontal.RotatedBy(anguloDesfaseArco) * 42f;

            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Projectile.timeLeft == 6 && Main.rand.NextBool(4))
            {
                SoundStyle estiloCorte = SoundID.Item1;
                estiloCorte.Volume = 0.07f;
                estiloCorte.Pitch = Main.rand.NextFloat(0.7f, 1.0f);
                SoundEngine.PlaySound(estiloCorte, Projectile.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            bool goldenSkin = StandSlotSystem.HasGoldenSkinFor(owner);
            string pathBase = goldenSkin ? PathGolden : PathNormal;

            Texture2D texture = ModContent.Request<Texture2D>(goldenSkin ? pathBase + "Estocada_Golden" : PathNormal + "Estocada_Tier_1").Value;

            Vector2 origin = new Vector2(0f, texture.Height / 2f);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Color colorDibujoFinal = lightColor * opacidadVisual;
            Vector2 escalaFinal = new Vector2(escalaLargoBase * factorEstiramiento, escalaAnchoBase);

            SpriteEffects efecto = SpriteEffects.None;
            if (!goRight)
            {
                efecto = SpriteEffects.FlipVertically;
            }

            Main.EntitySpriteDraw(texture, drawPos, null, colorDibujoFinal, Projectile.rotation, origin, escalaFinal, efecto, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorDibujoFinal, Projectile.rotation, origin, escalaFinal * new Vector2(0.85f, 0.4f), efecto, 0);

            return false;
        }
    }
}