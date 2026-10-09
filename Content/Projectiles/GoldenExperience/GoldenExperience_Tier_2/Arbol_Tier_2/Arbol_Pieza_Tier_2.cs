using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_2.Arbol_Tier_2
{
    public enum ArbolPiezaTipo_Tier_2
    {
        Base = 0,
        Segmento1 = 1,
        Segmento2 = 2,
        Segmento3 = 3,
        Cabeza = 4
    }

    public class Arbol_Pieza_Tier_2 : ModProjectile
    {
        public override string Texture => ObtenerRutaTextura(ArbolPiezaTipo_Tier_2.Base);

        const float DañoBase = 35f;
        const float Empuje = 6f;
        const int VidaPieza = 99999;
        const int CooldownGolpe = 20;
        const int TiempoCrecimiento = 12;
        const int TiempoMuerte = 75;

        static readonly Color ColorPodrido = new Color(95, 120, 60);

        ArbolPiezaTipo_Tier_2 Tipo => (ArbolPiezaTipo_Tier_2)Projectile.ai[0];
        int MiId => (int)Projectile.ai[1];

        // =========================================================================
        // FIX BUG MULTIJUGADOR (árbol flotando / tronco base clavado para el
        // jugador 2):
        // Antes, la posición de cada pieza la fijaba EXCLUSIVAMENTE
        // Arbol_Torre_Controller.SincronizarPosicionPiezas(), que solo corría en la
        // máquina del dueño, y esa actualización dependía de que cada pieza
        // recibiera, tick a tick, un paquete de red disparado desde el AI() de OTRO
        // proyectil (el tronco). Eso es frágil: en clientes no-dueños las piezas
        // solo llegaban a recibir su posición INICIAL (la del instante exacto en
        // que se crearon, con el árbol todavía cayendo), y nunca se corregían con
        // fiabilidad después.
        //
        // AHORA: cada pieza se autoposiciona en su propio AI(), que SÍ corre en
        // TODOS los clientes (dueño y no dueño) de forma nativa. Solo necesita:
        //   1) OffsetY: su altura fija dentro del árbol (sincronizada UNA vez al
        //      crearse, vía ExtraAI, por Arbol_Torre_Controller.ColocarPieza).
        //   2) MiId (ai[1]): a qué árbol pertenece (ya viaja por red de forma
        //      nativa, es un campo ai[] estándar).
        //   3) La posición del tronco (Projectile.Bottom del controller), que se
        //      sincroniza de forma nativa y fiable por ser la posición real del
        //      proyectil.
        // Con esto, la posición se recalcula de forma determinista en cada
        // cliente, sin depender de que un paquete de red llegue a tiempo cada
        // tick.
        // =========================================================================
        public float OffsetY;

        int truncoCacheIndex = -1;

        bool dustEmitido;
        bool dying;
        int muerteTimer;

        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 44;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = VidaPieza;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = CooldownGolpe;
            Projectile.knockBack = Empuje;
            Projectile.scale = 0f;
        }

        // =========================================================================
        // "dying" no viajaba por red antes de este fix (arreglo previo, se
        // mantiene). Ahora también viaja OffsetY, pero solo hace falta transmitirlo
        // una vez (es fijo durante toda la vida de la pieza), así que basta con que
        // vaya en el paquete de creación inicial.
        // =========================================================================
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(dying);
            writer.Write(OffsetY);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            bool dyingRecibido = reader.ReadBoolean();
            if (dyingRecibido && !dying)
            {
                dying = true;
                muerteTimer = 0;
                Projectile.friendly = false;
            }

            OffsetY = reader.ReadSingle();
        }

        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            hitbox.Inflate(12, 12);
        }

        public void StartDying()
        {
            if (dying) return;
            dying = true;
            muerteTimer = 0;
            Projectile.friendly = false;

            if (Main.netMode != NetmodeID.SinglePlayer)
                Projectile.netUpdate = true;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                dustEmitido = false;
                dying = false;
                muerteTimer = 0;

                Vector2 centerGuardado = Projectile.Center;

                Texture2D tex = ModContent.Request<Texture2D>(ObtenerRutaTextura(Tipo), AssetRequestMode.ImmediateLoad).Value;
                Projectile.width = tex.Width;
                Projectile.height = tex.Height;

                Projectile.Center = centerGuardado;
            }

            Main.instance.DrawCacheProjsOverPlayers.Add(Projectile.whoAmI);

            // --- Autoposicionamiento: corre en TODOS los clientes ---
            SeguirTronco();

            if (dying)
            {
                muerteTimer++;
                Projectile.alpha = (int)MathHelper.Clamp(255f * (muerteTimer / (float)TiempoMuerte), 0, 255);

                if (muerteTimer >= TiempoMuerte)
                    Projectile.Kill();

                return;
            }

            Player p = Main.player[Projectile.owner];

            if (Projectile.scale < 1f)
            {
                if (!dustEmitido)
                {
                    dustEmitido = true;
                    EmitirParticulasNacimiento();
                }

                Projectile.scale += 1f / TiempoCrecimiento;
                if (Projectile.scale > 1f) Projectile.scale = 1f;
            }

            Projectile.damage = p.active && !p.dead
                ? (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(DañoBase)
                : (int)DañoBase;

            Projectile.friendly = Projectile.scale >= 1f;
        }

        void SeguirTronco()
        {
            int tipoTronco = ModContent.ProjectileType<Arbol_Torre_Controller_Tier_2>();

            // Primero intenta reutilizar el índice cacheado (evita recorrer todo
            // el array de proyectiles cada tick en el caso normal).
            if (truncoCacheIndex >= 0 && truncoCacheIndex < Main.maxProjectiles)
            {
                Projectile cacheado = Main.projectile[truncoCacheIndex];
                if (cacheado.active
                    && cacheado.type == tipoTronco
                    && cacheado.owner == Projectile.owner
                    && cacheado.ModProjectile is Arbol_Torre_Controller_Tier_2 ctrlCacheado
                    && ctrlCacheado.MiId == MiId)
                {
                    Projectile.Center = cacheado.Bottom - new Vector2(0, OffsetY);
                    return;
                }

                truncoCacheIndex = -1;
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (!pr.active || pr.type != tipoTronco || pr.owner != Projectile.owner) continue;

                if (pr.ModProjectile is Arbol_Torre_Controller_Tier_2 ctrl && ctrl.MiId == MiId)
                {
                    truncoCacheIndex = i;
                    Projectile.Center = pr.Bottom - new Vector2(0, OffsetY);
                    return;
                }
            }

            // Si no se encuentra el tronco (por ejemplo, ya fue destruido mientras
            // esta pieza sigue desvaneciéndose), simplemente se queda en su última
            // posición conocida.
        }

        void EmitirParticulasNacimiento()
        {
            for (int i = 0; i < 14; i++)
            {
                Dust d = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.Firework_Yellow,
                    0f, -1.5f,
                    0,
                    default,
                    1.1f);

                d.velocity = new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-3f, -0.5f));
                d.noGravity = Main.rand.NextBool();
                d.color = Color.Yellow;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < 3; i++)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.WoodFurniture, 0f, -1f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(ObtenerRutaTextura(Tipo)).Value;
            Vector2 origin = tex.Size() * 0.5f;

            float progresoMuerte = dying ? MathHelper.Clamp(muerteTimer / (float)TiempoMuerte, 0f, 1f) : 0f;
            Color colorFinal = Color.Lerp(lightColor, ColorPodrido, progresoMuerte);
            colorFinal *= (255 - Projectile.alpha) / 255f;

            Main.EntitySpriteDraw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                colorFinal,
                0f,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0f
            );

            return false;
        }

        public static string ObtenerRutaTextura(ArbolPiezaTipo_Tier_2 tipo)
        {
            const string carpeta = "Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Tier_2/Arbol_Tier_2/";
            return tipo switch
            {
                ArbolPiezaTipo_Tier_2.Base => carpeta + "Arbol_Base",
                ArbolPiezaTipo_Tier_2.Segmento1 => carpeta + "Arbol_Segmento_1",
                ArbolPiezaTipo_Tier_2.Segmento2 => carpeta + "Arbol_Segmento_2",
                ArbolPiezaTipo_Tier_2.Segmento3 => carpeta + "Arbol_Segmento_3",
                ArbolPiezaTipo_Tier_2.Cabeza => carpeta + "Arbol_Cabeza",
                _ => carpeta + "Arbol_Base"
            };
        }
    }
}