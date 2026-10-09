using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.Localization;
using System;
using System.IO;
using Jojo.Content.Buffs;
using Jojo.Systems.StandFuncionComunes;
using Jojo.Content.Buffs.KillerQueen_Buffs;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_4.BitesTheDust;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Items;
using Jojo.Content.Items.Potenciadores;
using Jojo.Content.UI;

namespace Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_4
{
    public class KILLERQUEENSTAND_Tier_4 : ModProjectile
    {
        enum State { Idle, Attack }
        State state;
        bool auto, init, dying;
        NPC target;
        int frame, animT, cd, attackTimer, swingSoundTimer;
        int bombAnimFrame, bombAnimTimer;
        bool bombAnim, forcedIdle;
        bool fDash, shotFired, dashReturning;
        int dashT, strayCatFrame, strayCatTimer;
        Vector2 dashDir, bombPosCache;
        float manR = 150f, autoR = 150f, bombR = 360f, innerRadius = 90f;
        ParticulasStands.StandRuntime runtime = new();
        public int baseCritChance = 5;
        public int attackSpeed = 1;
        Projectile bombaActual;

        // Temporizador de espera para el ciclo de la bomba (igual sistema que Tier 1,
        // permite mantener pulsado el click derecho para colocar/detonar en bucle)
        int bombCooldown = 0;

        int strayCatProjId = -1;

        public bool isBitesTheDustMini = false;
        public int btdHostNPC = -1;

        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");
        static readonly SoundStyle SwingSoundBase = new("Terraria/Sounds/Item_1");
        static readonly SoundStyle BombPlaceSound = new("Jojo/Content/Sonidos/KillerQueenBomba1");
        static readonly SoundStyle StrayCatSound = new("Jojo/Content/Sonidos/StrayCat");
        static readonly SoundStyle SHASound = new("Jojo/Content/Sonidos/KillerQueenBomba_SHA");
        static readonly SoundStyle BTDPressSound = new("Jojo/Content/Sonidos/KillerQueenBomba_PresionasBiteTheDust");

        float syncOffX, syncOffY;
        int netTimer = 0;
        bool prevHasSHA = false;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)state); writer.Write(bombAnim); writer.Write(bombAnimFrame);
            writer.Write(forcedIdle); writer.Write(fDash); writer.Write(dashReturning);
            writer.Write(strayCatFrame); writer.Write(dashDir.X); writer.Write(dashDir.Y);
            writer.Write(syncOffX); writer.Write(syncOffY);
            writer.Write(isBitesTheDustMini); writer.Write(btdHostNPC);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            state = (State)reader.ReadByte(); bombAnim = reader.ReadBoolean(); bombAnimFrame = reader.ReadInt32();
            forcedIdle = reader.ReadBoolean(); fDash = reader.ReadBoolean(); dashReturning = reader.ReadBoolean();
            strayCatFrame = reader.ReadInt32(); dashDir = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            syncOffX = reader.ReadSingle(); syncOffY = reader.ReadSingle();
            isBitesTheDustMini = reader.ReadBoolean(); btdHostNPC = reader.ReadInt32();
        }

        void ClearBomb()
        {
            if (bombaActual != null && bombaActual.active) bombaActual.Kill();
            bombaActual = null;
        }

        void StartBombAnimation(Player p, Vector2 bombPos)
        {
            bombAnim = true; bombAnimFrame = 0; bombAnimTimer = 0;
            forcedIdle = true; bombPosCache = bombPos; Projectile.netUpdate = true;
        }

        void UpdateAttackSpeed(Player p)
        {
            float s = p.GetModPlayer<StandStatsPlayer>().standSpeed;
            if (s < 50f) { attackSpeed = 1; Projectile.localNPCHitCooldown = 7; }
            else if (s < 100f) { attackSpeed = 2; Projectile.localNPCHitCooldown = 6; }
            else if (s < 150f) { attackSpeed = 3; Projectile.localNPCHitCooldown = 5; }
            else { attackSpeed = 4; Projectile.localNPCHitCooldown = 4; }
        }

        SoundStyle GetSwingSound(float speed)
        {
            float pitch = speed >= 100f ? 0.05f : (speed >= 50f ? 0.08f : 0f);
            return SwingSoundBase with { Pitch = pitch };
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
            Projectile.usesLocalNPCImmunity = false;
            Projectile.localNPCHitCooldown = 0;
            Projectile.timeLeft = 25;
            SoundEngine.PlaySound(DespawnSound, Projectile.Center);
            ClearBomb();

            Player p = Main.player[Projectile.owner];
            if (p.active)
            {
                p.ClearBuff(ModContent.BuffType<SHA_Buff_Tier_4>());
                BitesTheDustSystem.CancelBTD(p, Projectile, this);
            }
        }

        public override void OnKill(int timeLeft)
        {
            Player p = Main.player[Projectile.owner];
            if (p.active)
            {
                p.ClearBuff(ModContent.BuffType<SHA_Buff_Tier_4>());
                BitesTheDustSystem.CancelBTD(p, Projectile, this);
            }
            ClearBomb();
            SoundEngine.PlaySound(DespawnSound, Projectile.Center);
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            UpdateAttackSpeed(p);

            if (!p.active || p.dead)
            {
                ClearBomb();
                p.ClearBuff(ModContent.BuffType<SHA_Buff_Tier_4>());
                BitesTheDustSystem.CancelBTD(p, Projectile, this);
                Projectile.Kill();
                return;
            }

            bool currentHasSHA = p.HasBuff(ModContent.BuffType<SHA_Buff_Tier_4>());
            if (currentHasSHA && !prevHasSHA)
            {
                SoundEngine.PlaySound(SHASound, p.Center);
            }
            prevHasSHA = currentHasSHA;

            Projectile.timeLeft = 2;

            if (dying)
            {
                ParticulasStands.Despawn(Projectile, p);
                ClearBomb();
                return;
            }

            if (!init)
            {
                init = true;
                Projectile.alpha = 0;
                Projectile.friendly = false;
                Projectile.damage = 0;
                SoundEngine.PlaySound(SpawnSound, Projectile.Center);
                ParticulasStands.Spawn(p, ParticulasStands.Stands.KillerP4);
            }

            var data = ParticulasStands.Stands.KillerP4;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);

            bool spawning = runtime.spawning;
            auto = Projectile.ai[0] == 1f;
            bool isOwner = Projectile.owner == Main.myPlayer;
            BitesTheDustPlayer btdPlayer = p.GetModPlayer<BitesTheDustPlayer>();

            if (isOwner && isBitesTheDustMini)
            {
                if (!btdPlayer.btdActive || btdHostNPC < 0 || !Main.npc[btdHostNPC].active)
                {
                    BitesTheDustSystem.CancelBTD(p, Projectile, this);
                }
                else if (!p.HasBuff(ModContent.BuffType<BitesTheDustBuff>()) || btdPlayer.isExplodingPhase)
                {
                    if (!btdPlayer.isExplodingPhase)
                    {
                        btdPlayer.ActivateBitesTheDust();
                    }
                    isBitesTheDustMini = false;
                    btdHostNPC = -1;
                    Projectile.netUpdate = true;
                }
            }

            if (isBitesTheDustMini) Projectile.scale = MathHelper.Lerp(Projectile.scale, 0.33f, 0.15f);
            else Projectile.scale = MathHelper.Lerp(Projectile.scale, 1f, 0.2f);

            if (!spawning && isOwner)
            {
                if (!isBitesTheDustMini)
                {
                    HandleToggle(p);
                    HandleSkills(p);
                    HandleBombAbility(p);
                    UpdateDash(p);
                }
                else
                {
                    if (JojoKeybinds.SkillH.JustPressed && !btdPlayer.isExplodingPhase)
                    {
                        btdPlayer.ActivateBitesTheDust();
                        isBitesTheDustMini = false;
                        btdHostNPC = -1;
                        Projectile.netUpdate = true;
                    }
                }
            }

            if (!ParticulasStands.CanAttack(runtime)) ClearBomb();

            if (isBitesTheDustMini && btdHostNPC >= 0 && Main.npc[btdHostNPC].active)
            {
                NPC host = Main.npc[btdHostNPC];
                state = State.Idle;
                Projectile.friendly = false;
                Projectile.rotation = 0f;
                Vector2 npcOffset = new Vector2((host.width / 2f) + 10f, -host.height / 3f);
                BitesTheDustSystem.FollowNPC(Projectile, host, npcOffset);

                // FIX: antes se ponía syncOffX/syncOffY a 0, lo que dejaba al flip
                // sin ningún dato válido. Ahora guardamos el signo real del offset
                // usado para seguir al NPC, igual que en el resto de estados.
                syncOffX = npcOffset.X;
                syncOffY = npcOffset.Y;
            }
            else
            {
                if (isOwner)
                {
                    bool canAttack = !fDash && ParticulasStands.CanAttack(runtime) && (auto ? (target = FindEnemy(p)) != null : Main.mouseLeft);
                    state = canAttack ? State.Attack : State.Idle;
                    Projectile.friendly = canAttack;
                    Vector2 off = GetOffset(p);
                    float multRango = 1f;
                    if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer)) multRango = rangoPlayer.multiplicadorRango;
                    float max = (auto ? autoR : manR) * multRango;
                    if (off.Length() > max) off = Vector2.Normalize(off) * max;
                    syncOffX = off.X; syncOffY = off.Y;
                    if ((state == State.Attack || fDash) && !bombAnim)
                    {
                        Vector2 rotDir = fDash ? dashDir : off;
                        if (rotDir.Length() <= innerRadius && !fDash) Projectile.rotation = 0f;
                        else
                        {
                            float rot = rotDir.ToRotation();
                            if (rotDir.X < 0) rot += MathHelper.Pi;
                            Projectile.rotation = rot;
                        }
                    }
                    else Projectile.rotation = 0f;
                    if (state == State.Attack && !spawning && !bombAnim)
                    {
                        BarrageSystem.SpawnPunches(Projectile, p, off);
                        float s = p.GetModPlayer<StandStatsPlayer>().standSpeed;
                        int swingDelay = s >= 100f ? 6 : s >= 50f ? 8 : 11;
                        swingSoundTimer++;
                        if (swingSoundTimer >= swingDelay) { SoundEngine.PlaySound(GetSwingSound(s), Projectile.Center); swingSoundTimer = 0; }
                        if (attackSpeed > 0)
                        {
                            attackTimer++;
                            int delay = attackSpeed <= 0 ? 60 : 60 / attackSpeed;
                            if (delay < 1) delay = 1;
                            if (attackTimer >= delay)
                            {
                                attackTimer = 0; Projectile.friendly = true;
                                Rectangle hitbox = Projectile.Hitbox;
                                foreach (NPC npc in Main.npc)
                                {
                                    if (!npc.active || npc.life <= 0 || npc.friendly || npc.dontTakeDamage) continue;
                                    if (!hitbox.Intersects(npc.Hitbox)) continue;
                                    if (npc.immune[Projectile.owner] > 0) continue;
                                    NPC.HitInfo hit = new() { Damage = Projectile.damage, Knockback = 0f, HitDirection = p.direction, Crit = Main.rand.Next(100) < Projectile.CritChance };
                                    npc.StrikeNPC(hit);
                                    npc.immune[Projectile.owner] = Projectile.localNPCHitCooldown;
                                }
                            }
                        }
                    }
                    else swingSoundTimer = 0;
                    ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);
                }
                else
                {
                    Vector2 off = new Vector2(syncOffX, syncOffY);
                    if ((state == State.Attack || fDash) && !bombAnim)
                    {
                        Vector2 rotDir = fDash ? dashDir : off;
                        if (rotDir.Length() <= innerRadius && !fDash) Projectile.rotation = 0f;
                        else
                        {
                            float rot = rotDir.ToRotation();
                            if (rotDir.X < 0) rot += MathHelper.Pi;
                            Projectile.rotation = rot;
                        }
                    }
                    else Projectile.rotation = 0f;
                    ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);
                }
            }

            if (isOwner)
            {
                if (++netTimer >= 6)
                {
                    netTimer = 0;
                    Projectile.netUpdate = true;
                }
            }

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);
            float baseDamage = 50f; //DAÑO GOLPES BARAGE
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);
            Animate(p);
        }

        void UpdateDash(Player p)
        {
            if (!fDash) return;

            if (dashT == 20)
            {
                SoundEngine.PlaySound(StrayCatSound, p.Center);
            }

            dashT++; strayCatTimer++;
            if (strayCatTimer >= 5)
            {
                strayCatTimer = 0;
                if (!dashReturning)
                {
                    if (strayCatFrame < 6) strayCatFrame++;
                    if (strayCatFrame == 5 && !shotFired)
                    {
                        shotFired = true;
                        int spawnedId = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + dashDir * 20f, dashDir * 6f, ModContent.ProjectileType<StrayCat_BOMBA_Tier_4>(), Projectile.damage, 2f, p.whoAmI);
                        strayCatProjId = spawnedId;
                    }
                    if (strayCatFrame >= 6) { dashReturning = true; Projectile.netUpdate = true; }
                }
                else
                {
                    if (strayCatFrame > 0) strayCatFrame--;
                }
            }
            if (dashReturning && strayCatFrame <= 0)
            {
                fDash = false; dashReturning = false; dashT = 0;
                strayCatFrame = 0; strayCatTimer = 0; shotFired = false;
                Projectile.rotation = 0f; Projectile.netUpdate = true;
            }
        }

        void HandleToggle(Player p)
        {
            if (cd-- > 0 || p.whoAmI != Main.myPlayer) return;
            if (JojoKeybinds.ToggleAuto.JustPressed)
            {
                auto = !auto; Projectile.ai[0] = auto ? 1f : 0f;
                cd = 30; target = null;
                if (!auto) ClearBomb();
                Projectile.netUpdate = true;
                Main.NewText(auto ? "AutoStand-ON" : "AutoStand-OFF", 255, 255, 0);
            }
        }

        bool TryGetActiveStrayCat(Player p, out StrayCat_BOMBA_Tier_4 strayCat)
        {
            strayCat = null;

            if (strayCatProjId < 0 || strayCatProjId >= Main.projectile.Length)
                return false;

            Projectile proj = Main.projectile[strayCatProjId];

            if (!proj.active || proj.owner != p.whoAmI || proj.type != ModContent.ProjectileType<StrayCat_BOMBA_Tier_4>())
                return false;

            if (proj.ModProjectile is not StrayCat_BOMBA_Tier_4 sc)
                return false;

            strayCat = sc;
            return true;
        }

        void HandleSkills(Player p)
        {
            if (p.whoAmI != Main.myPlayer) return;
            StandStatsPlayer stats = p.GetModPlayer<StandStatsPlayer>();
            BitesTheDustPlayer btdPlayer = p.GetModPlayer<BitesTheDustPlayer>();
            if (p.HasBuff(ModContent.BuffType<CD>())) return;
            if (JojoKeybinds.SkillF.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown1>()))
            {
                float factorTiempoF = Math.Max(0f, 1f - stats.standCooldown1Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown1>(), (int)(2000 * factorTiempoF));
                p.AddBuff(ModContent.BuffType<CD>(), 120);
                p.AddBuff(ModContent.BuffType<SHA_Buff_Tier_4>(), 24 * 60);
            }
            if (JojoKeybinds.SkillG.JustPressed)
            {
                if (TryGetActiveStrayCat(p, out StrayCat_BOMBA_Tier_4 activeStrayCat))
                {
                    activeStrayCat.ForceDetonate();
                    strayCatProjId = -1;
                }
                else if (!p.HasBuff(ModContent.BuffType<Cooldown2>()))
                {
                    float factorTiempoG = Math.Max(0f, 1f - stats.standCooldown2Reduction);
                    p.AddBuff(ModContent.BuffType<Cooldown2>(), (int)(720 * factorTiempoG));
                    p.AddBuff(ModContent.BuffType<CD>(), 0);
                    Vector2 dir = Vector2.Normalize(Main.MouseWorld - Projectile.Center);
                    if (dir == Vector2.Zero) dir = new Vector2(1, 0);
                    dashDir = dir; fDash = true; dashReturning = false;
                    dashT = 0; strayCatFrame = 0; strayCatTimer = 0; shotFired = false;
                    strayCatProjId = -1;
                    Projectile.netUpdate = true;
                }
            }
            if (JojoKeybinds.SkillH.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown3>()) && !btdPlayer.isExplodingPhase)
            {
                NPC btdTarget = BitesTheDustSystem.FindBTDTarget(p, 1000f);
                if (btdTarget != null)
                {
                    SoundEngine.PlaySound(BTDPressSound, p.Center); // EFECTO DE SONIDO AÑADIDO AQUÍ
                    float factorTiempoH = Math.Max(0f, 1f - stats.standCooldown3Reduction);
                    p.AddBuff(ModContent.BuffType<Cooldown3>(), (int)(3600 * factorTiempoH)); //7200
                    p.AddBuff(ModContent.BuffType<CD>(), 0);
                    p.AddBuff(ModContent.BuffType<BitesTheDustBuff>(), 600);
                    btdPlayer.SaveState();
                    isBitesTheDustMini = true;
                    btdHostNPC = btdTarget.whoAmI;
                    ClearBomb();
                    Projectile.netUpdate = true;
                }
                // EL BLOQUE ELSE QUE LANZABA EL MENSAJE EN CHAT HA SIDO ELIMINADO
            }
        }

        void HandleBombAbility(Player p)
        {
            if (p.whoAmI != Main.myPlayer) return;

            if (bombCooldown > 0) bombCooldown--;

            if (!Main.mouseRight || bombCooldown > 0) return;

            if (bombaActual != null && bombaActual.active)
            {
                Vector2 bombPos = bombaActual.Center;

                if (bombaActual.ModProjectile is BOMBA1_Tier_4 bomba) bomba.Explode(p);

                bombaActual = null;
                StartBombAnimation(p, bombPos);

                // Cooldown tras detonar, antes de poder colocar la siguiente
                bombCooldown = 16;
                return;
            }

            Vector2 mouse = Main.MouseWorld;
            float currentBombR = bombR;

            if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
            {
                currentBombR *= rangoPlayer.multiplicadorRango;
            }

            foreach (NPC npc in Main.npc)
            {
                if (!npc.active || npc.life <= 0) continue;

                if (npc.Hitbox.Contains(mouse.ToPoint()))
                {
                    if (Vector2.Distance(npc.Center, p.Center) > currentBombR) return;

                    int projNPC = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        npc.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<BOMBA1_Tier_4>(),
                        0,
                        0f,
                        p.whoAmI
                    );

                    bombaActual = Main.projectile[projNPC];
                    if (bombaActual.ModProjectile is BOMBA1_Tier_4 b) b.AttachToNPC(npc);

                    SoundEngine.PlaySound(BombPlaceSound, p.Center);

                    bombCooldown = 6;
                    return;
                }
            }

            int tileX = (int)(mouse.X / 16f);
            int tileY = (int)(mouse.Y / 16f);
            Tile tile = Framing.GetTileSafely(tileX, tileY);

            if (!tile.HasTile || !Main.tileSolid[tile.TileType]) return;

            Vector2 centerBlock = new(tileX * 16 + 8, tileY * 16 + 8);

            if (Vector2.Distance(centerBlock, p.Center) > currentBombR) return;

            int proj = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                centerBlock,
                Vector2.Zero,
                ModContent.ProjectileType<BOMBA1_Tier_4>(),
                0,
                0f,
                p.whoAmI
            );

            bombaActual = Main.projectile[proj];
            SoundEngine.PlaySound(BombPlaceSound, p.Center);

            bombCooldown = 6;
        }

        void Animate(Player p)
        {
            if (bombAnim)
            {
                if (bombAnimTimer == 0 && bombAnimFrame == 0)
                {
                    SoundEngine.PlaySound(BombPlaceSound, p.Center);
                }

                bombAnimTimer++;
                if (bombAnimTimer >= 8)
                {
                    bombAnimTimer = 0; bombAnimFrame++;
                    if (bombAnimFrame >= 5) { bombAnim = false; bombAnimFrame = 0; forcedIdle = false; Projectile.netUpdate = true; }
                }
                return;
            }
            if (fDash) return;
            if (++animT < (state == State.Attack ? 4 : 8)) return;
            animT = 0;
            if (state == State.Idle) { frame++; if (frame > 3) frame = 0; }
            else { frame++; if (frame < 4 || frame > 7) frame = 4; }
        }

        const float dashRadius = 60f;

        Vector2 GetOffset(Player p)
        {
            if (fDash) return dashDir * dashRadius;
            if (forcedIdle) return new Vector2(-40 * p.direction, -10);
            if (state == State.Attack) return auto && target != null ? target.Center - p.Center : Main.MouseWorld - p.Center;
            return new Vector2(-40 * p.direction, -10);
        }

        // Auto-apuntado: solo hostiles (ignora pacíficos, critters, town NPCs y dummies).
        // Aplica el multiplicador de rango internamente.
        NPC FindEnemy(Player p)
        {
            return StandTargeting.FindHostileEnemy(p, autoR);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player p = Main.player[Projectile.owner];
            var data = ParticulasStands.Stands.KillerP4;
            bool spawning = runtime.spawning;

            bool megumin = StandSlotSystem.HasMeguminSkinFor(p);
            bool isRed = StandSlotSystem.HasKillerQueenRedSkinFor(p);
            bool isBlue = StandSlotSystem.HasKillerQueenBlueSkinFor(p);
            bool isGreen = StandSlotSystem.HasKillerQueenGreenSkinFor(p);

            Texture2D tex; Rectangle r; Vector2 o;

            if (bombAnim)
            {
                string path = "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_4/KILLERQUEENSTAND_Tier_4_Bomba1";
                if (megumin) path = "Jojo/Content/Projectiles/Skins/KillerQueen/Megumin/KILLERQUEENSTAND_Megumin_Bomba1";
                else if (isRed) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Red/KILLERQUEENSTAND_Red_Bomba1";
                else if (isBlue) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Blue/KILLERQUEENSTAND_Blue_Bomba1";
                else if (isGreen) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Green/KILLERQUEENSTAND_Green_Bomba1";

                tex = ModContent.Request<Texture2D>(path).Value;
                r = new Rectangle(bombAnimFrame * 88, 0, 88, 76); o = new Vector2(44, 36);
            }
            else if (fDash)
            {
                string path = "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_4/StrayCat_Tier_4";
                if (megumin) path = "Jojo/Content/Projectiles/Skins/KillerQueen/Megumin/StrayCat_Megumin";
                else if (isRed) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Red/StrayCat_Red";
                else if (isBlue) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Blue/StrayCat_Blue";
                else if (isGreen) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Green/StrayCat_Green";

                tex = ModContent.Request<Texture2D>(path).Value;
                r = new Rectangle(strayCatFrame * 88, 0, 88, 80); o = new Vector2(44, 40);
            }
            else if (spawning)
            {
                string path = data.SpawnTexture;
                if (megumin) path = "Jojo/Content/Projectiles/Skins/KillerQueen/Megumin/KQ_SPAWN_Megumin";
                else if (isRed) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Red/KQ_SPAWN_Red";
                else if (isBlue) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Blue/KQ_SPAWN_Blue";
                else if (isGreen) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Green/KQ_SPAWN_Green";

                tex = ModContent.Request<Texture2D>(path).Value;
                r = new Rectangle(runtime.frame * data.FrameWidth, 0, data.FrameWidth, data.SpawnHeight); o = new Vector2(data.FrameWidth / 2f, data.SpawnHeight / 2f);
            }
            else
            {
                string path = "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_4/KILLERQUEENSTAND_Tier_4";
                if (megumin) path = "Jojo/Content/Projectiles/Skins/KillerQueen/Megumin/KILLERQUEENSTAND_Megumin";
                else if (isRed) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Red/KILLERQUEENSTAND_Red";
                else if (isBlue) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Blue/KILLERQUEENSTAND_Blue";
                else if (isGreen) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Green/KILLERQUEENSTAND_Green";

                tex = ModContent.Request<Texture2D>(path).Value;
                r = new Rectangle(frame * 88, 0, 88, 76); o = new Vector2(44, 36);
            }

            // FIX: usamos el offset sincronizado (syncOffX), congelado/sin-lerp,
            // igual que Tier 1, en vez de comparar la posición física en vivo del
            // proyectil contra el jugador (o el NPC en el modo mini de Bites The
            // Dust). Esa comparación en vivo se desincronizaba con el lerp de
            // FollowPlayer/FollowNPC y producía el giro al correr en el borde.
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