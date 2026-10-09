using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using System;
using Jojo.Content.UI;

namespace Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_4
{
    public class Estocada_Tier_4 : ModProjectile
    {
        private float opacidadVisual = 1f;
        private bool inicializado = false;

        private float escalaLargoBase;
        private float escalaAnchoBase;
        private float desplazamientoParaleloOriginal;
        private float curvaturaArqueada;

        private float factorEstiramiento = 0.1f;
        private float vibracionActual = 0f;

        // === FIX (portado de Tier 1) ===
        // Dirección de ataque congelada en el instante del disparo (solo aplica a estocadas
        // del stand principal, no a las de los clones).
        private float anguloFrontalFijo;
        private bool tieneAnguloFijo;

        // === RUTA BASE DE TEXTURAS SEGÚN SKIN ===
        const string PathNormal = "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Tier_4/";
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

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile duenoActual = null;

            int idClonDueno = (int)Projectile.ai[1];

            if (idClonDueno > 0)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile pClon = Main.projectile[i];
                    if (pClon.active && pClon.owner == Projectile.owner && pClon.type == ModContent.ProjectileType<ChariotClon_Tier_4>() && pClon.identity == idClonDueno)
                    {
                        duenoActual = pClon;
                        break;
                    }
                }
            }

            if (duenoActual == null)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active && proj.owner == Projectile.owner && proj.type == ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_4>())
                    {
                        duenoActual = proj;
                        break;
                    }
                }
            }

            if (duenoActual == null)
            {
                Projectile.Kill();
                return;
            }

            if (!inicializado)
            {
                inicializado = true;
                curvaturaArqueada = Main.rand.NextFloat(-0.02f, 0.02f);
                desplazamientoParaleloOriginal = Main.rand.NextFloat(-26f, 26f);
                escalaLargoBase = Main.rand.NextFloat(1.3f, 2.0f);
                escalaAnchoBase = Main.rand.NextFloat(0.50f, 0.80f);

                if (duenoActual.type == ModContent.ProjectileType<ChariotClon_Tier_4>())
                {
                    float vStand = Projectile.ai[0];

                    if (vStand >= 150f) Projectile.damage = (int)(Projectile.damage * 0.05f);
                    else if (vStand >= 100f) Projectile.damage = (int)(Projectile.damage * 0.03f);
                    else if (vStand >= 50f) Projectile.damage = (int)(Projectile.damage * 0.02f);
                    else Projectile.damage = (int)(Projectile.damage * 0.01f);

                    if (Projectile.damage < 1) Projectile.damage = 1;
                }
                else
                {
                    // === FIX (portado de Tier 1) ===
                    // Para las estocadas del stand principal, ai[0] no se usaba para nada
                    // (solo aplica a los clones), así que aquí decodificamos el ángulo de
                    // ataque que el stand ya calculó y congeló en el momento del disparo.
                    // Esto elimina la necesidad de "adivinar" la dirección cada frame mirando
                    // velocidad/posición del jugador, que era la causa del bug al correr con
                    // el mouse fijo a un lado.
                    float anguloNormalizado = Projectile.ai[0];
                    anguloFrontalFijo = (anguloNormalizado * (MathHelper.TwoPi + 0.01f)) - MathHelper.Pi;
                    tieneAnguloFijo = true;
                }
            }

            Vector2 direccionFrontal;

            if (tieneAnguloFijo)
            {
                // Dirección ya congelada al momento del disparo (fix portado de Tier 1)
                direccionFrontal = anguloFrontalFijo.ToRotationVector2();
            }
            else
            {
                // Estocadas disparadas por un ChariotClon: comportamiento original sin tocar
                float rotacionDueno = duenoActual.rotation;
                Vector2 direccionBase = (duenoActual.spriteDirection == -1) ? -Vector2.UnitX : Vector2.UnitX;
                direccionFrontal = direccionBase.RotatedBy(rotacionDueno);
            }

            direccionFrontal.Normalize();

            float ratioVida = (6f - Projectile.timeLeft) / 6f;
            float factorArcoElipse = (float)Math.Sin(ratioVida * Math.PI);

            if (Projectile.timeLeft >= 4)
            {
                factorEstiramiento = MathHelper.Lerp(0.3f, 1.3f, (6f - Projectile.timeLeft) / 2f);
            }
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

            // Unificado: Textura base y skin Golden
            Texture2D texture = ModContent.Request<Texture2D>(goldenSkin ? pathBase + "Estocada_Golden" : PathNormal + "Estocada_Tier_4").Value;
            Vector2 origin = new Vector2(0f, texture.Height / 2f);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Color colorDibujoFinal = lightColor * opacidadVisual;

            Vector2 escalaFinal = new Vector2(escalaLargoBase * factorEstiramiento, escalaAnchoBase);

            Main.EntitySpriteDraw(texture, drawPos, null, colorDibujoFinal, Projectile.rotation, origin, escalaFinal, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorDibujoFinal, Projectile.rotation, origin, escalaFinal * new Vector2(0.85f, 0.4f), SpriteEffects.None, 0);

            return false;
        }
    }
}