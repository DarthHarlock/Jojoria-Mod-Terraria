using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.GameContent;
using Terraria.ModLoader;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Systems;

namespace Jojo.Content.Projectiles.D4C.D4C_Tier_2
{
    public class ClonJugadorNPC_Tier_2 : ModNPC
    {
        // ====================================================================================
        // ============================ CONFIGURACIÓN DEL CLON ================================
        // ====================================================================================

        // --- Movimiento ---
        const float VELOCIDAD_CAMINAR = 4.5f;
        const float RANGO_BUSQUEDA = 700f;

        // --- Saltos y Plataformas ---
        const float MAX_BLOQUES_SALTO = 10f;
        const float FUERZA_SALTO_MINIMO = -4f;
        const float FUERZA_SALTO_MAXIMO = -16f;

        // --- Explosión y Daño (públicos: los lee el proyectil de la explosión) ---
        public const int DAÑO_EXPLOSION = 60;
        public const int CRIT_BASE_EXPLOSION = 5; // Igual que baseCritChance en el Stand
        public const float RADIO_EXPLOSION = 90f;
        public const int VIDA_MAXIMA_TICKS = 10 * 60;

        // --- Cantidad de clones ---
        public static int CantidadClones = 1;

        // Ticks de "gracia" tras spawnear
        const int TICKS_GRACIA_SPAWN = 20;

        // --- Ajustes Avanzados ---
        const int DURACION_EXPLOSION_TICKS = 20;
        const int INTERVALO_REEVALUAR = 40;

        // ====================================================================================

        public static readonly SoundStyle SonidoSpawnClon = new("Jojo/Content/Sonidos/D4CClon1");

        public static readonly Dictionary<int, List<int>> ClonesActivosPorJugador = new();
        public static readonly Dictionary<int, int> ObjetivoMarcadoPorJugador = new();

        public int OwnerIndex = -1;
        public int ObjetivoNPC = -1;
        public int TiempoVida;

        public bool Explotando;
        public bool localExplotando;

        int ticksDesdeSpawn;
        bool enElSuelo;
        int tiempoExplosion;
        int ticksSinReevaluar;

        // Controla cuánto tiempo el Stand emite partículas para formar el aro/estela
        int ticksEfectoSpawn;

        // Variables para hacer que el anillo de partículas siga al jugador
        Vector2 posStandAnterior;
        List<Dust> dustsAroInicial = new List<Dust>();

        public override string Texture => "Terraria/Images/NPC_1";

        public override void SetStaticDefaults() => Main.npcFrameCount[NPC.type] = 1;

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.aiStyle = -1;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.lifeMax = 5;
            NPC.life = 5;
            NPC.knockBackResist = 0f;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.friendly = true;
            NPC.dontTakeDamage = true;
            NPC.dontCountMe = true;
            NPC.netAlways = true;
            NPC.HitSound = null;
            NPC.DeathSound = null;

            // Reseteo de variables al spawnear
            OwnerIndex = -1;
            ObjetivoNPC = -1;
            TiempoVida = 0;
            Explotando = false;
            localExplotando = false;
            ticksDesdeSpawn = 0;
            enElSuelo = false;
            tiempoExplosion = 0;
            ticksSinReevaluar = 0;
            ticksEfectoSpawn = 0;
            jugadorFantasma = null;
            dustsAroInicial.Clear();
            posStandAnterior = Vector2.Zero;
        }

        public override bool CheckActive() => false;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)OwnerIndex);
            writer.Write(ObjetivoNPC);
            writer.Write(TiempoVida);
            writer.Write(Explotando);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            OwnerIndex = reader.ReadByte();
            ObjetivoNPC = reader.ReadInt32();
            TiempoVida = reader.ReadInt32();
            Explotando = reader.ReadBoolean();
        }

        // ── Helper: Encuentra la posición del Stand del jugador ──
        public static Vector2 ObtenerPosicionStand(Player owner)
        {
            int standType = ModContent.ProjectileType<D4CSTAND_Tier_2>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == owner.whoAmI && p.type == standType)
                {
                    return p.Center;
                }
            }
            return owner.Center;
        }

        // ── EXPLOSIÓN INICIAL DEL ARO (Ahora devuelve partículas para moverlas) ──
        public void GenerarAroInicial(Vector2 centro)
        {
            dustsAroInicial.Clear();
            int particulas = 32;
            float radio = 20f;
            for (int i = 0; i < particulas; i++)
            {
                float angulo = i * (MathHelper.TwoPi / particulas);
                Vector2 direccion = angulo.ToRotationVector2();
                Vector2 posicion = centro + direccion * radio;

                // Solo la velocidad de expansión (ya no añadimos la del jugador porque lo moveremos manualmente)
                Vector2 velocidad = direccion * 2.0f;

                Dust dust = Dust.NewDustPerfect(posicion, DustID.IceTorch, velocidad, Scale: 1.8f);
                dust.noGravity = true;
                dustsAroInicial.Add(dust);

                if (i % 2 == 0)
                {
                    Dust dustElec = Dust.NewDustPerfect(posicion, DustID.Electric, velocidad * 0.8f, Scale: 1.1f);
                    dustElec.noGravity = true;
                    dustsAroInicial.Add(dustElec);
                }
            }
        }

        // ── ESTELA ROTATORIA QUE SIGUE AL STAND ──
        public void GenerarEstelaAro(Vector2 centro, Vector2 velBase, int tick)
        {
            int particulasPorTick = 4;
            float radio = 20f;
            for (int i = 0; i < particulasPorTick; i++)
            {
                float angulo = (i * MathHelper.TwoPi / particulasPorTick) + (tick * 0.3f);
                Vector2 direccion = angulo.ToRotationVector2();
                Vector2 posicion = centro + direccion * radio;

                Vector2 velocidad = direccion * 1.2f + velBase * 0.8f;

                Dust dust = Dust.NewDustPerfect(posicion, DustID.IceTorch, velocidad, Scale: 1.4f);
                dust.noGravity = true;
            }
        }

        public override void AI()
        {
            if (OwnerIndex < 0 || OwnerIndex >= Main.maxPlayers)
            {
                NPC.active = false;
                return;
            }

            Player owner = Main.player[OwnerIndex];
            if (!owner.active || owner.dead)
            {
                NPC.active = false;
                return;
            }

            // ── EFECTO DE ARO (Visible para TODOS y sigue al Stand a cualquier velocidad) ──
            if (ticksEfectoSpawn < 15)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    Vector2 posStand = ObtenerPosicionStand(owner);

                    if (ticksEfectoSpawn == 0)
                    {
                        SoundEngine.PlaySound(SonidoSpawnClon, posStand);
                        GenerarAroInicial(posStand);
                        posStandAnterior = posStand;
                    }
                    else
                    {
                        // Mover manualmente todas las partículas de la explosión inicial junto con el Stand
                        Vector2 deltaStand = posStand - posStandAnterior;
                        foreach (Dust d in dustsAroInicial)
                        {
                            if (d != null && d.active)
                            {
                                d.position += deltaStand;
                            }
                        }
                        posStandAnterior = posStand;
                    }

                    // La estela se sigue generando en la posición actual, así que también rastrea bien
                    GenerarEstelaAro(posStand, owner.velocity, ticksEfectoSpawn);
                }

                ticksEfectoSpawn++;

                if (ticksEfectoSpawn == 15)
                {
                    dustsAroInicial.Clear(); // Limpiamos la lista para liberar memoria al terminar el efecto
                }
            }

            // MUNDO VISUAL: Explosión ordenada por el servidor
            if (Explotando)
            {
                if (!localExplotando)
                {
                    localExplotando = true;
                    if (Main.netMode != NetmodeID.Server)
                    {
                        GenerarEfectosVisualesExplosion();
                        SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
                    }
                }

                ActualizarExplosion();
                ActualizarVisual(owner);
                return;
            }

            ticksDesdeSpawn++;
            TiempoVida++;

            if (TiempoVida >= VIDA_MAXIMA_TICKS)
            {
                Detonar();
                return;
            }

            ActualizarObjetivo(owner);
            ActualizarMovimiento(owner);
            ActualizarVisual(owner);
        }

        public static void EliminarClonesDelJugador(Player owner)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(owner.GetSource_FromThis("D4C_BorrarClones"), owner.Center, Vector2.Zero, ModContent.ProjectileType<D4CRedBridgeProjectile_Tier_2>(), 0, 0, owner.whoAmI, 2f);
                return;
            }

            EliminarClonesDelJugadorServidor(owner);
        }

        public static void EliminarClonesDelJugadorServidor(Player owner)
        {
            if (ClonesActivosPorJugador.TryGetValue(owner.whoAmI, out List<int> prevIndices))
            {
                foreach (int prevIndex in prevIndices)
                {
                    if (prevIndex < 0 || prevIndex >= Main.maxNPCs) continue;

                    NPC prev = Main.npc[prevIndex];
                    if (prev.active && prev.ModNPC is ClonJugadorNPC_Tier_2 prevClon && prevClon.OwnerIndex == owner.whoAmI)
                    {
                        prev.active = false;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, prevIndex);
                    }
                }
            }

            ClonesActivosPorJugador[owner.whoAmI] = new List<int>();
        }

        public static void InvocarUnClon(Player owner, Vector2 spawnPos, int objetivoForzado = -1)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(owner.GetSource_FromThis("D4C_ClonJugador"), spawnPos, Vector2.Zero, ModContent.ProjectileType<D4CRedBridgeProjectile_Tier_2>(), 0, 0, owner.whoAmI, 1f, objetivoForzado);
            }
            else
            {
                InvocarUnClonServidor(owner, spawnPos, objetivoForzado);
            }
        }

        public static void InvocarUnClonServidor(Player owner, Vector2 spawnPos, int objetivoForzado = -1)
        {
            int i = NPC.NewNPC(owner.GetSource_FromThis("D4C_ClonJugador"),
                (int)spawnPos.X, (int)spawnPos.Y, ModContent.NPCType<ClonJugadorNPC_Tier_2>());

            if (i < 0 || i >= Main.maxNPCs) return;

            if (Main.npc[i].ModNPC is ClonJugadorNPC_Tier_2 clon)
            {
                clon.OwnerIndex = owner.whoAmI;
                clon.ObjetivoNPC = objetivoForzado;
                Main.npc[i].netUpdate = true;
            }

            if (!ClonesActivosPorJugador.TryGetValue(owner.whoAmI, out List<int> lista))
            {
                lista = new List<int>();
                ClonesActivosPorJugador[owner.whoAmI] = lista;
            }
            lista.Add(i);
        }

        void ActualizarObjetivo(Player owner)
        {
            if (ObjetivoMarcadoPorJugador.TryGetValue(owner.whoAmI, out int marcado) && marcado >= 0 && marcado < Main.maxNPCs)
            {
                NPC marcadoNpc = Main.npc[marcado];
                if (EsObjetivoValido(marcadoNpc))
                {
                    if (ObjetivoNPC != marcado)
                    {
                        ObjetivoNPC = marcado;
                        NPC.netUpdate = true;
                    }
                    return;
                }
                else
                {
                    ObjetivoMarcadoPorJugador[owner.whoAmI] = -1;
                }
            }

            if (ObjetivoNPC >= 0 && ObjetivoNPC < Main.maxNPCs)
            {
                NPC actual = Main.npc[ObjetivoNPC];
                if (EsObjetivoValido(actual)) return;
            }

            ticksSinReevaluar++;
            bool tocaReevaluar = ObjetivoNPC == -1 || ticksSinReevaluar >= INTERVALO_REEVALUAR;
            if (!tocaReevaluar) return;

            ticksSinReevaluar = 0;

            float mejorDist = RANGO_BUSQUEDA;
            int mejor = -1;

            for (int idx = 0; idx < Main.maxNPCs; idx++)
            {
                NPC n = Main.npc[idx];
                if (!EsObjetivoValido(n)) continue;

                float d = Vector2.Distance(NPC.Center, n.Center);
                if (d < mejorDist)
                {
                    mejorDist = d;
                    mejor = idx;
                }
            }

            if (mejor != ObjetivoNPC)
            {
                ObjetivoNPC = mejor;
                NPC.netUpdate = true;
            }
        }

        bool EsObjetivoValido(NPC n)
        {
            if (!n.active || n.friendly || n.life <= 0 || n.dontTakeDamage) return false;
            if (n.catchItem > 0 || NPCID.Sets.CountsAsCritter[n.type] || n.lifeMax <= 5) return false;
            return true;
        }

        void ActualizarMovimiento(Player owner)
        {
            bool hayObjetivo = ObjetivoNPC >= 0 && ObjetivoNPC < Main.maxNPCs && Main.npc[ObjetivoNPC].active;
            Vector2 destino = hayObjetivo ? Main.npc[ObjetivoNPC].Center : owner.Center;

            Vector2 diff = destino - NPC.Center;
            float distancia = diff.Length();

            if (hayObjetivo && distancia <= NPC.width * 0.75f && ticksDesdeSpawn >= TICKS_GRACIA_SPAWN)
            {
                Detonar();
                return;
            }

            MoverCaminando(diff, hayObjetivo);

            if (Math.Abs(NPC.velocity.X) > 0.5f)
                NPC.direction = NPC.spriteDirection = NPC.velocity.X > 0 ? 1 : -1;
            else if (distancia > 16f)
                NPC.direction = NPC.spriteDirection = diff.X > 0 ? 1 : -1;
        }

        void MoverCaminando(Vector2 diff, bool hayObjetivo)
        {
            NPC.noGravity = false;
            NPC.noTileCollide = false;

            enElSuelo = NPC.velocity.Y == 0f;

            bool cercaDelDueño = !hayObjetivo && diff.Length() < 80f;
            float dirX = cercaDelDueño ? 0 : (Math.Abs(diff.X) < 2f ? 0 : Math.Sign(diff.X));

            if (dirX == 0)
                NPC.velocity.X *= 0.8f;
            else
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, dirX * VELOCIDAD_CAMINAR, 0.2f);

            if (!enElSuelo) return;

            bool objetivoAbajo = diff.Y > 48f && !cercaDelDueño;
            if (objetivoAbajo)
            {
                bool puedeBajar = true;
                int startX = (int)(NPC.position.X / 16f);
                int endX = (int)((NPC.position.X + NPC.width) / 16f);
                int y = (int)((NPC.position.Y + NPC.height + 4f) / 16f);

                for (int x = startX; x <= endX; x++)
                {
                    Tile t = Framing.GetTileSafely(x, y);
                    if (t.HasTile && Main.tileSolid[t.TileType] && !TileID.Sets.Platforms[t.TileType])
                    {
                        puedeBajar = false;
                        break;
                    }
                }

                if (puedeBajar)
                {
                    NPC.position.Y += 6f;
                    return;
                }
            }

            bool objetivoArriba = diff.Y < -32f && !cercaDelDueño;
            bool bloqueadoEnX = dirX != 0 && Math.Abs(NPC.velocity.X) < 0.5f;

            bool obstaculoAdelante = dirX != 0 && Collision.SolidCollision(
                NPC.position + new Vector2(dirX * 16f, -8f), NPC.width, NPC.height);

            if (objetivoArriba || bloqueadoEnX || obstaculoAdelante)
            {
                float alturaDeseada = 0f;

                if (objetivoArriba)
                    alturaDeseada = -diff.Y + 32f;

                if (bloqueadoEnX || obstaculoAdelante)
                    alturaDeseada = Math.Max(alturaDeseada, 64f);

                if (alturaDeseada > 0)
                {
                    float topeSaltoPixeles = MAX_BLOQUES_SALTO * 16f;
                    alturaDeseada = MathHelper.Clamp(alturaDeseada, 16f, topeSaltoPixeles);
                    const float gravedadAprox = 0.4f;
                    float velocidadSalto = -(float)Math.Sqrt(2f * gravedadAprox * alturaDeseada);

                    NPC.velocity.Y = MathHelper.Clamp(velocidadSalto, FUERZA_SALTO_MAXIMO, FUERZA_SALTO_MINIMO);
                }
            }
        }

        void Detonar()
        {
            if (Explotando) return;

            Explotando = true;
            tiempoExplosion = 0;
            NPC.velocity = Vector2.Zero;
            NPC.netUpdate = true;

            // Solo el servidor (o singleplayer) crea el proyectil de la explosión.
            if (Main.netMode != NetmodeID.MultiplayerClient)
                SpawnearExplosion();
        }

        void SpawnearExplosion()
        {
            if (OwnerIndex < 0 || OwnerIndex >= Main.maxPlayers) return;
            Player owner = Main.player[OwnerIndex];
            if (!owner.active) return;

            int projIndex = Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                NPC.Center,
                Vector2.Zero,
                ModContent.ProjectileType<D4CClonExplosionProjectile_Tier_2>(),
                0,
                0f,
                OwnerIndex
            );

            // FIX: en un servidor dedicado, el proyectil se creaba SOLO en el servidor
            // y ningún cliente se enteraba de que existía. El daño de proyectiles
            // "friendly" lo calcula el cliente dueño (owner == Main.myPlayer): sin este
            // SyncProjectile, ese cliente nunca corre Colliding() ni reporta el golpe
            // al servidor, por eso la explosión no hacía daño en multijugador.
            if (projIndex >= 0 && projIndex < Main.maxProjectiles && Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, projIndex);
            }
        }

        void GenerarEfectosVisualesExplosion()
        {
            int smokeParticles = 125;
            for (int i = 0; i < smokeParticles; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(8.0f, 8.0f);
                Dust smoke = Dust.NewDustDirect(NPC.Center, 0, 0, DustID.Smoke,
                    velocity.X, velocity.Y, 130, Color.DarkGray, Main.rand.NextFloat(2.5f, 3.8f));
                smoke.noGravity = true;
                smoke.fadeIn = 1.4f;
            }

            int scatterFire = 70;
            for (int i = 0; i < scatterFire; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(7.5f, 7.5f);
                Dust fire = Dust.NewDustDirect(NPC.Center, 0, 0, DustID.Torch,
                    velocity.X, velocity.Y, 100, default, Main.rand.NextFloat(2.0f, 3.0f));
                fire.noGravity = true;
                fire.velocity = velocity * 1.35f;
            }

            int fireRingParticles = 80;
            float ringSpeed = 6.0f;

            for (int i = 0; i < fireRingParticles; i++)
            {
                float angle = i * (MathHelper.TwoPi / fireRingParticles);
                Vector2 velocity = angle.ToRotationVector2() * ringSpeed;

                Dust ringDust = Dust.NewDustDirect(NPC.Center, 0, 0, DustID.Torch,
                    velocity.X, velocity.Y, 100, default, Main.rand.NextFloat(1.8f, 2.5f));
                ringDust.noGravity = true;
                ringDust.velocity = velocity;
            }

            for (int g = 0; g < 3; g++)
            {
                Terraria.Gore.NewGore(
                    NPC.GetSource_Death(),
                    NPC.Center,
                    new Vector2(Main.rand.NextFloat(-2.8f, 2.8f), Main.rand.NextFloat(-2.8f, 2.8f)),
                    Main.rand.Next(61, 64),
                    1.1f
                );
            }
        }

        void ActualizarExplosion()
        {
            tiempoExplosion++;
            NPC.velocity *= 0.9f;

            if (tiempoExplosion >= DURACION_EXPLOSION_TICKS)
                NPC.active = false;
        }

        Player jugadorFantasma;

        void ActualizarVisual(Player owner)
        {
            if (jugadorFantasma == null)
            {
                jugadorFantasma = new Player();
                CopiarApariencia(owner, jugadorFantasma);
            }

            jugadorFantasma.dead = false;
            jugadorFantasma.active = true;
            jugadorFantasma.invis = false;
            jugadorFantasma.whoAmI = owner.whoAmI;

            jugadorFantasma.position = NPC.position;
            jugadorFantasma.velocity = NPC.velocity;
            jugadorFantasma.oldPosition = NPC.oldPosition;
            jugadorFantasma.oldVelocity = NPC.oldVelocity;

            jugadorFantasma.width = NPC.width;
            jugadorFantasma.height = NPC.height;
            jugadorFantasma.direction = NPC.direction;
            jugadorFantasma.gravity = Player.defaultGravity;

            jugadorFantasma.controlLeft = NPC.velocity.X < -0.1f;
            jugadorFantasma.controlRight = NPC.velocity.X > 0.1f;
            jugadorFantasma.controlUp = NPC.velocity.Y < -0.1f;

            jugadorFantasma.controlJump = false;
            jugadorFantasma.jump = 0;
            jugadorFantasma.wingTime = 100f;

            try
            {
                jugadorFantasma.ResetEffects();
                jugadorFantasma.UpdateEquips(OwnerIndex);
                jugadorFantasma.UpdateDyes();
                jugadorFantasma.PlayerFrame();
            }
            catch (Exception)
            {
                // Silenciado
            }
        }

        static void CopiarApariencia(Player origen, Player destino)
        {
            destino.skinVariant = origen.skinVariant;
            destino.hair = origen.hair;
            destino.hairColor = origen.hairColor;
            destino.skinColor = origen.skinColor;
            destino.eyeColor = origen.eyeColor;
            destino.shirtColor = origen.shirtColor;
            destino.underShirtColor = origen.underShirtColor;
            destino.pantsColor = origen.pantsColor;
            destino.shoeColor = origen.shoeColor;
            destino.name = origen.name;
            destino.Male = origen.Male;

            destino.head = origen.head;
            destino.body = origen.body;
            destino.legs = origen.legs;

            for (int i = 0; i < origen.armor.Length; i++)
            {
                if (destino.armor.Length > i && origen.armor[i] != null)
                    destino.armor[i] = origen.armor[i].Clone();
            }

            for (int i = 0; i < origen.dye.Length; i++)
            {
                if (destino.dye.Length > i && origen.dye[i] != null)
                    destino.dye[i] = origen.dye[i].Clone();
            }

            for (int i = 0; i < origen.hideVisibleAccessory.Length; i++)
            {
                if (destino.hideVisibleAccessory.Length > i)
                    destino.hideVisibleAccessory[i] = origen.hideVisibleAccessory[i];
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (jugadorFantasma != null)
            {
                try
                {
                    Main.PlayerRenderer.DrawPlayer(
                        Main.Camera,
                        jugadorFantasma,
                        jugadorFantasma.position,
                        jugadorFantasma.fullRotation,
                        jugadorFantasma.fullRotationOrigin
                    );
                }
                catch (Exception)
                {
                    // Silenciado
                }
            }

            return false;
        }
    }

    // ====================================================================================
    // PROYECTIL DE LA EXPLOSIÓN (aplica el daño como cualquier arma normal — sin StrikeNPC manual)
    // ====================================================================================
    public class D4CClonExplosionProjectile_Tier_2 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_1";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 20; // antes 4: muy poco margen para MP con ping real
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10; // más que timeLeft: cada NPC solo recibe un golpe
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active)
            {
                Projectile.Kill();
                return;
            }

            // Exactamente igual que el Stand: se recalcula cada tick a partir de los
            // bonos de daño/crítico del jugador. El propio motor de Terraria se encarga
            // de aplicar la variación aleatoria y el golpe al colisionar.
            Projectile.CritChance = StandCritSystem.GetFinalCritChance(owner, ClonJugadorNPC_Tier_2.CRIT_BASE_EXPLOSION);
            Projectile.damage = (int)owner.GetTotalDamage(ModContent.GetInstance<ClaseStand>())
                .ApplyTo(ClonJugadorNPC_Tier_2.DAÑO_EXPLOSION);
        }

        // Reemplaza la hitbox rectangular por defecto por un radio real,
        // igual que la explosión original (RADIO_EXPLOSION).
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Vector2.Distance(Projectile.Center, targetHitbox.Center.ToVector2()) <= ClonJugadorNPC_Tier_2.RADIO_EXPLOSION;
        }
    }

    // ====================================================================================
    // PROYECTIL PUENTE DE RED
    // ====================================================================================
    public class D4CRedBridgeProjectile_Tier_2 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_1";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.timeLeft = 2;
            Projectile.alpha = 255;
            Projectile.hide = true;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (Projectile.localAI[0] != 0) return;
            Projectile.localAI[0] = 1;

            Player owner = Main.player[Projectile.owner];
            if (!owner.active) return;

            if (Projectile.ai[0] == 1f)
            {
                int objetivo = (int)Projectile.ai[1];
                ClonJugadorNPC_Tier_2.InvocarUnClonServidor(owner, Projectile.Center, objetivo);
            }
            else if (Projectile.ai[0] == 2f)
            {
                ClonJugadorNPC_Tier_2.EliminarClonesDelJugadorServidor(owner);
            }
            else if (Projectile.ai[0] == 3f)
            {
                
                ClonJugadorNPC_Tier_2.EliminarClonesDelJugadorServidor(owner);
                AliadoMeleeNPC_Tier_2.EliminarAliadosDelJugadorServidor(owner);
                AliadoRangedNPC_Tier_2.EliminarAliadosDelJugadorServidor(owner);
            }
        }
    }
}