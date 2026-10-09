using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_1.Arbol_Tier_1
{
    public class Arbol_Torre_Controller_Tier_1 : ModProjectile
    {
        public override string Texture =>
            "Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Tier_1/Arbol_Tier_1/Arbol_Base";

        const int TotalSegmentos = 4;
        const int TicksEntrePiezas = 8;
        const int TicksEntreMuertes = 6;
        const int TiempoVidaMax = 900;
        const float Gravedad = 0.4f;
        const float VelocidadCaidaMax = 12f;

        int etapa;
        int timer;
        int tiempoDeVida;

        Vector2 origen;
        float alturaAcumulada;
        float velocidadY;
        bool enSuelo;

        // =========================================================================
        // FIX BUG MULTIJUGADOR "segmentos mal colocados en caídas largas / desde
        // mucha altura para el jugador que NO invocó el árbol":
        //
        // Antes, la identidad de cada árbol (miId) se calculaba así:
        //     static int ContadorArboles = 0;
        //     miId = ++ContadorArboles;
        // dentro del bloque de inicialización de AI(), que se ejecuta en TODAS las
        // máquinas (incluidos los clientes no-dueños), ya que corre ANTES del
        // "if (Projectile.owner != Main.myPlayer) return;".
        //
        // El problema: ContadorArboles es un contador LOCAL a cada máquina. El
        // mismo árbol podía terminar con un miId distinto en la máquina del dueño
        // y en la de otro jugador, dependiendo de cuántos otros árboles hubiera
        // procesado cada máquina hasta ese momento (orden de llegada de paquetes,
        // otros Stands con árboles activos, etc.). Cuando eso diverge, cada pieza
        // (que sí lleva el miId "canónico" del dueño en su ai[1], transmitido
        // correctamente por red) deja de encontrar coincidencia con la copia local
        // del tronco en la máquina del jugador 2 -> la pieza se queda "perdida" en
        // su última posición conocida. Cuantas más piezas se coloquen mientras el
        // árbol sigue cayendo (caídas largas = más tiempo = más piezas creadas
        // antes de que la situación se "estabilice"), más piezas quedan afectadas.
        //
        // LA SOLUCIÓN: usar Projectile.identity en vez de un contador local.
        // Es un campo NATIVO de Terraria pensado exactamente para esto: el motor
        // garantiza que tiene el MISMO valor en todas las máquinas para el mismo
        // proyectil lógico (a diferencia de whoAmI, que es solo el índice del
        // array local en cada cliente y puede no coincidir entre ellos).
        // =========================================================================
        public int MiId => Projectile.identity;

        private struct PiezaData
        {
            public int Index;
            public float OffsetY;
        }

        // Sigue usándose SOLO en la máquina del dueño, para decidir el ORDEN en el
        // que las piezas empiezan a desvanecerse al morir el árbol (ver
        // ActualizarMuerte). Ya NO se usa para posicionar piezas.
        List<PiezaData> piezas = new();

        bool muriendo;
        int muerteIndex;
        int muerteTimer;

        // =========================================================================
        // FIX "caída con aspecto de lag para el jugador que NO invocó el árbol":
        //
        // El tronco nunca escribe en Projectile.velocity (mueve Projectile.position
        // manualmente dentro de AplicarGravedad, que SOLO corre en la máquina del
        // dueño). Eso significa que, en clientes no-dueños, la posición del tronco
        // se queda completamente CONGELADA entre un paquete de red y el siguiente
        // -> aspecto de caída a saltos/lageada, aunque el dueño la vea perfecta.
        //
        // Solución: sincronizamos la velocidad de caída actual y si ya tocó el
        // suelo (vía ExtraAI). Mientras el árbol sigue cayendo según el último
        // dato recibido, el cliente no-dueño EXTRAPOLA localmente la posición cada
        // tick usando esa velocidad, dando una caída fluida en vez de a saltos. En
        // cuanto llega una corrección real de red (nueva posición, o el aviso de
        // que ya está en el suelo), esta sustituye automáticamente a la
        // predicción, así que no se acumula ninguna deriva permanente.
        // =========================================================================
        float velocidadYSincronizada;
        bool enSueloSincronizado;

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 5000;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(velocidadY);
            writer.Write(enSuelo);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            velocidadYSincronizada = reader.ReadSingle();
            enSueloSincronizado = reader.ReadBoolean();
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void IniciarMuerte()
        {
            if (muriendo) return;
            muriendo = true;
            muerteIndex = 0;
            muerteTimer = 0;
        }

        public static void MatarArbolesDe(int owner)
        {
            foreach (Projectile proj in Main.projectile)
            {
                if (!proj.active || proj.owner != owner) continue;
                if (proj.type != ModContent.ProjectileType<Arbol_Torre_Controller_Tier_1>()) continue;

                if (proj.ModProjectile is Arbol_Torre_Controller_Tier_1 controller)
                    controller.IniciarMuerte();
            }
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                origen = Projectile.Bottom;
                etapa = 0;
                timer = 0;
                alturaAcumulada = 0f;
                tiempoDeVida = 0;
                velocidadY = 0f;
                enSuelo = false;
                piezas = new List<PiezaData>();
                muriendo = false;
                muerteIndex = 0;
                muerteTimer = 0;
            }

            if (Projectile.owner != Main.myPlayer)
            {
                // --- Cliente no-dueño: solo extrapolar visualmente la caída ---
                // Mientras el último dato recibido diga que el árbol sigue cayendo,
                // avanzamos localmente su posición con la última velocidad
                // conocida, para que se vea fluido en vez de a saltos. En cuanto
                // llegue una corrección real (nueva posición sincronizada, o
                // enSueloSincronizado = true), esta prevalece automáticamente.
                if (!enSueloSincronizado)
                {
                    Projectile.position.Y += velocidadYSincronizada;
                }
                return;
            }

            Player p = Main.player[Projectile.owner];

            AplicarGravedad();

            if (muriendo)
            {
                ActualizarMuerte();
                return;
            }

            if (!p.active || p.dead) { IniciarMuerte(); return; }

            if (etapa >= TotalSegmentos + 2)
            {
                tiempoDeVida++;
                if (tiempoDeVida >= TiempoVidaMax)
                {
                    IniciarMuerte();
                }
                return;
            }

            timer++;
            if (timer < TicksEntrePiezas) return;
            timer = 0;

            if (etapa == 0)
            {
                ColocarPieza(p, ArbolPiezaTipo_Tier_1.Base);
            }
            else if (etapa >= 1 && etapa <= TotalSegmentos)
            {
                ArbolPiezaTipo_Tier_1 tipo = (ArbolPiezaTipo_Tier_1)Main.rand.Next(1, 4);
                ColocarPieza(p, tipo);
            }
            else
            {
                ColocarPieza(p, ArbolPiezaTipo_Tier_1.Cabeza);
            }

            etapa++;
        }

        void AplicarGravedad()
        {
            if (enSuelo) return;

            velocidadY += Gravedad;
            if (velocidadY > VelocidadCaidaMax) velocidadY = VelocidadCaidaMax;

            Vector2 posFutura = Projectile.position + new Vector2(0, velocidadY);
            float bottomY = posFutura.Y + Projectile.height;

            int tileX1 = (int)(Projectile.Left.X / 16f);
            int tileX2 = (int)(Projectile.Right.X / 16f);
            int tileY = (int)(bottomY / 16f);

            for (int x = tileX1; x <= tileX2; x++)
            {
                if (WorldGen.InWorld(x, tileY))
                {
                    Tile tile = Main.tile[x, tileY];
                    if (tile != null && tile.HasTile && !tile.IsActuated)
                    {
                        bool esSolido = Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
                        bool esPlataforma = Main.tileSolidTop[tile.TileType];

                        if (esSolido || esPlataforma)
                        {
                            enSuelo = true;
                            velocidadY = 0f;
                            Projectile.position.Y = tileY * 16f - Projectile.height;

                            if (Main.netMode != NetmodeID.SinglePlayer)
                                Projectile.netUpdate = true;

                            break;
                        }
                    }
                }
            }

            if (!enSuelo)
            {
                Projectile.position.Y += velocidadY;

                if (Main.netMode != NetmodeID.SinglePlayer)
                    Projectile.netUpdate = true;
            }

            origen = Projectile.Bottom;
        }

        void ColocarPieza(Player p, ArbolPiezaTipo_Tier_1 tipo)
        {
            float altura = ObtenerAlturaTextura(tipo);
            float centroOffsetY = alturaAcumulada + (altura * 0.5f);
            alturaAcumulada += altura;

            Vector2 posCentro = origen - new Vector2(0, centroOffsetY);

            int idx = Projectile.NewProjectile(
                p.GetSource_FromThis(),
                posCentro,
                Vector2.Zero,
                ModContent.ProjectileType<Arbol_Pieza>(),
                0,
                0f,
                p.whoAmI,
                (float)tipo,
                (float)MiId
            );

            if (idx >= 0 && idx < Main.maxProjectiles)
            {
                Projectile nuevo = Main.projectile[idx];
                nuevo.Center = posCentro;

                // FIX: el OffsetY se fija UNA sola vez aquí, en la máquina del dueño,
                // ANTES de mandar el paquete de sincronización. Como forma parte de
                // ExtraAI, ese mismo paquete de creación ya lo transporta al resto de
                // clientes, que a partir de ahí pueden calcular la posición de la
                // pieza por sí mismos en cada tick, sin depender de más paquetes.
                if (nuevo.ModProjectile is Arbol_Pieza piezaModProj)
                {
                    piezaModProj.OffsetY = centroOffsetY;
                }

                if (Main.netMode != NetmodeID.SinglePlayer)
                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, idx);
            }

            piezas.Add(new PiezaData { Index = idx, OffsetY = centroOffsetY });
        }

        float ObtenerAlturaTextura(ArbolPiezaTipo_Tier_1 tipo)
        {
            string ruta = Arbol_Pieza.ObtenerRutaTextura(tipo);
            return ModContent.Request<Texture2D>(ruta, AssetRequestMode.ImmediateLoad).Value.Height;
        }

        void ActualizarMuerte()
        {
            if (muerteIndex >= piezas.Count)
            {
                Projectile.Kill();
                return;
            }

            if (muerteTimer == 0)
            {
                int idxLista = piezas.Count - 1 - muerteIndex;
                int idxProyectil = piezas[idxLista].Index;

                if (idxProyectil >= 0 && idxProyectil < Main.maxProjectiles)
                {
                    Projectile pieza = Main.projectile[idxProyectil];

                    if (pieza.active
                        && pieza.type == ModContent.ProjectileType<Arbol_Pieza>()
                        && pieza.owner == Projectile.owner
                        && pieza.ai[1] == (float)MiId)
                    {
                        if (pieza.ModProjectile is Arbol_Pieza ap)
                            ap.StartDying();
                    }
                }

                muerteIndex++;
            }

            if (muerteIndex < piezas.Count)
            {
                muerteTimer++;
                if (muerteTimer >= TicksEntreMuertes)
                    muerteTimer = 0;
            }
        }
    }
}