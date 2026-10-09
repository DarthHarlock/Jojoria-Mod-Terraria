using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using System;
using System.IO;
using Jojo.Content.Buffs;
using Jojo.Content.Habilidades;
using Jojo.Systems.StandFuncionComunes;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
// Reutilizamos la logica de la Tier 4 (NO se copian los scripts)
using Jojo.Content.Projectiles.CrazyDiamond.Transformacion;
using Jojo.Content.Projectiles.CrazyDiamond.descrafteo;
using Jojo.Content.Projectiles.CrazyDiamond.EscudoRoca;

namespace Jojo.Content.Projectiles.CrazyDiamond.CrazyDiamond_Tier_3
{
    public class CRAZYDIAMONDSTAND_Tier_3 : ModProjectile
    {
        enum State { Idle, Attack }
        State state;

        bool auto, init, dying, fDash;
        NPC target;

        int frame, animT, cd, dashT;
        int attackTimer, swingSoundTimer;

        float manR = 140f, autoR = 140f;
        float innerRadius = 90f;

        ParticulasStands.StandRuntime runtime = new();

        public int baseCritChance = 5;
        public int attackSpeed = 1;

        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");
        static readonly SoundStyle SwingSoundBase = new("Terraria/Sounds/Item_1");

        float syncOffX, syncOffY;
        int netTimer = 0;

        int[] serverHitCd;
        int[] serverItemHitCd;
        int[] playerHealCd = new int[Main.maxPlayers];

        public bool isTransformed = false;
        bool wasTransformed = false; // Detecta el momento exacto del cambio para todos los jugadores

        // =====================================================================
        //  CONFIGURACION DE LA HABILIDAD F (TIER 3) - SIN COOLDOWN  <<<<  EDITA AQUI  >>>>
        //  Estos valores se envian a la logica de la Tier 4 (TransformacionLogic)
        // =====================================================================

        // --- Curacion al golpear mientras estas transformado ---
        public float hostileHealPct = 0.001f;   // % de vida maxima que cura a ENEMIGOS por golpe (0.001 = 0.1%)
        public float friendlyHealPct = 0.05f;   // % de vida maxima que cura a ALIADOS/NPC de pueblo por golpe (0.05 = 5%)
        public float playerHealPct = 0.05f;     // % de vida maxima que cura a JUGADORES (con o sin PvP). Solo cura, nunca daña.

        // --- Carga de la roca ---
        public float rockChargePerHit = 15f;        // Carga que suma cada golpe (la roca se crea al llegar a 100)
        public float rockChargeDecayRate = 0.8f;    // Cuanta carga pierde por tick cuando deja de ser golpeado
        public float rockChargeDecayDelay = 90;     // Ticks sin recibir golpes antes de empezar a perder carga (60 = 1 seg)
        public float rockDefenseMultiplier = 0.5f;  // Multiplicador de dano que recibe la roca (0.5 = recibe 50%)
        public int cinematicDuration = 60;          // Ticks que dura la cinematica de rocas antes de atrapar al enemigo
        public int rockSpawnRate = 3;               // Cada cuantos ticks salen rocas durante la cinematica (menor = mas rocas)

        // --- Cooldown azul (despues de liberar al enemigo) ---
        public float rockCooldownDecayRate = 0.1f;  // Cuanto baja la barra azul por tick (barra de 100; 0.1 = ~1000 ticks)

        // --- Escalado por vida del enemigo/jefe ---
        public float bossHpScalingThreshold = 500f;     // Vida a partir de la cual cuesta mas atraparlo
        public float bossHpScalingPercentPer500 = 0.2f; // +20% de dificultad por cada 500 HP sobre el umbral

        // --- Dano de la transformacion (al convertir en roca) ---
        // Dano = dano del stand * este multiplicador, con variacion de +-15%.
        public float transformDamageMultiplier = 5f;

        // --- Dano y efectos del stand transformado ---
        public float baseDamage = 50f;                  // Dano base del stand
        public float transformedEnemyDamageMult = 0.5f; // Multiplicador de dano a enemigos mientras esta transformado
        public int transformedFriendlyMaxDamage = 1;    // Dano maximo a aliados/NPC de pueblo mientras esta transformado
        public int itemDecraftCooldown = 15;            // Ticks de espera entre descrafteos del mismo objeto

        // =====================================================================
        //  CONFIGURACION DE LA HABILIDAD G - ESCUDO DE ROCAS (TIER 3)  <<<<  EDITA AQUI  >>>>
        //  Usa EscudoRocaProyectil (de la Tier 4)
        // =====================================================================

        public int skillGCooldownTicks = 400;       // Numero ENTERO directo para el Cooldown2 (400 ticks)
        public int shieldRockCount = 15;            // Cantidad de rocas del escudo
        public float shieldRadius = 64f;            // Radio del escudo alrededor del jugador (en pixeles)
        public int shieldDamageBase = 20;           // Dano base de cada roca (se escala con el dano de Stand)
        public float shieldKnockback = 8f;          // Empuje de cada roca

        // --- Aparicion de las rocas (debajo del jugador) ---
        public float shieldSpawnDepthY = 450f;      // Distancia minima bajo el jugador donde aparecen
        public float shieldSpawnDepthRandom = 80f;  // Variacion extra aleatoria de profundidad
        public float shieldSpawnSpacingX = 28f;     // Separacion horizontal entre rocas al aparecer
        public float shieldSpawnJitterX = 12f;      // Variacion aleatoria horizontal
        public float shieldLaunchSpeedMin = 4f;     // Velocidad inicial minima hacia arriba
        public float shieldLaunchSpeedMax = 8f;     // Velocidad inicial maxima hacia arriba

        // =====================================================================

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)state);
            writer.Write(auto);
            writer.Write(fDash);
            writer.Write(syncOffX);
            writer.Write(syncOffY);
            writer.Write(isTransformed);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            state = (State)reader.ReadByte();
            auto = reader.ReadBoolean();
            fDash = reader.ReadBoolean();
            syncOffX = reader.ReadSingle();
            syncOffY = reader.ReadSingle();
            isTransformed = reader.ReadBoolean();
        }

        public override void SetDefaults()
        {
            Projectile.width = 88;
            Projectile.height = 88;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
            Projectile.ArmorPenetration = 1000;
        }

        // Destruye todas las rocas del escudo que pertenecen al dueño de este stand.
        void KillShieldRocks()
        {
            int rockType = ModContent.ProjectileType<EscudoRocaProyectil>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr.active && pr.type == rockType && pr.owner == Projectile.owner)
                {
                    pr.Kill();
                }
            }
        }

        public void StartDying()
        {
            if (dying) return;
            dying = true;
            Projectile.friendly = false;
            Projectile.timeLeft = 25;
            SoundEngine.PlaySound(DespawnSound, Projectile.Center);

            // Stand guardado/desinvocado => el escudo de rocas se destruye al instante
            KillShieldRocks();
        }

        // Red de seguridad: si el stand desaparece por cualquier otra via, las rocas tambien.
        public override void OnKill(int timeLeft)
        {
            KillShieldRocks();
        }

        void UpdateAttackSpeed(Player p)
        {
            float s = p.GetModPlayer<StandStatsPlayer>().standSpeed;
            if (s < 50f) { attackSpeed = 1; Projectile.localNPCHitCooldown = 7; }
            else if (s < 100f) { attackSpeed = 2; Projectile.localNPCHitCooldown = 6; }
            else if (s < 150f) { attackSpeed = 3; Projectile.localNPCHitCooldown = 5; }
            else { attackSpeed = 4; Projectile.localNPCHitCooldown = 4; }
        }

        int GetAttackDelay()
        {
            switch (attackSpeed)
            {
                case 1: return 5;
                case 2: return 4;
                case 3: return 3;
                case 4: return 2;
                default: return 5;
            }
        }

        SoundStyle GetSwingSound(float speed)
        {
            float pitch = speed >= 100f ? 0.05f : speed >= 50f ? 0.08f : 0f;
            return SwingSoundBase with { Pitch = pitch };
        }

        int GetSwingDelay(float speed)
        {
            if (speed >= 100f) return 6;
            if (speed >= 50f) return 8;
            return 11;
        }

        void ServerHitCheck()
        {
            if (serverHitCd == null) serverHitCd = new int[Main.maxNPCs];

            for (int i = 0; i < serverHitCd.Length; i++)
                if (serverHitCd[i] > 0) serverHitCd[i]--;

            if (dying || !isTransformed || state != State.Attack) return;

            Rectangle hb = Projectile.Hitbox;
            int rocaType = ModContent.NPCType<Roca>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!n.active || n.life <= 0 || n.dontTakeDamage || n.immortal || n.type == rocaType) continue;
                if (serverHitCd[i] > 0) continue;
                if (!hb.Intersects(n.Hitbox)) continue;

                serverHitCd[i] = Math.Max(1, Projectile.localNPCHitCooldown);
                ApplyTransformEffectsOnNPC(n);
            }
        }

        void ItemHitCheck()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (dying || !isTransformed || state != State.Attack) return;

            if (serverItemHitCd == null) serverItemHitCd = new int[Main.maxItems];

            for (int i = 0; i < serverItemHitCd.Length; i++)
                if (serverItemHitCd[i] > 0) serverItemHitCd[i]--;

            Rectangle hb = Projectile.Hitbox;

            for (int i = 0; i < Main.maxItems; i++)
            {
                Item item = Main.item[i];
                if (!item.active || item.type == ItemID.None) continue;
                if (serverItemHitCd[i] > 0) continue;
                if (!hb.Intersects(item.Hitbox)) continue;

                if (DescrafteoLogic.IntentarDescraftear(item, item.Center))
                {
                    serverItemHitCd[i] = itemDecraftCooldown;
                }
            }
        }

        // Cura a jugadores aunque NO tengan PvP activado. Solo el dueño del stand lo ejecuta.
        // No hace daño: es una colision manual, independiente del sistema de golpes de vanilla.
        void PlayerHealCheck()
        {
            for (int i = 0; i < playerHealCd.Length; i++)
                if (playerHealCd[i] > 0) playerHealCd[i]--;

            if (dying || !isTransformed || state != State.Attack) return;

            Rectangle hb = Projectile.Hitbox;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player t = Main.player[i];
                if (!t.active || t.dead || t.ghost || t.whoAmI == Projectile.owner) continue;
                if (playerHealCd[i] > 0) continue;
                if (!hb.Intersects(t.Hitbox)) continue;

                playerHealCd[i] = Math.Max(1, Projectile.localNPCHitCooldown);
                TransformacionLogic.HealTargetPlayer(t, playerHealPct);
            }
        }

        // Aplica curacion + carga de roca (logica compartida con la Tier 4)
        void ApplyTransformEffectsOnNPC(NPC n)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (n.type == ModContent.NPCType<Roca>()) return;

            TransformacionLogic.HealTargetNPC(n, hostileHealPct, friendlyHealPct);

            TransformacionLogic.AddRockCharge(
                n,
                rockChargePerHit,
                rockChargeDecayRate,
                (int)rockChargeDecayDelay,
                rockDefenseMultiplier,
                cinematicDuration,
                rockSpawnRate,
                bossHpScalingThreshold,
                bossHpScalingPercentPer500,
                rockCooldownDecayRate,
                Projectile.damage * transformDamageMultiplier
            );
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            UpdateAttackSpeed(p);

            if (dying) { ParticulasStands.Despawn(Projectile, p); return; }
            if (!p.active || p.dead) { StartDying(); return; }

            if (!init)
            {
                init = true;
                Projectile.alpha = 0;
                Projectile.friendly = false;
                Projectile.damage = 0;
                SoundEngine.PlaySound(SpawnSound, Projectile.Center);
                ParticulasStands.Spawn(p, ParticulasStands.Stands.CrazyDiamond3);
            }

            var data = ParticulasStands.Stands.CrazyDiamond3;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);

            bool spawning = runtime.spawning;
            bool isOwner = Projectile.owner == Main.myPlayer;

            if (!spawning && isOwner)
            {
                HandleSkills(p);
                HandleToggle(p);
            }

            auto = Projectile.ai[0] == 1f;

            if (!spawning)
            {
                UpdateSkills(p);
            }

            if (!spawning && Main.netMode == NetmodeID.Server)
            {
                ServerHitCheck();
            }

            if (!spawning && Main.netMode != NetmodeID.MultiplayerClient)
            {
                ItemHitCheck();
            }

            if (isOwner)
            {
                bool canAttack = ParticulasStands.CanAttack(runtime) && (auto ? (target = FindEnemy(p)) != null : Main.mouseLeft);
                state = canAttack ? State.Attack : State.Idle;
                Projectile.friendly = canAttack;

                Vector2 off = GetOffset(p);
                float max = (auto ? autoR : manR);

                if (off.Length() > max)
                    off = Vector2.Normalize(off) * max;

                syncOffX = off.X;
                syncOffY = off.Y;

                if (state == State.Attack)
                {
                    BarrageSystem.SpawnPunches(Projectile, p, off);

                    float s = p.GetModPlayer<StandStatsPlayer>().standSpeed;
                    int swingDelay = GetSwingDelay(s);

                    swingSoundTimer++;
                    if (swingSoundTimer >= swingDelay)
                    {
                        SoundEngine.PlaySound(GetSwingSound(s), Projectile.Center);
                        swingSoundTimer = 0;
                    }

                    attackTimer++;
                    int delay = GetAttackDelay();

                    if (attackTimer >= delay)
                    {
                        attackTimer = 0;
                        Projectile.friendly = true;
                    }
                }
                else swingSoundTimer = 0;

                if (state == State.Attack)
                {
                    if (off.Length() <= innerRadius) Projectile.rotation = 0f;
                    else
                    {
                        float rot = off.ToRotation();
                        if (off.X < 0) rot += MathHelper.Pi;
                        Projectile.rotation = rot;
                    }
                }
                else Projectile.rotation = 0f;

                ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);

                if (++netTimer >= 5)
                {
                    netTimer = 0;
                    Projectile.netUpdate = true;
                }
            }
            else
            {
                Vector2 off = new Vector2(syncOffX, syncOffY);

                if (state == State.Attack)
                {
                    if (off.Length() <= innerRadius) Projectile.rotation = 0f;
                    else
                    {
                        float rot = off.ToRotation();
                        if (off.X < 0) rot += MathHelper.Pi;
                        Projectile.rotation = rot;
                    }
                }
                else Projectile.rotation = 0f;

                ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);
            }

            // Curacion de jugadores (solo el dueño, solo transformado, sin daño)
            if (!spawning && isOwner)
            {
                PlayerHealCheck();
            }

            // =========================================================
            // EFECTOS VISUALES Y DE SONIDO DE LA TRANSFORMACION (SKILL F)
            // =========================================================
            if (!spawning)
            {
                if (isTransformed && !wasTransformed)
                {
                    SoundEngine.PlaySound(SoundID.Item29, Projectile.Center);

                    for (int i = 0; i < 35; i++)
                    {
                        Vector2 velocity = Main.rand.NextVector2Circular(9f, 9f);
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.YellowTorch, velocity, 0, default, 2.5f);
                        d.noGravity = true;
                        d.scale = 2.5f;
                    }
                }

                if (isTransformed)
                {
                    if (Main.rand.NextBool(3))
                    {
                        Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(35f, 40f);
                        Vector2 driftVel = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-1.5f, -0.2f));

                        Dust d = Dust.NewDustPerfect(dustPos, DustID.YellowTorch, driftVel, 0, default, 2.0f);
                        d.noGravity = true;
                        d.scale = 2.0f;
                    }
                }

                wasTransformed = isTransformed;
            }

            // Solo el dueño calcula el daño; el servidor lo recibe por la sincronizacion del proyectil.
            if (isOwner)
            {
                Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);
                Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);
            }

            if (!spawning)
            {
                Animate();
            }
        }

        Vector2 GetOffset(Player p)
        {
            if (fDash) return (Main.MouseWorld - p.Center) * 0.35f;

            if (state == State.Attack)
                return auto && target != null
                    ? target.Center - p.Center
                    : Main.MouseWorld - p.Center;

            return new Vector2(-40 * p.direction, -10);
        }

        void UpdateSkills(Player p)
        {
            if (fDash && ++dashT > 6)
            {
                fDash = false;
                dashT = 0;
                if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
            }
        }

        void HandleToggle(Player p)
        {
            if (cd-- > 0 || p.whoAmI != Main.myPlayer) return;

            if (JojoKeybinds.ToggleAuto.JustPressed)
            {
                auto = !auto;
                Projectile.ai[0] = auto ? 1f : 0f;
                cd = 30;
                target = null;

                if (Main.netMode != NetmodeID.Server)
                {
                    Main.NewText(auto ? "AutoStand-ON" : "AutoStand-OFF", 255, 255, 0);
                }
                Projectile.netUpdate = true;
            }
        }

        // HABILIDADES F (transformacion) y G (escudo de rocas). La Tier 3 no tiene H.
        void HandleSkills(Player p)
        {
            if (p.whoAmI != Main.myPlayer) return;
            if (!ParticulasStands.CanUseSkills(runtime)) return;

            // ---------------- SKILL F (Sin NINGUN cooldown de ningun tipo) ----------------
            if (JojoKeybinds.SkillF.JustPressed)
            {
                isTransformed = !isTransformed;
                fDash = false;
                Projectile.netUpdate = true;
            }

            // ---------------- SKILL G (ESCUDO DE ROCAS) ----------------
            if (JojoKeybinds.SkillG.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown2>()))
            {
                isTransformed = false;

                // Aplica el cooldown usando directamente el numero entero (400)
                p.AddBuff(ModContent.BuffType<Cooldown2>(), skillGCooldownTicks);
                Projectile.netUpdate = true;

                int totalRocas = Math.Max(1, shieldRockCount);

                // Dano (se escala con el dano de Stand del jugador)
                int finalDamage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(shieldDamageBase);

                for (int i = 0; i < totalRocas; i++)
                {
                    float offsetX = (i - (totalRocas - 1) / 2f) * shieldSpawnSpacingX + Main.rand.NextFloat(-shieldSpawnJitterX, shieldSpawnJitterX);
                    float offsetY = shieldSpawnDepthY + Main.rand.NextFloat(0f, shieldSpawnDepthRandom);
                    Vector2 spawnPos = p.Center + new Vector2(offsetX, offsetY);

                    Vector2 initialVelocity = new Vector2(
                        Main.rand.NextFloat(-1f, 1f),
                        -Main.rand.NextFloat(shieldLaunchSpeedMin, shieldLaunchSpeedMax));

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        spawnPos,
                        initialVelocity,
                        ModContent.ProjectileType<EscudoRocaProyectil>(),
                        finalDamage,
                        shieldKnockback,
                        p.whoAmI,
                        i,            // ai[0] = indice de la roca
                        totalRocas,   // ai[1] = cantidad total de rocas
                        shieldRadius  // ai[2] = radio del escudo
                    );
                }
            }
        }

        // Auto-apuntado: solo hostiles (ignora pacíficos, critters, town NPCs y dummies).
        // Aplica el multiplicador de rango internamente.
        NPC FindEnemy(Player p)
        {
            return StandTargeting.FindHostileEnemy(p, autoR);
        }

        void Animate()
        {
            if (++animT < (state == State.Attack ? 5 : 9)) return;
            animT = 0;

            frame = state == State.Idle
                ? (frame + 1) % 4
                : (frame < 4 || frame > 7 ? 4 : (frame + 1 > 7 ? 4 : frame + 1));
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (isTransformed && (target.friendly || target.townNPC)) return true;
            return base.CanHitNPC(target);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (isTransformed)
            {
                if (target.friendly || target.townNPC) modifiers.SetMaxDamage(transformedFriendlyMaxDamage);
                else modifiers.FinalDamage *= transformedEnemyDamageMult;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (isTransformed && Main.netMode == NetmodeID.SinglePlayer)
            {
                ApplyTransformEffectsOnNPC(target);
            }
        }

        // El stand NUNCA daña jugadores (tengan PvP o no). La curacion se hace en PlayerHealCheck().
        public override bool CanHitPlayer(Player target)
        {
            return false;
        }

        public override bool CanHitPvp(Player target)
        {
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player p = Main.player[Projectile.owner];
            var data = ParticulasStands.Stands.CrazyDiamond3;
            bool spawning = runtime.spawning;

            Texture2D tex;
            Rectangle r;
            Vector2 o;

            if (spawning)
            {
                tex = ModContent.Request<Texture2D>(data.SpawnTexture).Value;
                r = new Rectangle(runtime.frame * data.FrameWidth, 0, data.FrameWidth, data.SpawnHeight);
                o = new Vector2(data.FrameWidth / 2f, data.SpawnHeight / 2f);
                lightColor = Color.White;
            }
            else
            {
                if (isTransformed)
                {
                    // Textura de la Tier 3 transformada (ajusta la ruta si es distinta)
                    tex = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/CrazyDiamond/CrazyDiamond_Tier_3/Transformation_Tier_3").Value;
                }
                else
                {
                    tex = ModContent.Request<Texture2D>(data.IdleTexture).Value;
                }

                r = new Rectangle(frame * 88, 0, 88, 88);
                o = new Vector2(44, 49);
            }

            bool flipVisual = syncOffX < 0;

            Main.EntitySpriteDraw(
                tex,
                Projectile.Center - Main.screenPosition,
                r,
                lightColor * ((255 - Projectile.alpha) / 255f),
                Projectile.rotation,
                o,
                Projectile.scale,
                flipVisual ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                0f
            );

            return false;
        }
    }
}