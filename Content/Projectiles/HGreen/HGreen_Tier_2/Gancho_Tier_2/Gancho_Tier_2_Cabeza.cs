using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.HGreen.HGreen_Tier_2.Gancho_Tier_2
{
    public class Gancho_Tier_2_Cabeza : ModProjectile
    {
        // ==============================================================
        // --- CONFIGURACIÓN FÁCIL DEL GANCHO --- (Modifica esto a tu gusto)
        // ==============================================================
        public const int NumeroDeGanchos = 2;        // Cuántos ganchos/tentáculos puedes tener activos a la vez
        public const float VelocidadDisparo = 16f;   // Velocidad al salir disparado hacia tu ratón
        public const float DistanciaMaxima = 350f;   // Distancia máxima a la que llega el gancho
        public const float VelocidadRegreso = 20f;   // Velocidad a la que vuelve si fallas y no toca bloque
        public const float VelocidadAtraccion = 15f; // Velocidad a la que te arrastra hacia la pared

        // --- CONFIGURACIÓN DE LAS PARTÍCULAS DEL LÁTIGO ---
        public const int IntervaloParticulas = 4;    // Cada cuántos ticks aparece 1 partícula. Más alto = MENOS partículas
        public const float TamanoParticula = 0.4f;   // Tamaño de cada partícula
        public const int AlphaInicialParticula = 180; // 0-255. Más alto = se desvanece MÁS rápido (menos "vida")
        // ==============================================================

        // Mismo color/brillo verde que usa el Stand, para que sea consistente visualmente
        static readonly Color GanchoGlowColor = new Color(60, 255, 90);

        // Cacheamos la textura de la cadena una sola vez, para no pedirla cada tick
        static Texture2D chainTextureCache;

        int dustTimer;

        public override void SetStaticDefaults()
        {
            Main.projHook[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.netImportant = true;
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.aiStyle = 7;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft *= 10;
        }

        // Usa la configuración para saber si puedes disparar otro gancho
        public override bool? CanUseGrapple(Player player)
        {
            int hooksOut = 0;
            for (int l = 0; l < Main.maxProjectiles; l++)
            {
                if (Main.projectile[l].active && Main.projectile[l].owner == Main.myPlayer && Main.projectile[l].type == Projectile.type)
                {
                    hooksOut++;
                }
            }
            return hooksOut < NumeroDeGanchos;
        }

        // Aplica tu configuración a las físicas de Terraria
        public override float GrappleRange() => DistanciaMaxima;
        public override void NumGrappleHooks(Player player, ref int numHooks) => numHooks = NumeroDeGanchos;
        public override void GrappleRetreatSpeed(Player player, ref float speed) => speed = VelocidadRegreso;
        public override void GrapplePullSpeed(Player player, ref float speed) => speed = VelocidadAtraccion;

        // --- Brillo (en cada eslabón) + partículas verdes (SOLO 1 cada varios ticks, sin velocidad) ---
        // AI() se ejecuta localmente en TODOS los clientes (igual que el Stand), así que esto se ve
        // para todos los jugadores sin sincronizar nada por red. Se suma a la física vanilla del
        // aiStyle = 7, no la reemplaza.
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.25f, 1f, 0.35f);

            if (Main.netMode == NetmodeID.Server) return; // Todo lo demás es puramente visual

            Player player = Main.player[Projectile.owner];
            Vector2 playerCenter = player.MountedCenter;
            Vector2 center = Projectile.Center;
            Vector2 directionToPlayer = playerCenter - center;
            float distance = directionToPlayer.Length();

            if (distance <= 0f) return;

            directionToPlayer.Normalize();

            if (chainTextureCache == null)
            {
                chainTextureCache = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/HGreen/HGreen_Tier_2/Gancho_Tier_2/Gancho_Tier_2_Segmento").Value;
            }

            // --- Luz en cada eslabón: esto NO se acumula visualmente ni deja estela, es barato dejarlo así ---
            float chainStep = chainTextureCache.Height;
            float length = distance;
            Vector2 currentPos = center;

            while (length > 0f)
            {
                if (length < 16f) break;
                Lighting.AddLight(currentPos, 0.25f, 1f, 0.35f);
                currentPos += directionToPlayer * chainStep;
                length -= chainStep;
            }

            // --- Partículas: como mucho 1 cada "IntervaloParticulas" ticks, en un punto aleatorio ---
            // del látigo (desde la punta hasta el jugador). Al no depender del número de segmentos,
            // no importa si el látigo es largo o corto: la cantidad generada es siempre la misma.
            dustTimer++;
            if (dustTimer >= IntervaloParticulas)
            {
                dustTimer = 0;

                float randomDistance = Main.rand.NextFloat(0f, distance);
                Vector2 dustPos = center + directionToPlayer * randomDistance;

                Dust dust = Dust.NewDustDirect(dustPos - new Vector2(3f, 3f), 6, 6, DustID.GreenFairy, 0f, 0f, AlphaInicialParticula, default, TamanoParticula);
                dust.noGravity = true;
                dust.velocity = Vector2.Zero; // SIN velocidad -> no se aleja, no deja estela, se queda pegada al látigo
                dust.color = GanchoGlowColor;
                dust.fadeIn = 0f;
            }
        }

        public override bool PreDrawExtras()
        {
            Texture2D chainTexture = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/HGreen/HGreen_Tier_2/Gancho_Tier_2/Gancho_Tier_2_Segmento").Value;

            Player player = Main.player[Projectile.owner];
            Vector2 playerCenter = player.MountedCenter;
            Vector2 center = Projectile.Center;
            Vector2 directionToPlayer = playerCenter - center;
            float distance = directionToPlayer.Length();
            float rotation = directionToPlayer.ToRotation() - MathHelper.PiOver2;

            if (distance > 0f)
            {
                directionToPlayer.Normalize();
                float chainStep = chainTexture.Height;
                float length = distance;
                Vector2 currentPos = center;

                while (length > 0f)
                {
                    if (length < 16f) break;

                    Main.EntitySpriteDraw(
                        chainTexture,
                        currentPos - Main.screenPosition,
                        null,
                        Lighting.GetColor((int)(currentPos.X / 16f), (int)(currentPos.Y / 16f)),
                        rotation,
                        chainTexture.Size() * 0.5f,
                        1f,
                        SpriteEffects.None,
                        0f
                    );

                    currentPos += directionToPlayer * chainStep;
                    length -= chainStep;
                }
            }

            return false; // Anula la cadena vanilla
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return true;
        }
    }
}