using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.HGreen.HGreen_Tier_2.CirculoEsmeralda_Tier_2
{
    // Proyectil exclusivo de la habilidad "Circulo Esmeralda" (Skill G).
    // Es visualmente identico a Esmeralda_4 (mismo spritesheet de 4 frames,
    // misma animacion, mismas particulas verdes), pero en vez de morir al
    // chocar con un tile, rebota hasta "maxBounces" veces antes de desaparecer.
    public class Esmeralda_4_Tier_2 : ModProjectile
    {
        static readonly Color NeonGreen = new Color(60, 255, 90);

        // ================= CONFIGURACION DE LA HABILIDAD =================
        // Daño base de CADA esmeralda del circulo, ANTES de aplicar los
        // bonus/multiplicadores de daño que tenga el jugador en la clase
        // Stand (objetos, buffs, arbol de talentos, etc). Cambia este
        // numero para subir o bajar el daño de la habilidad.
        public int baseDamage = 30;

        // Cuantas veces puede rebotar cada esmeralda contra un tile antes
        // de morir. 0 = muere en el primer impacto (como una Esmeralda_4
        // normal). Cambia este numero para mas o menos rebotes.
        public int maxBounces = 5;
        // ====================================================================

        bool init;

        // Contador de rebotes ya usados. Se guarda en ai[0] para que se
        // sincronice automaticamente en multijugador (sin SendExtraAI).
        int Bounces
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.hostile = false;

            // El daño de este proyectil es de clase Stand: escala con
            // cualquier bonus de daño de clase Stand que tenga el jugador.
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.penetrate = 1;
            // Aumentado a 600 ticks (10 segundos) para que no expiren por tiempo 
            // y puedan completar todos sus rebotes tranquilamente.
            Projectile.timeLeft = 600;
            Projectile.alpha = 0;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 0;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            // Se calcula una sola vez, en el primer tick tras el spawn.
            // Tomamos el daño base configurado arriba y le aplicamos los
            // multiplicadores de daño de clase Stand del jugador dueño
            // (esto es lo que hace que escale con la clase Stand: objetos,
            // buffs, arbol de talentos, etc). El valor que llega desde
            // Projectile.NewProjectile() al spawnear se descarta y se
            // reemplaza por este calculo.
            if (!init)
            {
                init = true;

                Player owner = Main.player[Projectile.owner];
                Projectile.damage = (int)owner.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= 4)
                {
                    Projectile.frame = 0;
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.15f, 0.8f, 0.27f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.GreenFairy,
                    0f, 0f,
                    100,
                    default,
                    0.65f
                );

                dust.noGravity = true;
                dust.color = NeonGreen;
                dust.velocity = Projectile.velocity * -0.05f;
                dust.fadeIn = 0.4f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Igual que en Esmeralda_4: al tener penetrate = 1, Terraria mata
            // el proyectil automaticamente y llama a OnKill() en red.
        }

        // AQUI ESTA LA DIFERENCIA CLAVE CON Esmeralda_4:
        // en vez de morir al chocar con un tile, invierte su velocidad
        // (rebota) hasta "maxBounces" veces. Al agotar los rebotes, muere
        // igual que la Esmeralda normal.
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Bounces < maxBounces)
            {
                Bounces++;

                if (Projectile.velocity.X != oldVelocity.X)
                    Projectile.velocity.X = -oldVelocity.X;
                if (Projectile.velocity.Y != oldVelocity.Y)
                    Projectile.velocity.Y = -oldVelocity.Y;

                Projectile.netUpdate = true;
                return false; // No muere, sigue rebotando
            }

            Projectile.Kill(); // Se agotaron los rebotes -> activa OnKill() en red
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SpawnImpactDust();
        }

        void SpawnImpactDust()
        {
            if (Main.netMode == NetmodeID.Server) return;

            for (int i = 0; i < 10; i++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GreenFairy, dir * Main.rand.NextFloat(1.5f, 3.5f), 100, default, 1.4f);
                dust.noGravity = true;
                dust.color = NeonGreen;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;

            int frameWidth = texture.Width / 4;
            int frameHeight = texture.Height;

            Rectangle sourceRectangle = new Rectangle(Projectile.frame * frameWidth, 0, frameWidth, frameHeight);
            Vector2 origin = new Vector2(frameWidth * 0.5f, frameHeight * 0.5f);

            SpriteEffects effects = Projectile.velocity.X < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;

            Color glowColor = Color.White * 0.75f;

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                sourceRectangle,
                glowColor * ((255 - Projectile.alpha) / 255f),
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0
            );

            return false;
        }
    }
}