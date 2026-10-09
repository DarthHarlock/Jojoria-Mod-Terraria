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
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.D4C.D4C_Tier_2
{
    public class AliadoMeleeNPC_Tier_2 : ModNPC
    {
        const float VELOCIDAD_CAMINAR = 6.0f;
        const float RANGO_BUSQUEDA = 800f;
        const float RANGO_ATAQUE = 60f;
        const float MAX_BLOQUES_SALTO = 10f;
        const float FUERZA_SALTO_MINIMO = -4f;
        const float FUERZA_SALTO_MAXIMO = -16f;

        public const int DAÑO_MELEE = 35;
        public const int VIDA_MAXIMA_TICKS = 20 * 60;
        const int TICKS_GRACIA_SPAWN = 10;
        const int INTERVALO_REEVALUAR = 30;

        // ── DEFENSA BASE DEL CLON ──
        public const int DEFENSA_BASE = 220;

        const int ITEM_ID_ARMA = ItemID.Muramasa;
        static int cacheUseTime = 30;

        const float RADIO_EVASION = 34f;
        const float FUERZA_EVASION_MAX = 3f;
        const float PESO_EVASION_CAMINANDO = 55f;
        const float PESO_EVASION_QUIETO = 1.4f;

        public const int CRIT_BASE_PORCENTAJE = 5;

        public const int ANCHO_HITBOX_ATAQUE = 70;
        public const int ALTO_HITBOX_ATAQUE = 70;

        const int TICKS_ATASCO_TELEPORT = 180;
        int ticksAtascado;

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
            NPC.defense = DEFENSA_BASE;
            NPC.lifeMax = 200;
            NPC.life = 200;
            NPC.knockBackResist = 0.3f;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.friendly = true;
            NPC.dontTakeDamage = false;
            NPC.dontCountMe = true;
            NPC.netAlways = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;

            ticksEfectoSpawn = 0;
            posAnteriorEfecto = Vector2.Zero;
            dustsAroInicial.Clear();
        }

        public static void InvocarUnAliadoServidor(Player owner, Vector2 posicion, float angulo)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            int npcIdx = NPC.NewNPC(owner.GetSource_FromThis("D4C_Melee"), (int)posicion.X, (int)posicion.Y, ModContent.NPCType<AliadoMeleeNPC_Tier_2>());
            if (npcIdx >= 0 && npcIdx < Main.maxNPCs)
            {
                NPC npc = Main.npc[npcIdx];
                if (npc.ModNPC is AliadoMeleeNPC_Tier_2 aliado)
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
                if (npc.active && npc.type == ModContent.NPCType<AliadoMeleeNPC_Tier_2>())
                {
                    if (npc.ModNPC is AliadoMeleeNPC_Tier_2 aliado && aliado.OwnerIndex == owner.whoAmI)
                    {
                        npc.active = false;
                        if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
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
                    dustsAroInicial.Clear();
                }
            }

            ticksDesdeSpawn++;
            TiempoVida++;

            if (TiempoVida >= VIDA_MAXIMA_TICKS)
            {
                NPC.active = false;
                return;
            }

            // ── DEFENSA: base propia del clon + la del jugador ──
            NPC.defense = DEFENSA_BASE + owner.statDefense;

            if (ResolverAtascoEnBloques(owner))
            {
                ActualizarVisual(owner);
                return;
            }

            ActualizarObjetivo(owner);
            ActualizarMovimientoYAtaque(owner);
            ActualizarVisual(owner);
        }

        bool EsObjetivoValido(NPC n) => AliadosD4CTier2Compartido.EsObjetivoValido(NPC, n) && n.active && n.life > 0;

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

            if (!ClonJugadorNPC_Tier_2.ObjetivoMarcadoPorJugador.TryGetValue(owner.whoAmI, out int marcado)) return false;
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

        static bool EsAliadoTier2(NPC n) => n.ModNPC is AliadoMeleeNPC_Tier_2 || n.ModNPC is AliadoRangedNPC_Tier_2;

        Vector2 CalcularVectorEvasion()
        {
            Vector2 evasion = Vector2.Zero;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC otro = Main.npc[i];
                if (otro.whoAmI == NPC.whoAmI || !otro.active || !otro.friendly || !EsAliadoTier2(otro)) continue;

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

        void ActualizarMovimientoYAtaque(Player owner)
        {
            bool hayObjetivo = ObjetivoNPC >= 0 && ObjetivoNPC < Main.maxNPCs && EsObjetivoValido(Main.npc[ObjetivoNPC]);

            if (cooldownAtaque > 0) cooldownAtaque--;

            Vector2 evasion = CalcularVectorEvasion();

            if (!hayObjetivo)
            {
                Vector2 diffOwner = owner.Center - NPC.Center + evasion * PESO_EVASION_CAMINANDO;
                AliadosD4CTier2Compartido.MoverCaminando(NPC, diffOwner, false,
                    VELOCIDAD_CAMINAR, MAX_BLOQUES_SALTO, FUERZA_SALTO_MINIMO, FUERZA_SALTO_MAXIMO, ref enElSuelo);

                if (Math.Abs(NPC.velocity.X) > 0.5f) NPC.direction = NPC.spriteDirection = NPC.velocity.X > 0 ? 1 : -1;
                return;
            }

            NPC objetivo = Main.npc[ObjetivoNPC];
            float distanciaReal = Vector2.Distance(NPC.Center, objetivo.Center);
            Vector2 diff = objetivo.Center - NPC.Center;

            if (distanciaReal > RANGO_ATAQUE * 0.8f)
            {
                AliadosD4CTier2Compartido.MoverCaminando(NPC, diff + evasion * PESO_EVASION_CAMINANDO, true,
                    VELOCIDAD_CAMINAR, MAX_BLOQUES_SALTO, FUERZA_SALTO_MINIMO, FUERZA_SALTO_MAXIMO, ref enElSuelo);
            }
            else
            {
                NPC.velocity.X *= 0.5f;
                NPC.velocity.X += evasion.X * PESO_EVASION_QUIETO;
            }

            NPC.direction = NPC.spriteDirection = objetivo.Center.X > NPC.Center.X ? 1 : -1;

            if (distanciaReal <= RANGO_ATAQUE && ticksDesdeSpawn >= TICKS_GRACIA_SPAWN && cooldownAtaque <= 0)
            {
                Atacar();
                cooldownAtaque = cacheUseTime + 10;
            }
        }

        void Atacar()
        {
            itemAnimTimer = cacheUseTime;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 centroGolpe = new Vector2(
                    NPC.Center.X + (NPC.direction == 1 ? ANCHO_HITBOX_ATAQUE / 2f : -ANCHO_HITBOX_ATAQUE / 2f),
                    NPC.Center.Y
                );

                int projIndex = Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    centroGolpe,
                    Vector2.Zero,
                    ModContent.ProjectileType<D4CAliadoMeleeGolpeProjectile_Tier_2>(),
                    0,
                    5f,
                    OwnerIndex
                );

                if (projIndex >= 0 && projIndex < Main.maxProjectiles && Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, projIndex);
                }
            }

            SoundEngine.PlaySound(SoundID.Item1, NPC.Center);
        }

        void ActualizarVisual(Player owner)
        {
            if (jugadorFantasma == null)
            {
                jugadorFantasma = new Player();
                AliadosD4CTier2Compartido.CopiarApariencia(owner, jugadorFantasma);
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
                jugadorFantasma.itemAnimation = 0;
                jugadorFantasma.itemAnimationMax = 0;
                jugadorFantasma.itemTime = 0;
                jugadorFantasma.controlUseItem = false;
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
            if (jugadorFantasma == null || itemAnimTimer <= 0) return;

            Main.instance.LoadItem(ITEM_ID_ARMA);
            Texture2D tex = TextureAssets.Item[ITEM_ID_ARMA].Value;

            float progress = 1f - ((float)itemAnimTimer / cacheUseTime);
            float swing = MathHelper.Lerp(-1.8f, 1.8f, progress);

            Vector2 offsetMano = new Vector2(NPC.direction * 10f, 0f);
            Vector2 manoPos = jugadorFantasma.Center + offsetMano - screenPos;

            float anguloDerecha = -MathHelper.PiOver4 + swing;

            float angulo = NPC.direction == 1 ? anguloDerecha : -anguloDerecha;
            Vector2 origin = NPC.direction == 1
                ? new Vector2(0, tex.Height)
                : new Vector2(tex.Width, tex.Height);
            SpriteEffects efectos = NPC.direction == 1
                ? SpriteEffects.None
                : SpriteEffects.FlipHorizontally;

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

    public class D4CAliadoMeleeGolpeProjectile_Tier_2 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_1";

        public override void SetDefaults()
        {
            Projectile.width = AliadoMeleeNPC_Tier_2.ANCHO_HITBOX_ATAQUE;
            Projectile.height = AliadoMeleeNPC_Tier_2.ALTO_HITBOX_ATAQUE;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 20;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 25;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            // PENETRACIÓN DE ARMADURA AÑADIDA (igual que el Stand)
            Projectile.ArmorPenetration = 1000;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active)
            {
                Projectile.Kill();
                return;
            }

            StatModifier modificadorDaño = owner.GetDamage(DamageClass.Generic)
                .CombineWith(owner.GetDamage(DamageClass.Melee))
                .CombineWith(owner.GetDamage(ModContent.GetInstance<ClaseStand>()));

            Projectile.damage = Math.Max(1, (int)modificadorDaño.ApplyTo(AliadoMeleeNPC_Tier_2.DAÑO_MELEE));

            int chanceCritico = Math.Clamp(
                (int)(AliadoMeleeNPC_Tier_2.CRIT_BASE_PORCENTAJE + owner.GetCritChance(DamageClass.Melee)),
                0, 100);
            Projectile.CritChance = chanceCritico;
        }
    }
}