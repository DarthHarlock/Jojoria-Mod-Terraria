using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.D4C.D4C_Tier_4
{
    public class AliadoRangedNPC_Tier_4 : ModNPC
    {
        const float VELOCIDAD_CAMINAR = 5.0f;
        const float RANGO_BUSQUEDA = 900f;
        const float MAX_BLOQUES_SALTO = 10f;
        const float FUERZA_SALTO_MINIMO = -4f;
        const float FUERZA_SALTO_MAXIMO = -16f;
        const float DISTANCIA_IDEAL_MIN = 150f;
        const float DISTANCIA_IDEAL_MAX = 350f;

        public const int DAÑO_RANGED = 40;
        // FIX: antes 60*60 (60s, "vivía para siempre" en la práctica). Ahora 20 segundos exactos.
        // TiempoVida es un campo de INSTANCIA (uno por cada aliado), así que este límite
        // nunca borra aliados de OTROS jugadores: cada uno corre su propio cronómetro.
        public const int VIDA_MAXIMA_TICKS = 20 * 60;
        const int TICKS_GRACIA_SPAWN = 10;
        const int INTERVALO_REEVALUAR = 30;

        const int ITEM_ID_ARMA = ItemID.Revolver;

        static int cacheUseTime = 25;
        static bool cacheCargado;

        // --- Anti-stacking (mismos parámetros que el Melee) ---
        const float RADIO_EVASION = 34f;
        const float FUERZA_EVASION_MAX = 3f;
        const float PESO_EVASION_CAMINANDO = 55f;
        const float PESO_EVASION_QUIETO = 1.4f;

        // --- Crítico ---
        const int CRIT_BASE_PORCENTAJE = 5;

        // --- Anti-atasco en bloques ---
        const int TICKS_ATASCO_TELEPORT = 180;
        int ticksAtascado;

        // --- Reposicionamiento por falta de línea de tiro ---
        // Si un bloque tapa el disparo, en vez de quedarse quieto apuntando en vano,
        // el aliado se desplaza lateralmente buscando un ángulo libre. Todo el cálculo
        // es determinista (sin Main.rand), basado únicamente en posiciones y contadores
        // ya sincronizados, para que el comportamiento sea IDÉNTICO en todos los clientes
        // y en el servidor sin depender de sincronización extra.
        const int TICKS_GRACIA_SIN_VISION = 6;    // ~0.1s de gracia antes de reposicionarse (evita jitter)
        const int TICKS_CAMBIO_ESTRAFEO = 90;     // si sigue bloqueado, prueba el lado contrario cada 1.5s
        int ticksSinLineaVision;
        int direccionEstrafeo; // 0 = sin decidir, 1 = derecha (perpendicular), -1 = izquierda

        public int OwnerIndex = -1;
        public int ObjetivoNPC = -1;
        public int TiempoVida;
        public float AnguloPersonal;

        int ticksDesdeSpawn;
        bool enElSuelo;
        int ticksSinReevaluar;
        int cooldownAtaque;
        int itemAnimTimer;

        Player jugadorFantasma;

        // ── Efecto de aro de partículas al spawnear (igual que ClonJugadorNPC_Tier_4) ──
        static readonly SoundStyle SonidoSpawnAliado = new("Jojo/Content/Sonidos/D4CClon1");
        int ticksEfectoSpawn;
        Vector2 posAnteriorEfecto;
        List<Dust> dustsAroInicial = new List<Dust>();

        public override string Texture => "Terraria/Images/NPC_1";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.aiStyle = -1;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.lifeMax = 150;
            NPC.life = 150; //VIDA
            NPC.knockBackResist = 0.3f;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.friendly = true;
            NPC.dontTakeDamage = false;
            NPC.dontCountMe = true;
            NPC.netAlways = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;

            // Reseteo del efecto de spawn por si el pool de NPCs reutiliza esta instancia
            ticksEfectoSpawn = 0;
            posAnteriorEfecto = Vector2.Zero;
            dustsAroInicial.Clear();

            // Reseteo del estado de reposicionamiento por el mismo motivo
            ticksSinLineaVision = 0;
            direccionEstrafeo = 0;
        }

        public static void InvocarUnAliadoServidor(Player owner, Vector2 posicion, float angulo)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            int npcIdx = NPC.NewNPC(owner.GetSource_FromThis("D4C_Ranged"), (int)posicion.X, (int)posicion.Y, ModContent.NPCType<AliadoRangedNPC_Tier_4>());
            if (npcIdx >= 0 && npcIdx < Main.maxNPCs)
            {
                NPC npc = Main.npc[npcIdx];
                if (npc.ModNPC is AliadoRangedNPC_Tier_4 aliado)
                {
                    aliado.OwnerIndex = owner.whoAmI;
                    aliado.AnguloPersonal = angulo;
                    npc.netUpdate = true;
                }
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIdx);
                }
            }
        }

        public static void EliminarAliadosDelJugadorServidor(Player owner)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.type == ModContent.NPCType<AliadoRangedNPC_Tier_4>())
                {
                    if (npc.ModNPC is AliadoRangedNPC_Tier_4 aliado && aliado.OwnerIndex == owner.whoAmI)
                    {
                        npc.active = false;
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                        }
                    }
                }
            }
        }

        public override bool CheckActive() => false;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)OwnerIndex);
            writer.Write((short)ObjetivoNPC);
            writer.Write(TiempoVida);
            writer.Write(AnguloPersonal);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            OwnerIndex = reader.ReadByte();
            ObjetivoNPC = reader.ReadInt16();
            TiempoVida = reader.ReadInt32();
            AnguloPersonal = reader.ReadSingle();
        }

        static void CargarStatsArma()
        {
            if (cacheCargado) return;
            cacheCargado = true;
            Item referencia = new Item();
            referencia.SetDefaults(ITEM_ID_ARMA);
            if (referencia.useTime > 0) cacheUseTime = referencia.useTime;
        }

        // ── EXPLOSIÓN INICIAL DEL ARO (idéntico al Clon: 32 partículas en círculo) ──
        void GenerarAroInicialSpawn(Vector2 centro)
        {
            dustsAroInicial.Clear();
            int particulas = 32;
            float radio = 20f;
            for (int i = 0; i < particulas; i++)
            {
                float angulo = i * (MathHelper.TwoPi / particulas);
                Vector2 direccion = angulo.ToRotationVector2();
                Vector2 posicion = centro + direccion * radio;

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

        // ── ESTELA ROTATORIA QUE SIGUE AL ALIADO MIENTRAS DURA EL EFECTO ──
        void GenerarEstelaAroSpawn(Vector2 centro, Vector2 velBase, int tick)
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
            CargarStatsArma();

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

            // ── EFECTO DE ARO AL SPAWNEAR (visible para TODOS, igual que ClonJugadorNPC_Tier_4) ──
            if (ticksEfectoSpawn < 15)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    Vector2 posActual = NPC.Center;

                    if (ticksEfectoSpawn == 0)
                    {
                        SoundEngine.PlaySound(SonidoSpawnAliado, posActual);
                        GenerarAroInicialSpawn(posActual);
                        posAnteriorEfecto = posActual;
                    }
                    else
                    {
                        // Movemos manualmente las partículas de la explosión inicial junto con el aliado
                        Vector2 delta = posActual - posAnteriorEfecto;
                        foreach (Dust d in dustsAroInicial)
                        {
                            if (d != null && d.active)
                            {
                                d.position += delta;
                            }
                        }
                        posAnteriorEfecto = posActual;
                    }

                    GenerarEstelaAroSpawn(posActual, NPC.velocity, ticksEfectoSpawn);
                }

                ticksEfectoSpawn++;

                if (ticksEfectoSpawn == 15)
                {
                    dustsAroInicial.Clear(); // Liberamos memoria al terminar el efecto
                }
            }

            ticksDesdeSpawn++;
            TiempoVida++;

            if (TiempoVida >= VIDA_MAXIMA_TICKS)
            {
                NPC.active = false;
                return;
            }

            NPC.defense = owner.statDefense;

            // FIX PARED: si quedó embebido en un bloque sólido (invocado pegado a una pared,
            // por ejemplo), lo sacamos manualmente acá antes de dejar que la IA normal actúe.
            if (ResolverAtascoEnBloques(owner))
            {
                ActualizarVisual(owner);
                return;
            }

            ActualizarObjetivo(owner);
            ActualizarMovimientoYAtaque(owner);
            ActualizarVisual(owner);
        }

        bool EsObjetivoValido(NPC n) => AliadosD4CTier4Compartido.EsObjetivoValido(NPC, n) && n.active && n.life > 0;

        void ActualizarObjetivo(Player owner)
        {
            if (ObjetivoMarcadoValido(owner)) return;

            if (ObjetivoNPC >= 0 && ObjetivoNPC < Main.maxNPCs && EsObjetivoValido(Main.npc[ObjetivoNPC])) return;

            ticksSinReevaluar++;
            if (ObjetivoNPC != -1 && ticksSinReevaluar < INTERVALO_REEVALUAR) return;
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
                if (Main.netMode != NetmodeID.MultiplayerClient) NPC.netUpdate = true;
            }
        }

        bool ObjetivoMarcadoValido(Player owner)
        {
            if (!ClonJugadorNPC_Tier_4.ObjetivoMarcadoPorJugador.TryGetValue(owner.whoAmI, out int marcado)) return false;
            if (marcado < 0 || marcado >= Main.maxNPCs) return false;

            NPC marcadoNpc = Main.npc[marcado];
            if (!EsObjetivoValido(marcadoNpc)) return false;

            if (ObjetivoNPC != marcado)
            {
                ObjetivoNPC = marcado;
                if (Main.netMode != NetmodeID.MultiplayerClient) NPC.netUpdate = true;
            }
            return true;
        }

        static bool EsAliadoTier4(NPC n) => n.ModNPC is AliadoMeleeNPC_Tier_4 || n.ModNPC is AliadoRangedNPC_Tier_4;

        Vector2 CalcularVectorEvasion()
        {
            Vector2 evasion = Vector2.Zero;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC otro = Main.npc[i];
                if (otro.whoAmI == NPC.whoAmI || !otro.active || !otro.friendly || !EsAliadoTier4(otro)) continue;

                float dist = Vector2.Distance(NPC.Center, otro.Center);
                if (dist >= RADIO_EVASION) continue;

                Vector2 dir;
                if (dist > 1f)
                {
                    dir = NPC.Center - otro.Center;
                    dir.Normalize();
                }
                else
                {
                    dir = new Vector2(NPC.whoAmI > otro.whoAmI ? 1f : -1f, 0f);
                }

                dir.Y = 0f;
                if (dir.X == 0f) dir.X = NPC.whoAmI > otro.whoAmI ? 1f : -1f;

                float peso = (RADIO_EVASION - dist) / RADIO_EVASION;
                evasion += dir * peso;
            }

            if (evasion.LengthSquared() > FUERZA_EVASION_MAX * FUERZA_EVASION_MAX)
            {
                evasion.Normalize();
                evasion *= FUERZA_EVASION_MAX;
            }

            return evasion;
        }

        bool EstaEmbebidoEnBloque()
        {
            const int margen = 6;
            return Collision.SolidCollision(
                NPC.position + new Vector2(margen, margen),
                Math.Max(1, NPC.width - margen * 2),
                Math.Max(1, NPC.height - margen * 2));
        }

        bool ResolverAtascoEnBloques(Player owner)
        {
            if (!EstaEmbebidoEnBloque())
            {
                ticksAtascado = 0;
                return false;
            }

            ticksAtascado++;
            NPC.velocity = Vector2.Zero;
            NPC.position.Y -= 2f;

            if (ticksAtascado > TICKS_ATASCO_TELEPORT)
            {
                NPC.position = owner.Top - new Vector2(NPC.width / 2f, NPC.height + 4f);
                NPC.velocity = Vector2.Zero;
                enElSuelo = false;
                ticksAtascado = 0;
                NPC.netUpdate = true;
            }

            return true;
        }

        // Elige de forma DETERMINISTA hacia qué lado (perpendicular a la línea de tiro)
        // conviene desplazarse para intentar recuperar el ángulo de disparo. Se basa
        // exclusivamente en colisiones de tiles (mismo resultado en todos los clientes y
        // en el servidor, sin RNG), comprobando qué lado tiene más espacio libre.
        int ElegirDireccionEstrafeo(Vector2 diff)
        {
            Vector2 perpendicular = new Vector2(-diff.Y, diff.X);
            if (perpendicular.LengthSquared() > 0.0001f) perpendicular.Normalize();
            else perpendicular = Vector2.UnitX;

            Vector2 puntoDerecha = NPC.Center + perpendicular * 40f;
            Vector2 puntoIzquierda = NPC.Center - perpendicular * 40f;

            bool bloqueadoDerecha = Collision.SolidCollision(puntoDerecha - new Vector2(8f, 8f), 16, 16);
            bool bloqueadoIzquierda = Collision.SolidCollision(puntoIzquierda - new Vector2(8f, 8f), 16, 16);

            if (bloqueadoDerecha && !bloqueadoIzquierda) return -1;
            if (bloqueadoIzquierda && !bloqueadoDerecha) return 1;

            // Empate: usamos el índice del NPC como desempate determinista, así todos los
            // clientes eligen exactamente el mismo lado sin necesidad de sincronizar nada.
            return (NPC.whoAmI % 2 == 0) ? 1 : -1;
        }

        void ActualizarMovimientoYAtaque(Player owner)
        {
            bool hayObjetivo = ObjetivoNPC >= 0 && ObjetivoNPC < Main.maxNPCs && EsObjetivoValido(Main.npc[ObjetivoNPC]);

            if (cooldownAtaque > 0) cooldownAtaque--;

            Vector2 evasion = CalcularVectorEvasion();

            if (!hayObjetivo)
            {
                ticksSinLineaVision = 0;
                direccionEstrafeo = 0;

                Vector2 diffOwner = owner.Center - NPC.Center + evasion * PESO_EVASION_CAMINANDO;
                AliadosD4CTier4Compartido.MoverCaminando(NPC, diffOwner, false,
                    VELOCIDAD_CAMINAR, MAX_BLOQUES_SALTO, FUERZA_SALTO_MINIMO, FUERZA_SALTO_MAXIMO, ref enElSuelo);

                if (Math.Abs(NPC.velocity.X) > 0.5f)
                    NPC.direction = NPC.spriteDirection = NPC.velocity.X > 0 ? 1 : -1;
                return;
            }

            NPC objetivo = Main.npc[ObjetivoNPC];
            float distanciaReal = Vector2.Distance(NPC.Center, objetivo.Center);
            Vector2 diff = objetivo.Center - NPC.Center;

            bool tieneLineaDeVision = distanciaReal <= RANGO_BUSQUEDA &&
                Collision.CanHitLine(NPC.position, NPC.width, NPC.height, objetivo.position, objetivo.width, objetivo.height);

            if (tieneLineaDeVision)
            {
                ticksSinLineaVision = 0;
                direccionEstrafeo = 0;
            }
            else
            {
                ticksSinLineaVision++;
            }

            // FIX: antes, si la distancia ya era "ideal" pero un bloque tapaba el disparo,
            // el aliado se quedaba completamente quieto apuntando sin poder disparar nunca.
            // Ahora, tras una pequeña gracia (para no reaccionar a bloqueos de un solo tick),
            // entra en modo reposicionamiento: se mueve perpendicularmente al objetivo
            // buscando un ángulo libre, y si sigue sin conseguirlo cambia de lado.
            bool reposicionando = ticksSinLineaVision > TICKS_GRACIA_SIN_VISION;

            if (reposicionando)
            {
                if (direccionEstrafeo == 0)
                {
                    direccionEstrafeo = ElegirDireccionEstrafeo(diff);
                }
                else if (ticksSinLineaVision % TICKS_CAMBIO_ESTRAFEO == 0)
                {
                    // Llevamos mucho tiempo bloqueados incluso reposicionándonos (p. ej.
                    // arrinconados contra una esquina): probamos el lado contrario.
                    direccionEstrafeo = -direccionEstrafeo;
                }

                Vector2 perpendicular = new Vector2(-diff.Y, diff.X);
                if (perpendicular.LengthSquared() > 0.0001f) perpendicular.Normalize();
                else perpendicular = Vector2.UnitX;

                Vector2 objetivoMovimiento = perpendicular * direccionEstrafeo * 200f;

                // Sin dejar de respetar la distancia ideal mientras nos reposicionamos.
                if (distanciaReal > DISTANCIA_IDEAL_MAX)
                    objetivoMovimiento += diff;
                else if (distanciaReal < DISTANCIA_IDEAL_MIN)
                    objetivoMovimiento -= diff;

                AliadosD4CTier4Compartido.MoverCaminando(NPC, objetivoMovimiento + evasion * PESO_EVASION_CAMINANDO, true,
                    VELOCIDAD_CAMINAR, MAX_BLOQUES_SALTO, FUERZA_SALTO_MINIMO, FUERZA_SALTO_MAXIMO, ref enElSuelo);

                NPC.direction = NPC.spriteDirection = objetivo.Center.X > NPC.Center.X ? 1 : -1;
            }
            else if (distanciaReal > DISTANCIA_IDEAL_MAX)
            {
                AliadosD4CTier4Compartido.MoverCaminando(NPC, diff + evasion * PESO_EVASION_CAMINANDO, true,
                    VELOCIDAD_CAMINAR, MAX_BLOQUES_SALTO, FUERZA_SALTO_MINIMO, FUERZA_SALTO_MAXIMO, ref enElSuelo);

                NPC.direction = NPC.spriteDirection = objetivo.Center.X > NPC.Center.X ? 1 : -1;
            }
            else if (distanciaReal < DISTANCIA_IDEAL_MIN)
            {
                AliadosD4CTier4Compartido.MoverCaminando(NPC, -diff + evasion * PESO_EVASION_CAMINANDO, false,
                    VELOCIDAD_CAMINAR * 0.8f, MAX_BLOQUES_SALTO, FUERZA_SALTO_MINIMO, FUERZA_SALTO_MAXIMO, ref enElSuelo);

                NPC.direction = NPC.spriteDirection = objetivo.Center.X > NPC.Center.X ? 1 : -1;
            }
            else
            {
                NPC.velocity.X *= 0.5f;
                NPC.velocity.X += evasion.X * PESO_EVASION_QUIETO;

                NPC.direction = NPC.spriteDirection = objetivo.Center.X > NPC.Center.X ? 1 : -1;
            }

            bool puedeAtacar = tieneLineaDeVision && ticksDesdeSpawn >= TICKS_GRACIA_SPAWN;

            if (puedeAtacar && cooldownAtaque <= 0)
            {
                Atacar(owner, objetivo);
                cooldownAtaque = cacheUseTime + 15;
            }
        }

        void Atacar(Player owner, NPC objetivo)
        {
            itemAnimTimer = cacheUseTime;

            Vector2 direccion = objetivo.Center - NPC.Center;
            if (direccion == Vector2.Zero) direccion = -Vector2.UnitY;
            direccion.Normalize();

            Vector2 vel = direccion * 16f;
            Vector2 spawnPos = NPC.Center + direccion * 12f;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int projIndex = Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    spawnPos,
                    vel,
                    ProjectileID.Bullet,
                    DAÑO_RANGED,
                    2f,
                    owner.whoAmI
                );

                if (projIndex >= 0 && projIndex < Main.maxProjectiles)
                {
                    Main.projectile[projIndex].friendly = true;
                    Main.projectile[projIndex].hostile = false;
                    Main.projectile[projIndex].penetrate = 3;
                    Main.projectile[projIndex].usesLocalNPCImmunity = true;
                    Main.projectile[projIndex].localNPCHitCooldown = 10;
                    Main.projectile[projIndex].CritChance = CRIT_BASE_PORCENTAJE;

                    // PENETRACIÓN DE ARMADURA AÑADIDA (igual que el Stand)
                    Main.projectile[projIndex].ArmorPenetration = 1000;

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, projIndex);
                    }
                }
            }

            // Evitamos intentar reproducir sonido en el servidor dedicado (no tiene motor
            // de audio inicializado); cada cliente lo reproduce localmente al llegar a la
            // misma decisión determinista de disparar.
            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Item11, NPC.Center);
            }
        }

        void ActualizarVisual(Player owner)
        {
            if (jugadorFantasma == null)
            {
                jugadorFantasma = new Player();
                AliadosD4CTier4Compartido.CopiarApariencia(owner, jugadorFantasma);
                jugadorFantasma.inventory[0] = new Item();
                jugadorFantasma.inventory[0].SetDefaults(ITEM_ID_ARMA);
                jugadorFantasma.selectedItem = 0;
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

            if (itemAnimTimer > 0)
            {
                jugadorFantasma.itemAnimation = itemAnimTimer;
                jugadorFantasma.itemAnimationMax = cacheUseTime;
                jugadorFantasma.itemTime = itemAnimTimer;
                jugadorFantasma.controlUseItem = true;
                itemAnimTimer--;
            }
            else
            {
                jugadorFantasma.itemAnimation = 1;
                jugadorFantasma.itemAnimationMax = 2;
                jugadorFantasma.itemTime = 1;
                jugadorFantasma.controlUseItem = true;
            }

            try
            {
                jugadorFantasma.ResetEffects();
                jugadorFantasma.UpdateEquips(OwnerIndex);
                jugadorFantasma.UpdateDyes();
                jugadorFantasma.PlayerFrame();
            }
            catch (Exception) { }
        }

        void DibujarArmaEnMano(SpriteBatch spriteBatch, Vector2 screenPos)
        {
            if (jugadorFantasma == null) return;

            Main.instance.LoadItem(ITEM_ID_ARMA);
            Texture2D tex = TextureAssets.Item[ITEM_ID_ARMA].Value;

            Vector2 offsetMano = new Vector2(NPC.direction * 6f, 4f);
            Vector2 manoPos = jugadorFantasma.Center + offsetMano - screenPos;

            float angulo = 0f;
            bool hayObjetivo = ObjetivoNPC >= 0 && ObjetivoNPC < Main.maxNPCs && Main.npc[ObjetivoNPC].active;

            if (hayObjetivo)
            {
                Vector2 dir = Main.npc[ObjetivoNPC].Center - NPC.Center;
                angulo = dir.ToRotation();
            }
            else
            {
                angulo = NPC.direction == 1 ? 0f : MathHelper.Pi;
            }

            SpriteEffects efectos = NPC.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically;
            Vector2 origin = new Vector2(4f, tex.Height / 2f);
            Color luz = Lighting.GetColor((int)(jugadorFantasma.Center.X / 16f), (int)(jugadorFantasma.Center.Y / 16f));

            spriteBatch.Draw(tex, manoPos, null, luz, angulo, origin, 1f, efectos, 0f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (jugadorFantasma != null)
            {
                Main.PlayerRenderer.DrawPlayer(Main.Camera, jugadorFantasma, jugadorFantasma.position, jugadorFantasma.fullRotation, jugadorFantasma.fullRotationOrigin);
                DibujarArmaEnMano(spriteBatch, screenPos);
            }
            return false;
        }
    }
}