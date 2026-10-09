using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Clases;
using Jojo.Content.Buffs.MagiciansRed_Buffs;

namespace Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_1
{
    public class FuegoLanzallamas_Tier_1 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Flames;

        // --- VARIABLES CONFIGURABLES ---
        private int maxLifetime = 25;
        public int velocidadDeGolpeEnemigo = 8; // Ticks entre golpes

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 7;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = maxLifetime;

            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = velocidadDeGolpeEnemigo;
        }

        private void GetSkinVisuals(Player p, out int mainDust, out Color color1, out Color color2, out Color color3, out Color color4)
        {
            if (UI.StandSlotSystem.HasMagiciansRedPinkSkinFor(p))
            {
                mainDust = DustID.PinkTorch;
                color1 = new Color(255, 180, 255, 200);
                color2 = new Color(255, 80, 200, 180);
                color3 = new Color(180, 20, 150, 140);
                color4 = new Color(60, 10, 80, 0);
            }
            else if (UI.StandSlotSystem.HasMagiciansRedGreenSkinFor(p))
            {
                mainDust = DustID.CursedTorch;
                color1 = new Color(180, 255, 150, 200);
                color2 = new Color(80, 255, 50, 180);
                color3 = new Color(20, 180, 40, 140);
                color4 = new Color(15, 60, 20, 0);
            }
            else if (UI.StandSlotSystem.HasMagiciansRedBlueSkinFor(p))
            {
                mainDust = DustID.IceTorch;
                color1 = new Color(150, 200, 255, 200);
                color2 = new Color(50, 150, 255, 180);
                color3 = new Color(20, 60, 240, 140);
                color4 = new Color(10, 20, 80, 0);
            }
            else
            {
                mainDust = DustID.Torch;
                color1 = new Color(255, 255, 150, 200);
                color2 = new Color(255, 170, 20, 180);
                color3 = new Color(240, 60, 15, 140);
                color4 = new Color(80, 25, 20, 0);
            }
        }

        public override void AI()
        {
            if (Projectile.ai[0] == 0f)
            {
                Projectile.ai[0] = 1f;

                if (Projectile.ai[1] > 0)
                {
                    maxLifetime = (int)Projectile.ai[1];
                    Projectile.timeLeft = maxLifetime;
                }

                Projectile.scale = 0.5f;
                Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            }

            // Animación de frames
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }

            Projectile.rotation += 0.15f * (Projectile.identity % 2 == 0 ? 1 : -1);

            if (Projectile.scale < 2.2f)
            {
                Projectile.scale += 0.05f;
            }

            // --- CORRECCIÓN DE HITBOX ---
            Vector2 currentCenter = Projectile.Center;
            int currentSize = (int)(42 * Projectile.scale);
            Projectile.width = currentSize;
            Projectile.height = currentSize;
            Projectile.Center = currentCenter;

            Projectile.velocity *= 0.95f;

            // --- PARTICULAS DE FUEGO EXPULSADAS HACIA AFUERA ---
            Player owner = Main.player[Projectile.owner];
            GetSkinVisuals(owner, out int mainDust, out _, out _, out _, out _);

            // Generamos de 1 a 2 partículas por tick
            int cantidadPolvo = Main.rand.Next(1, 3);
            for (int i = 0; i < cantidadPolvo; i++)
            {
                // Obtenemos una dirección radial aleatoria (borde de un círculo)
                Vector2 direccionHaciaAfuera = Main.rand.NextVector2CircularEdge(1f, 1f);

                // Posicionamos la partícula un poco alejada del centro, dentro de la llama
                float distanciaDesdeCentro = Main.rand.NextFloat(0f, Projectile.width * 0.4f);
                Vector2 posPolvo = Projectile.Center + direccionHaciaAfuera * distanciaDesdeCentro;

                // Le damos velocidad en esa misma dirección hacia afuera para que se expulse
                float velocidadExpulsion = Main.rand.NextFloat(1.5f, 4f);
                Vector2 velPolvo = direccionHaciaAfuera * velocidadExpulsion;

                // Sumamos un poco de la velocidad del proyectil para que siga la corriente de la llama
                velPolvo += Projectile.velocity * 0.3f;

                Dust fuego = Dust.NewDustPerfect(
                    posPolvo,
                    mainDust,
                    velPolvo,
                    100,
                    default,
                    Projectile.scale * 1.2f
                );

                fuego.noGravity = true;
            }

            if (Projectile.wet)
            {
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle sourceRectangle = new Rectangle(0, frameHeight * Projectile.frame, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width / 2f, frameHeight / 2f);

            float progress = 1f - ((float)Projectile.timeLeft / maxLifetime);

            Player owner = Main.player[Projectile.owner];
            GetSkinVisuals(owner, out _, out Color c1, out Color c2, out Color c3, out Color c4);

            Color drawColor;

            if (progress < 0.35f)
            {
                float t = progress / 0.35f;
                drawColor = Color.Lerp(c1, c2, t);
            }
            else if (progress < 0.75f)
            {
                float t = (progress - 0.35f) / 0.40f;
                drawColor = Color.Lerp(c2, c3, t);
            }
            else
            {
                float t = (progress - 0.75f) / 0.25f;
                drawColor = Color.Lerp(c3, c4, t);
            }

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                sourceRectangle,
                drawColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity *= 0.2f;
            if (Projectile.timeLeft > 10)
            {
                Projectile.timeLeft = 10;
            }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<MagiciansFire_Tier_1>(), 180);
        }
    }
}