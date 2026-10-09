using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using System;
using System.IO;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.WhiteSnake_Buffs;
using Jojo.Content.Habilidades;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Items;
using Jojo.Content.Items.Potenciadores;
using Jojo.Systems.StandFuncionComunes;

namespace Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_3
{
    public class WHITESNAKESTAND_Tier_3 : ModProjectile
    {
        // ═══════════════ 1) ÁCIDO (EL DEBUFF) - TIER 3 ═══════════════
        // Daño por segundo = ACIDO_DANO_POR_GOLPE x ACIDO_GOLPES_POR_SEGUNDO
        public const int ACIDO_DURACION = 550;   // ticks que dura el ácido (60 = 1 s)
        public const int ACIDO_DANO_POR_GOLPE = 10;    // daño REAL de cada golpe (ignora defensa)
        public const int ACIDO_GOLPES_POR_SEGUNDO = 8;     // cuántos golpes pequeños por segundo
        public const float ACIDO_SLOW = 0.40f;  // 1.0 = enemigo parado; 0.4 = 40% más lento

        // ═══════════════ 2) GOTA Y RASTRO (EL PROYECTIL) - TIER 3 ═══════════════
        // Daño base de la gota del vómito y de los rastros (suelo y pared). Se mejora con tus buffs de Stand.
        public const float DANO_GOTA_Y_RASTRO = 10f;
        // ═════════════════════════════════════════════════════════════════════

        enum State { Idle, Attack, Extracting }
        State state;

        bool auto, init, dying, fDash, grabAnimStarted;
        NPC target;
        int extractTargetIndex = -1;

        int frame, animT, cd, dashT;
        int attackTimer, swingSoundTimer;

        byte extractFrame;

        float manR = 250f, autoR = 250f;
        float innerRadius = 90f;

        ParticulasStands.StandRuntime runtime = new();

        public int baseCritChance = 5;
        public int attackSpeed = 1;

        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");
        static readonly SoundStyle SwingSoundBase = new("Terraria/Sounds/Item_1");

        float syncOffX, syncOffY;
        int netTimer = 0;

        const float VelocidadAcercamientoCliente = 20f;

        // --- VARIABLES DE ILUMINACIÓN Y PARTÍCULAS ---
        Vector2 lastCenter = Vector2.Zero;

        // --- VARIABLES DE LA HABILIDAD DE VÓMITO ---
        bool isVomiting;
        int vomitTimer;
        int vomitFrame;
        Vector2 vomitAimDir;

        // --- VARIABLES DE LA HABILIDAD DE PISTOLA (tecla G) ---
        bool isShooting;
        int shootFrame;
        int shootAnimT;
        int shootRecoilStep;
        int shootRecoilTimer;
        Vector2 shootAimDir;
        bool prevMouseLeft;
        float shootKickback;

        public bool IsPistolaActive => isShooting;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)state);
            writer.Write(auto);
            writer.Write(fDash);
            writer.Write(syncOffX);
            writer.Write(syncOffY);

            writer.Write(extractFrame);

            writer.Write(grabAnimStarted);
            writer.Write((short)extractTargetIndex);

            writer.Write(isVomiting);
            writer.Write(vomitAimDir.X);
            writer.Write(vomitAimDir.Y);
            writer.Write((byte)vomitFrame);

            writer.Write(isShooting);
            writer.Write((byte)shootFrame);
            writer.Write((byte)shootRecoilStep);
            writer.Write(shootAimDir.X);
            writer.Write(shootAimDir.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            state = (State)reader.ReadByte();
            auto = reader.ReadBoolean();
            fDash = reader.ReadBoolean();
            syncOffX = reader.ReadSingle();
            syncOffY = reader.ReadSingle();

            extractFrame = reader.ReadByte();

            grabAnimStarted = reader.ReadBoolean();
            extractTargetIndex = reader.ReadInt16();

            isVomiting = reader.ReadBoolean();
            vomitAimDir = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            vomitFrame = reader.ReadByte();

            isShooting = reader.ReadBoolean();
            shootFrame = reader.ReadByte();
            shootRecoilStep = reader.ReadByte();
            shootAimDir = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        public override void SetDefaults()
        {
            Projectile.width = 88;
            Projectile.height = 76;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;

            // PENETRACIÓN DE ARMADURA AÑADIDA AQUÍ
            Projectile.ArmorPenetration = 1000; // Ajusta este valor si necesitas más o menos penetración
        }

        public void StartDying()
        {
            if (dying) return;
            dying = true;
            Projectile.friendly = false;
            Projectile.timeLeft = 25;
            SoundEngine.PlaySound(DespawnSound, Projectile.Center);
        }

        void UpdateAttackSpeed(Player p)
        {
            float s = p.GetModPlayer<StandStatsPlayer>().standSpeed;
            if (s < 50f) { attackSpeed = 1; Projectile.localNPCHitCooldown = 7; }
            else if (s < 100f) { attackSpeed = 2; Projectile.localNPCHitCooldown = 6; }
            else if (s < 150f) { attackSpeed = 3; Projectile.localNPCHitCooldown = 5; }
            else { attackSpeed = 4; Projectile.localNPCHitCooldown = 4; }
        }

        int GetAttackDelay() => attackSpeed switch { 1 => 5, 2 => 4, 3 => 3, 4 => 2, _ => 5 };
        SoundStyle GetSwingSound(float speed) => SwingSoundBase with { Pitch = speed >= 100f ? 0.05f : speed >= 50f ? 0.08f : 0f };
        int GetSwingDelay(float speed) => speed >= 100f ? 6 : speed >= 50f ? 8 : 11;

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            UpdateAttackSpeed(p);

            if (dying) { ParticulasStands.Despawn(Projectile, p); return; }
            if (!p.active || p.dead) { StartDying(); return; }

            Projectile.timeLeft = 2;

            if (!init)
            {
                init = true;
                Projectile.alpha = 0;
                Projectile.friendly = false;
                Projectile.damage = 0;
                SoundEngine.PlaySound(SpawnSound, Projectile.Center);
                ParticulasStands.Spawn(p, ParticulasStands.Stands.WhiteSnakeParticulas3);
            }

            var data = ParticulasStands.Stands.WhiteSnakeParticulas3;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);

            bool spawning = runtime.spawning;
            bool isOwner = Projectile.owner == Main.myPlayer;

            // --- SISTEMA DE ILUMINACIÓN Y PARTÍCULAS ---
            if (!spawning)
            {
                // 1. Luz Ambiental
                Lighting.AddLight(Projectile.Center, 90f / 255f, 89f / 255f, 164f / 255f);

                // 2. Generación de Aura (12.5% de probabilidad por tick)
                if (Main.rand.NextBool(8))
                {
                    Vector2 spawnOffset = new Vector2(Main.rand.NextFloat(-18f, 18f), Main.rand.NextFloat(-24f, 20f));
                    Dust d = Dust.NewDustPerfect(Projectile.Center + spawnOffset, 267, new Vector2(0, -Main.rand.NextFloat(0.2f, 0.6f)));
                    d.noGravity = true;
                    d.scale = Main.rand.NextFloat(0.7f, 1.0f);
                    d.customData = Projectile.whoAmI;

                    int colorIndex = Main.rand.Next(3);
                    d.color = colorIndex switch
                    {
                        0 => new Color(57, 76, 131),   // #394C83
                        1 => new Color(90, 89, 164),   // #5A59A4
                        _ => new Color(45, 55, 115)    // #2D3773
                    };
                }

                // 3. Sistema de Arrastre de Partículas
                if (lastCenter != Vector2.Zero)
                {
                    Vector2 delta = Projectile.Center - lastCenter;
                    if (delta != Vector2.Zero)
                    {
                        for (int i = 0; i < Main.maxDust; i++)
                        {
                            Dust d = Main.dust[i];
                            if (d.active && d.type == 267 && d.customData is int id && id == Projectile.whoAmI)
                            {
                                d.position += delta;
                            }
                        }
                    }
                }
                lastCenter = Projectile.Center;
            }

            if (!spawning && isOwner)
            {
                HandleSkills(p);
                HandleToggle(p);
                HandlePistolaToggle(p);
            }

            auto = Projectile.ai[0] == 1f;
            if (!spawning) UpdateSkills(p);

            if (isOwner)
            {
                float multRango = 1f;
                if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
                {
                    multRango = rangoPlayer.multiplicadorRango;
                }

                float baseMax = (auto ? autoR : manR) * multRango;
                float extractMax = baseMax * 1.5f;

                // --- LÓGICA DE VÓMITO ---
                if (isVomiting)
                {
                    state = State.Idle;
                    extractTargetIndex = -1;
                    grabAnimStarted = false;
                    Projectile.friendly = false;

                    Vector2 dirHaciaMouse = Main.MouseWorld - p.Center;
                    if (dirHaciaMouse == Vector2.Zero) dirHaciaMouse = new Vector2(p.direction, 0);
                    dirHaciaMouse.Normalize();

                    Vector2 offsetVomito = dirHaciaMouse * 60f;

                    syncOffX = offsetVomito.X;
                    syncOffY = offsetVomito.Y;

                    float rot = dirHaciaMouse.ToRotation();
                    if (dirHaciaMouse.X < 0) rot += MathHelper.Pi;
                    Projectile.rotation = rot;

                    ParticulasStands.FollowPlayer(Projectile, p, offsetVomito, 0.4f);

                    WhitesnakeVomitSystem_Tier_3.UpdateVomitAttack(p, Projectile, ref vomitTimer, ref vomitFrame, out vomitAimDir);

                    if (vomitTimer >= 90)
                    {
                        isVomiting = false;
                        vomitTimer = 0;
                        vomitFrame = 0;
                        Projectile.rotation = 0f;
                        Projectile.netUpdate = true;
                    }
                }
                else if (isShooting)
                {
                    state = State.Idle;
                    extractTargetIndex = -1;
                    grabAnimStarted = false;
                    Projectile.friendly = false;

                    Vector2 dirPistola = Main.MouseWorld - p.Center;
                    if (dirPistola == Vector2.Zero) dirPistola = new Vector2(p.direction, 0);
                    dirPistola.Normalize();

                    float pistolaDist = MathHelper.Clamp(60f - shootKickback, 20f, 60f);
                    Vector2 offsetPistola = dirPistola * pistolaDist;
                    syncOffX = offsetPistola.X;
                    syncOffY = offsetPistola.Y;
                    shootAimDir = dirPistola;

                    float rotPistola = dirPistola.ToRotation();
                    if (dirPistola.X < 0) rotPistola += MathHelper.Pi;
                    Projectile.rotation = rotPistola;

                    ParticulasStands.FollowPlayer(Projectile, p, offsetPistola, 0.4f);

                    bool clickPressed = Main.mouseLeft && !prevMouseLeft;
                    prevMouseLeft = Main.mouseLeft;

                    WhitesnakePistolaSystem_Tier_3.UpdatePistolaAttack(
                        p, Projectile, clickPressed,
                        ref shootFrame, ref shootAnimT, ref shootRecoilStep, ref shootRecoilTimer,
                        ref shootKickback,
                        dirPistola);
                }
                else
                {
                    // --- SISTEMA DE EXTRACCIÓN (CLIC DERECHO) ---
                    if (state == State.Extracting)
                    {
                        NPC extTarget = (extractTargetIndex >= 0 && extractTargetIndex < Main.maxNPCs) ? Main.npc[extractTargetIndex] : null;

                        var estadoExtr = ExtraccionLogica.Actualizar(Projectile, p, ref extTarget, extractMax, grabAnimStarted);

                        if (estadoExtr == ExtraccionLogica.EstadoExtraccion.Cancelado)
                        {
                            state = State.Idle;
                            extractTargetIndex = -1;
                            grabAnimStarted = false;
                            extractFrame = 0;
                            Projectile.netUpdate = true;
                        }
                        else if (estadoExtr == ExtraccionLogica.EstadoExtraccion.Extrayendo)
                        {
                            extractTargetIndex = extTarget.whoAmI;

                            if (!grabAnimStarted)
                            {
                                grabAnimStarted = true;
                                extractFrame = 0;
                                animT = 0;
                                Projectile.netUpdate = true;
                            }

                            animT++;
                            if (animT >= 12)
                            {
                                animT = 0;
                                extractFrame++;
                                if (extractFrame > 5)
                                {
                                    if (extTarget != null && extTarget.active)
                                    {
                                        ExtraccionLogica.AplicarAmnesia(extTarget);
                                    }

                                    state = State.Idle;
                                    extractTargetIndex = -1;
                                    grabAnimStarted = false;
                                    extractFrame = 0;
                                    Projectile.netUpdate = true;
                                }
                            }
                        }
                        else if (estadoExtr == ExtraccionLogica.EstadoExtraccion.Acercandose)
                        {
                            if (extractTargetIndex != extTarget.whoAmI)
                                Projectile.netUpdate = true;

                            extractTargetIndex = extTarget.whoAmI;
                            grabAnimStarted = false;

                            if (extractFrame > 3) extractFrame = 0;

                            if (++animT >= 9)
                            {
                                animT = 0;
                                extractFrame = (byte)((extractFrame + 1) % 4);
                            }
                        }
                        else // BUSCANDO
                        {
                            extractTargetIndex = -1;
                            grabAnimStarted = false;

                            if (extractFrame > 3) extractFrame = 0;

                            if (++animT >= 9)
                            {
                                animT = 0;
                                extractFrame = (byte)((extractFrame + 1) % 4);
                            }
                        }

                        if (state == State.Extracting)
                        {
                            if (extractTargetIndex == -1)
                            {
                                Vector2 targetOff = Main.MouseWorld - p.Center;
                                if (targetOff.Length() > extractMax)
                                    targetOff = Vector2.Normalize(targetOff) * extractMax;

                                syncOffX = targetOff.X;
                                syncOffY = targetOff.Y;

                                ParticulasStands.FollowPlayer(Projectile, p, new Vector2(syncOffX, syncOffY), 0.12f);
                            }
                            else if (extTarget != null)
                            {
                                syncOffX = extTarget.Center.X - p.Center.X;
                                syncOffY = extTarget.Center.Y - p.Center.Y;
                            }

                            Projectile.friendly = false;
                            Projectile.rotation = 0f;
                        }
                    }

                    // --- SISTEMA NORMAL (ATAQUE Y REPOSO) ---
                    if (state != State.Extracting)
                    {
                        if (Main.mouseRight && !Main.mouseLeft && ParticulasStands.CanAttack(runtime))
                        {
                            state = State.Extracting;
                            extractFrame = 0;
                            animT = 0;
                            extractTargetIndex = -1;
                            Projectile.netUpdate = true;
                        }
                        else
                        {
                            bool canAttack = ParticulasStands.CanAttack(runtime) && (auto ? (target = FindEnemy(p)) != null : Main.mouseLeft);

                            state = canAttack ? State.Attack : State.Idle;
                            Projectile.friendly = canAttack;

                            Vector2 off = GetOffset(p);
                            if (off.Length() > baseMax) off = Vector2.Normalize(off) * baseMax;

                            syncOffX = off.X;
                            syncOffY = off.Y;

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
                            else
                            {
                                Projectile.rotation = 0f;
                            }

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
                                if (attackTimer >= GetAttackDelay())
                                {
                                    attackTimer = 0;
                                    Projectile.friendly = true;
                                }
                            }
                            else
                            {
                                swingSoundTimer = 0;
                            }

                            ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);
                        }
                    }
                }

                if (++netTimer >= 5)
                {
                    netTimer = 0;
                    Projectile.netUpdate = true;
                }
            }
            else // MULTIPLAYER CLIENTE
            {
                Vector2 off = new Vector2(syncOffX, syncOffY);

                if (isVomiting)
                {
                    Projectile.rotation = vomitAimDir.ToRotation();
                    if (vomitAimDir.X < 0) Projectile.rotation += MathHelper.Pi;
                    ParticulasStands.FollowPlayer(Projectile, p, off, 0.12f);
                }
                else if (isShooting)
                {
                    Projectile.rotation = shootAimDir.ToRotation();
                    if (shootAimDir.X < 0) Projectile.rotation += MathHelper.Pi;
                    ParticulasStands.FollowPlayer(Projectile, p, off, 0.4f);
                }
                else
                {
                    if (state != State.Extracting)
                    {
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
                    else
                    {
                        Projectile.rotation = 0f;

                        NPC clientTarget = (extractTargetIndex >= 0 && extractTargetIndex < Main.maxNPCs)
                            ? Main.npc[extractTargetIndex]
                            : null;

                        if (clientTarget != null && clientTarget.active)
                        {
                            if (grabAnimStarted)
                            {
                                Projectile.Center = clientTarget.Center;
                            }
                            else
                            {
                                Vector2 dir = clientTarget.Center - Projectile.Center;
                                float dist = dir.Length();
                                if (dist > 0.01f)
                                {
                                    float avance = MathHelper.Min(VelocidadAcercamientoCliente, dist);
                                    Projectile.Center += dir / dist * avance;
                                }
                            }
                        }
                        else
                        {
                            ParticulasStands.FollowPlayer(Projectile, p, off, 0.15f);
                        }

                        Projectile.rotation = 0f;
                    }
                }
            }

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);
            float baseDamage = 25f; // daño de los PUÑETAZOS del stand (no es el del ácido ni el de la gota)
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);

            if (!spawning && state != State.Extracting && !isVomiting && !isShooting)
            {
                Animate();
            }
        }

        Vector2 GetOffset(Player p)
        {
            if (fDash) return (Main.MouseWorld - p.Center) * 0.35f;

            if (state == State.Attack)
                return auto && target != null ? (target.Center - p.Center) + new Vector2(0, 25f) : Main.MouseWorld - p.Center;

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

                if (auto && isShooting)
                {
                    isShooting = false;
                    shootRecoilStep = 0;
                    shootRecoilTimer = 0;
                    shootFrame = 0;
                    Projectile.rotation = 0f;
                }

                if (Main.netMode != Terraria.ID.NetmodeID.Server)
                    Main.NewText(auto ? "AutoStand-ON" : "AutoStand-OFF", 255, 255, 0);
                Projectile.netUpdate = true;
            }
        }

        void HandlePistolaToggle(Player p)
        {
            if (p.whoAmI != Main.myPlayer) return;
            if (!JojoKeybinds.SkillG.JustPressed) return;

            isShooting = !isShooting;

            if (isShooting)
            {
                shootFrame = 0;
                shootAnimT = 0;
                shootRecoilStep = 0;
                shootRecoilTimer = 0;
                prevMouseLeft = Main.mouseLeft;

                if (isVomiting)
                {
                    isVomiting = false;
                    vomitTimer = 0;
                    vomitFrame = 0;
                    Projectile.rotation = 0f;
                }

                state = State.Idle;
                extractTargetIndex = -1;
                grabAnimStarted = false;
                fDash = false;

                auto = false;
                Projectile.ai[0] = 0f;
            }
            else
            {
                shootRecoilStep = 0;
                shootRecoilTimer = 0;
                shootFrame = 0;
                Projectile.rotation = 0f;
            }

            Projectile.netUpdate = true;
        }

        void HandleSkills(Player p)
        {
            if (p.whoAmI != Main.myPlayer) return;
            if (!ParticulasStands.CanUseSkills(runtime)) return;
            if (p.HasBuff(ModContent.BuffType<CD>())) return;

            StandStatsPlayer stats = p.GetModPlayer<StandStatsPlayer>();

            if (JojoKeybinds.SkillF.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown1>()) && !isVomiting)
            {
                float factorTiempoF = Math.Max(0f, 1f - stats.standCooldown1Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown1>(), (int)(780 * factorTiempoF));
                p.AddBuff(ModContent.BuffType<CD>(), 0);

                fDash = false;

                isVomiting = true;
                vomitTimer = 0;
                vomitFrame = 0;

                isShooting = false;
                shootRecoilStep = 0;
                shootRecoilTimer = 0;
                shootFrame = 0;

                Projectile.netUpdate = true;
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

        public override bool PreDraw(ref Color lightColor)
        {
            Player p = Main.player[Projectile.owner];
            var data = ParticulasStands.Stands.WhiteSnakeParticulas3;
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
            else if (isVomiting)
            {
                tex = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_3/Vomitando_Tier_3").Value;
                r = new Rectangle(vomitFrame * 88, 0, 88, 76);
                o = new Vector2(44, 38);
            }
            else if (isShooting)
            {
                tex = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_3/Disparo_Tier_3").Value;
                r = new Rectangle(shootFrame * 88, 0, 88, 76);
                o = new Vector2(44, 38);
            }
            else if (state == State.Extracting && grabAnimStarted)
            {
                tex = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_3/Extraccion_Tier_3").Value;
                r = new Rectangle(extractFrame * 88, 0, 88, 76);
                o = new Vector2(44, 49);
            }
            else if (state == State.Extracting && !grabAnimStarted)
            {
                tex = ModContent.Request<Texture2D>(data.IdleTexture).Value;
                r = new Rectangle(extractFrame * 88, 0, 88, 76);
                o = new Vector2(44, 49);
            }
            else
            {
                tex = ModContent.Request<Texture2D>(data.IdleTexture).Value;
                r = new Rectangle(frame * 88, 0, 88, 76);
                o = new Vector2(44, 49);
            }

            bool enMitadDerecha = syncOffX >= 0;
            bool flipVisual;

            if (isVomiting)
            {
                flipVisual = vomitAimDir.X < 0;
            }
            else if (isShooting)
            {
                flipVisual = shootAimDir.X < 0;
            }
            else
            {
                flipVisual = state == State.Extracting
                    ? enMitadDerecha
                    : !enMitadDerecha;
            }

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

    public class AmnesiaVisualsNPC_3 : GlobalNPC
    {
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (npc.active && npc.HasBuff(ModContent.BuffType<AmnesiaDebuff>()))
            {
                Texture2D tex = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_3/NoDiscoDebuff").Value;

                Vector2 drawPos = npc.Center - screenPos;
                Vector2 origin = tex.Size() / 2f;

                spriteBatch.Draw(
                    tex,
                    drawPos,
                    null,
                    Color.White,
                    0f,
                    origin,
                    1f,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }
}