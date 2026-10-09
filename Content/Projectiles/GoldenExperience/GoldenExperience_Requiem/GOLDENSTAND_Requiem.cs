using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using System;
using System.IO;
using System.Collections.Generic;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.GoldenExperienceRequiem_Buffs;
using Jojo.Content.Habilidades;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Items;
using Jojo.Content.Items.Potenciadores;
using Jojo.Systems.StandFuncionComunes;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem.Laser;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem.Animales;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem
{
    public class GOLDENSTAND_Requiem : ModProjectile
    {
        enum State { Idle, Attack, LaserState }
        State state;

        bool auto, init, dying, fDash;
        NPC target;

        int frame, animT, cd, dashT;
        int attackTimer, swingSoundTimer;

        int laserTimer = 0;

        // =========================================================================
        // Igual patrón que pedidoMariposas/pedidoTransformSpawn en
        // GOLDENSTAND_Tier_4: la invocación de los animales NO se dispara
        // directo desde HandleSkills (eso corría en cualquier cliente y
        // llamaba a Projectile.NewProjectile, que para el enfoque anterior
        // "funcionaba" porque cada animal era un simple proyectil; ahora que
        // son NPCs reales, SOLO el servidor/singleplayer puede decidir
        // crearlos). En su lugar, HandleSkills solo levanta este flag, que
        // se sincroniza por SendExtraAI/ReceiveExtraAI, y AI() lo consume
        // server-side llamando a AnimalesTierManager_Requiem.
        // =========================================================================
        bool pedidoAnimales;
        int pedidoAnimalesTimer;
        const int PEDIDO_ANIMALES_DURACION = 3;

        float manR = 160f, autoR = 160f;
        float innerRadius = 90f;

        ParticulasStands.StandRuntime runtime = new();

        public int baseCritChance = 5;
        public int attackSpeed = 1;

        public int returnZeroDuration = 240;

        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");
        static readonly SoundStyle SwingSoundBase = new("Terraria/Sounds/Item_1");

        float syncOffX, syncOffY;
        int netTimer = 0;

        public float fingerOffsetY = -16f;
        public float fingerOffsetX = 0f;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)state);
            writer.Write(auto);
            writer.Write(fDash);
            writer.Write(syncOffX);
            writer.Write(syncOffY);
            writer.Write(laserTimer);
            writer.Write(pedidoAnimales);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            state = (State)reader.ReadByte();
            auto = reader.ReadBoolean();
            fDash = reader.ReadBoolean();
            syncOffX = reader.ReadSingle();
            syncOffY = reader.ReadSingle();
            laserTimer = reader.ReadInt32();
            pedidoAnimales = reader.ReadBoolean();
        }

        public override void SetDefaults()
        {
            Projectile.width = 88;
            Projectile.height = 98;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
            Projectile.ArmorPenetration = 1000;
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

        Vector2 GetFingerWorldPosition()
        {
            bool flipVisual = syncOffX < 0;

            Vector2 localOffset = new Vector2(fingerOffsetX, fingerOffsetY);
            if (flipVisual) localOffset.X *= -1f;

            Vector2 rotatedOffset = localOffset.RotatedBy(Projectile.rotation) * Projectile.scale;

            return Projectile.Center + rotatedOffset;
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
                ParticulasStands.Spawn(p, ParticulasStands.Stands.GoldenRequiem);
            }

            var data = ParticulasStands.Stands.GoldenRequiem;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);

            bool spawning = runtime.spawning;
            bool isOwner = Projectile.owner == Main.myPlayer;

            // Consumo server-side del pedido de invocación (igual patrón que
            // pedidoMariposas en GOLDENSTAND_Tier_4).
            if (pedidoAnimales && Main.netMode != NetmodeID.MultiplayerClient)
            {
                pedidoAnimales = false;
                AnimalesTierManager_Requiem.SpawnAnimales(p);
                Projectile.netUpdate = true;
            }

            if (!spawning && isOwner)
            {
                HandleSkills(p);
                HandleToggle(p);
            }

            if (isOwner && pedidoAnimales)
            {
                pedidoAnimalesTimer--;
                if (pedidoAnimalesTimer <= 0)
                {
                    pedidoAnimales = false;
                }
            }

            auto = Projectile.ai[0] == 1f;

            if (!spawning)
            {
                UpdateSkills(p);
            }

            if (isOwner)
            {
                if (state == State.LaserState)
                {
                    Projectile.friendly = false;
                    laserTimer++;

                    if (laserTimer == 9)
                    {
                        Vector2 firePos = GetFingerWorldPosition();

                        Vector2 dir = Main.MouseWorld - firePos;
                        if (dir != Vector2.Zero) dir.Normalize();
                        else dir = new Vector2(syncOffX < 0 ? -1f : 1f, 0);

                        int dmg = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(10f);

                        SoundEngine.PlaySound(SoundID.Item33, firePos);

                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            firePos,
                            dir,
                            ModContent.ProjectileType<GoldenRequiem_Laser>(),
                            dmg,
                            0f,
                            p.whoAmI,
                            Projectile.whoAmI
                        );
                    }

                    if (laserTimer > 18)
                    {
                        state = State.Idle;
                        laserTimer = 0;
                        frame = 0;
                        Projectile.netUpdate = true;
                    }
                }
                else
                {
                    bool canAttack = ParticulasStands.CanAttack(runtime) && (auto ? (target = FindEnemy(p)) != null : Main.mouseLeft);
                    state = canAttack ? State.Attack : State.Idle;
                    Projectile.friendly = canAttack;
                }

                Vector2 off = GetOffset(p);

                float multRango = 1f;
                if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
                {
                    multRango = rangoPlayer.multiplicadorRango;
                }

                float max = (auto ? autoR : manR) * multRango;

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

                if (state == State.Attack || state == State.LaserState)
                {
                    if (off.Length() <= innerRadius && state != State.LaserState) Projectile.rotation = 0f;
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

                if (state == State.Attack || state == State.LaserState)
                {
                    if (off.Length() <= innerRadius && state != State.LaserState) Projectile.rotation = 0f;
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

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);

            float baseDamage = 10f;
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);

            if (!spawning)
            {
                Animate();
            }
        }

        Vector2 GetOffset(Player p)
        {
            if (fDash) return (Main.MouseWorld - p.Center) * 0.35f;

            if (state == State.LaserState)
            {
                Vector2 dirMouse = Main.MouseWorld - p.Center;
                if (dirMouse != Vector2.Zero)
                {
                    dirMouse.Normalize();
                }
                else
                {
                    dirMouse = new Vector2(p.direction, 0);
                }

                return dirMouse * 50f;
            }

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

                if (Main.netMode != Terraria.ID.NetmodeID.Server)
                {
                    Main.NewText(auto ? "AutoStand-ON" : "AutoStand-OFF", 255, 255, 0);
                }
                Projectile.netUpdate = true;
            }
        }

        void HandleSkills(Player p)
        {
            if (p.whoAmI != Main.myPlayer) return;
            if (!ParticulasStands.CanUseSkills(runtime)) return;
            if (p.HasBuff(ModContent.BuffType<CD>())) return;

            StandStatsPlayer stats = p.GetModPlayer<StandStatsPlayer>();

            // HABILIDAD 1 (SkillF): Disparo Láser
            if ((Main.mouseRight || JojoKeybinds.SkillF.JustPressed) && !p.HasBuff(ModContent.BuffType<Cooldown1>()) && state != State.LaserState)
            {
                float factorTiempoF = Math.Max(0f, 1f - stats.standCooldown1Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown1>(), (int)(700 * factorTiempoF));
                p.AddBuff(ModContent.BuffType<CD>(), 60);
                fDash = false;

                state = State.LaserState;
                laserTimer = 0;
                frame = 0;
                Projectile.netUpdate = true;
            }

            // HABILIDAD 2 (SkillG): Creación de Vida (invoca 3 ranas, 3 mariposas y 3 pájaros)
            if (JojoKeybinds.SkillG.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown2>()))
            {
                float factorTiempoG = Math.Max(0f, 1f - stats.standCooldown2Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown2>(), (int)(700 * factorTiempoG));
                p.AddBuff(ModContent.BuffType<CD>(), 60);

                // La invocación real ahora ocurre en AI() (server/singleplayer
                // only) vía AnimalesTierManager_Requiem, porque los animales
                // pasaron a ser NPCs reales y ya no se pueden crear a la ligera
                // desde cualquier cliente como antes con Projectile.NewProjectile.
                pedidoAnimales = true;
                pedidoAnimalesTimer = PEDIDO_ANIMALES_DURACION;

                Projectile.netUpdate = true;

                // Feedback de sonido inmediato en el cliente que castea.
                SoundEngine.PlaySound(SoundID.Item4, Projectile.Center);
            }

            // HABILIDAD 3 (SkillH): Return To Zero
            if (JojoKeybinds.SkillH.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown3>()))
            {
                float factorTiempoH = Math.Max(0f, 1f - stats.standCooldown3Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown3>(), (int)(700 * factorTiempoH));
                p.AddBuff(ModContent.BuffType<CD>(), 60);

                p.AddBuff(ModContent.BuffType<ReturnToZero>(), returnZeroDuration);

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
            if (state == State.LaserState)
            {
                if (laserTimer < 9)
                {
                    frame = laserTimer / 3;
                }
                else
                {
                    frame = 3;
                }
                return;
            }

            if (++animT < (state == State.Attack ? 5 : 9)) return;
            animT = 0;

            frame = state == State.Idle
                ? (frame + 1) % 4
                : (frame < 4 || frame > 7 ? 4 : (frame + 1 > 7 ? 4 : frame + 1));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player p = Main.player[Projectile.owner];
            var data = ParticulasStands.Stands.GoldenRequiem;
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
            else if (state == State.LaserState)
            {
                tex = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Requiem/Laser/Laser_Requiem").Value;
                r = new Rectangle(frame * 88, 0, 88, 88);
                o = new Vector2(44, 49);
            }
            else
            {
                tex = ModContent.Request<Texture2D>(data.IdleTexture).Value;
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