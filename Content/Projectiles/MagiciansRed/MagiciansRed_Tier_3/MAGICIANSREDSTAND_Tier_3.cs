using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Jojo.Systems.StandFuncionComunes;
using Terraria.Audio;
using Terraria.ID;
using System;
using System.IO;
using System.Collections.Generic;
using Jojo.Content.Buffs;
using Jojo.Content.Habilidades;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Items;
using Jojo.Content.Items.Potenciadores;

namespace Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_3
{
    public class MAGICIANSREDSTAND_Tier_3 : ModProjectile
    {
        enum State { Idle, Attack, Flamethrower }
        State state;

        bool auto, init, dying, fDash;
        NPC target;

        int frame, animT, cd, dashT;
        int attackTimer, swingSoundTimer;

        float manR = 160f, autoR = 300f;

        ParticulasStands.StandRuntime runtime = new();

        public int baseCritChance = 5;
        public int attackSpeed = 1;

        // --- VARIABLES DEL LANZALLAMAS ---
        public int dañoLanzallamas = 10;
        public int velocidadDisparoLanzallamas = 5;
        public int distanciaLanzallamas = 35;
        // ---------------------------------

        public int tornadoDamage = 50;
        public int tornadoDurationTicks = 420;
        public int tornadoGrowTicks = 70;
        public int tornadoDyingTicks = 80;
        public int tornadoSize = 12;
        public int tornadoTickCooldown = 20;

        public int fanProjectileCount = 3;
        public float fanSpreadDegrees = 30f;
        public float fanProjectileSpeed = 7f;

        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");

        float syncOffX, syncOffY;
        int netTimer = 0;

        const string SkinBasePath = "Jojo/Content/Projectiles/Skins/MagiciansRed/";

        static string GetMagiciansRedSkinFolder(Player p)
        {
            if (UI.StandSlotSystem.HasMagiciansRedPinkSkinFor(p))
                return SkinBasePath + "Magicias_Pink/";

            if (UI.StandSlotSystem.HasMagiciansRedGreenSkinFor(p))
                return SkinBasePath + "Magicias_Green/";

            if (UI.StandSlotSystem.HasMagiciansRedBlueSkinFor(p))
                return SkinBasePath + "Magicias_Blue/";

            return null;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)state);
            writer.Write(auto);
            writer.Write(fDash);
            writer.Write(syncOffX);
            writer.Write(syncOffY);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            state = (State)reader.ReadByte();
            auto = reader.ReadBoolean();
            fDash = reader.ReadBoolean();
            syncOffX = reader.ReadSingle();
            syncOffY = reader.ReadSingle();
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

        public override bool? CanDamage()
        {
            return false;
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

            if (s < 50f)
            {
                attackSpeed = 1;
                Projectile.localNPCHitCooldown = 6;
            }
            else if (s < 100f)
            {
                attackSpeed = 2;
                Projectile.localNPCHitCooldown = 5;
            }
            else if (s < 150f)
            {
                attackSpeed = 3;
                Projectile.localNPCHitCooldown = 4;
            }
            else
            {
                attackSpeed = 4;
                Projectile.localNPCHitCooldown = 3;
            }
        }

        int GetAttackDelay()
        {
            switch (attackSpeed)
            {
                case 1: return 50;
                case 2: return 42;
                case 3: return 34;
                case 4: return 26;
                default: return 50;
            }
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
                ParticulasStands.Spawn(p, ParticulasStands.Stands.MagiciansRed3);
            }

            var data = ParticulasStands.Stands.MagiciansRed3;
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

            if (isOwner)
            {
                bool canAttack = ParticulasStands.CanAttack(runtime) && (auto ? (target = FindEnemy(p)) != null : Main.mouseLeft);
                bool canFlamethrower = ParticulasStands.CanAttack(runtime) && Main.mouseRight;

                if (canFlamethrower)
                {
                    state = State.Flamethrower;
                }
                else if (canAttack)
                {
                    state = State.Attack;
                }
                else
                {
                    state = State.Idle;
                }

                Projectile.friendly = canAttack || canFlamethrower;

                Vector2 aimDir = (auto && target != null) ? target.Center - p.Center : Main.MouseWorld - p.Center;

                if (aimDir.LengthSquared() < 16f)
                    aimDir = new Vector2(p.direction, 0f);

                aimDir = aimDir.SafeNormalize(new Vector2(p.direction, 0f));

                Vector2 off = GetOffset(p, aimDir);

                float multRango = 1f;
                if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
                {
                    multRango = rangoPlayer.multiplicadorRango;
                }

                float max = (auto ? autoR : manR) * multRango;

                if (off.Length() > max && state != State.Attack && state != State.Flamethrower)
                    off = Vector2.Normalize(off) * max;

                syncOffX = off.X;
                syncOffY = off.Y;

                // --- ATAQUE NORMAL (CLICK IZQUIERDO) ---
                if (state == State.Attack)
                {
                    int delay = GetAttackDelay();

                    swingSoundTimer++;
                    if (swingSoundTimer >= delay)
                    {
                        SoundEngine.PlaySound(SoundID.Item20, Projectile.Center);
                        swingSoundTimer = 0;
                    }

                    attackTimer++;
                    if (attackTimer >= delay)
                    {
                        attackTimer = 0;

                        Vector2 spawnPos = Projectile.Center;
                        ShootFan(spawnPos, aimDir, p);
                    }
                }
                // --- LANZALLAMAS (CLICK DERECHO) ---
                else if (state == State.Flamethrower)
                {
                    if (distanciaLanzallamas <= 0)
                    {
                        swingSoundTimer = 0;
                        attackTimer = 0;
                    }
                    else
                    {
                        int flamerDelay = velocidadDisparoLanzallamas;

                        swingSoundTimer++;
                        if (swingSoundTimer >= 20)
                        {
                            SoundEngine.PlaySound(SoundID.Item34, Projectile.Center);
                            swingSoundTimer = 0;
                        }

                        attackTimer++;
                        if (attackTimer >= flamerDelay)
                        {
                            attackTimer = 0;

                            Vector2 offsetCentroVisual = new Vector2(0, -25f).RotatedBy(Projectile.rotation);
                            Vector2 spawnPos = Projectile.Center + offsetCentroVisual;

                            float velocidadLlama = 4f + (distanciaLanzallamas * 0.12f);
                            Vector2 shootDir = (aimDir * velocidadLlama).RotatedByRandom(MathHelper.ToRadians(12));

                            int damageToDeal = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(dañoLanzallamas);

                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                spawnPos,
                                shootDir,
                                ModContent.ProjectileType<FuegoLanzallamas_Tier_3>(),
                                damageToDeal,
                                Projectile.knockBack * 0.1f,
                                p.whoAmI,
                                0f,
                                distanciaLanzallamas
                            );
                        }
                    }
                }
                else
                {
                    swingSoundTimer = 0;
                }

                if (state == State.Attack || state == State.Flamethrower)
                {
                    float rotDir = aimDir.ToRotation();
                    if (aimDir.X < 0) rotDir += MathHelper.Pi;
                    Projectile.rotation = rotDir;
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

                if (state == State.Attack || state == State.Flamethrower)
                {
                    float rot = off.ToRotation();
                    if (off.X < 0) rot += MathHelper.Pi;
                    Projectile.rotation = rot;
                }
                else Projectile.rotation = 0f;

                ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);
            }

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);

            float baseDamage = 45f;
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);

            if (!spawning)
            {
                Animate();
            }
        }

        void ShootFan(Vector2 spawnPos, Vector2 baseDir, Player p)
        {
            int count = Math.Max(1, fanProjectileCount);
            float totalSpreadRad = MathHelper.ToRadians(fanSpreadDegrees);

            for (int i = 0; i < count; i++)
            {
                float rotationOffset = 0f;

                if (count > 1)
                {
                    rotationOffset = -totalSpreadRad / 2f + (totalSpreadRad / (count - 1)) * i;
                }

                Vector2 shootDir = baseDir.RotatedBy(rotationOffset) * fanProjectileSpeed;

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPos,
                    shootDir,
                    ModContent.ProjectileType<Fuego1_Tier_3>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    p.whoAmI
                );
            }
        }

        Vector2 GetOffset(Player p, Vector2 aimDir)
        {
            if (fDash) return (Main.MouseWorld - p.Center) * 0.35f;

            if (state == State.Attack || state == State.Flamethrower)
                return aimDir * 60f;

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

            if (JojoKeybinds.SkillF.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown1>()))
            {
                float factorTiempoF = Math.Max(0f, 1f - stats.standCooldown1Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown1>(), (int)(900 * factorTiempoF));
                p.AddBuff(ModContent.BuffType<CD>(), 60);
                fDash = false;

                MagiciansFire2Helper.Activar(p);
                Projectile.netUpdate = true;
            }

            if (JojoKeybinds.SkillG.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown2>()))
            {
                float factorTiempoG = Math.Max(0f, 1f - stats.standCooldown2Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown2>(), (int)(1800 * factorTiempoG));
                p.AddBuff(ModContent.BuffType<CD>(), 60);

                SpawnTornado(p);
                Projectile.netUpdate = true;
            }

            if (JojoKeybinds.SkillH.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown3>()))
            {
                float factorTiempoH = Math.Max(0f, 1f - stats.standCooldown3Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown3>(), (int)(700 * factorTiempoH));
                p.AddBuff(ModContent.BuffType<CD>(), 60);
                Projectile.netUpdate = true;
            }
        }

        void SpawnTornado(Player p)
        {
            float multRango = 1f;
            if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
            {
                multRango = rangoPlayer.multiplicadorRango;
            }

            // CORRECCIÓN: El tornado ahora tiene su propia distancia máxima masiva en vez de usar la corta de 'manR'
            float rangoExclusivoTornado = 450f;
            float maxRange = rangoExclusivoTornado * multRango;

            Vector2 dir = Main.MouseWorld - p.Center;
            if (dir.Length() > maxRange)
                dir = dir.SafeNormalize(Vector2.UnitX) * maxRange;

            Vector2 spawnPos = p.Center + dir;

            // CORRECCIÓN: Sonido nuevo de viento/fuego fuerte al colocar el tornado.
            SoundEngine.PlaySound(SoundID.Item82, spawnPos);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPos,
                Vector2.Zero,
                ModContent.ProjectileType<Tornado_Tier_3>(),
                tornadoDamage,
                0f,
                p.whoAmI
            );
        }

        // Auto-apuntado: solo hostiles (ignora pacíficos, critters, town NPCs y dummies).
        // Aplica el multiplicador de rango internamente.
        NPC FindEnemy(Player p)
        {
            return StandTargeting.FindHostileEnemy(p, autoR);
        }

        void Animate()
        {
            // CORRECCIÓN: Ahora siempre tardará 9 ticks para cambiar de frame, ya no importan los stats de velocidad
            // ni si está en ataque o idle. Se verá tranquilo siempre.
            if (++animT < 9) return;
            animT = 0;

            frame = state == State.Idle
                ? (frame + 1) % 4
                : (frame < 4 || frame > 7 ? 4 : (frame + 1 > 7 ? 4 : frame + 1));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player p = Main.player[Projectile.owner];
            var data = ParticulasStands.Stands.MagiciansRed3;
            bool spawning = runtime.spawning;

            string skinFolder = GetMagiciansRedSkinFolder(p);

            Texture2D tex;
            Rectangle r;
            Vector2 o;

            if (spawning)
            {
                string spawnPath = skinFolder != null
                    ? skinFolder + "MagiciansRed_Spawn"
                    : data.SpawnTexture;

                tex = ModContent.Request<Texture2D>(spawnPath).Value;
                r = new Rectangle(runtime.frame * data.FrameWidth, 0, data.FrameWidth, data.SpawnHeight);
                o = new Vector2(data.FrameWidth / 2f, data.SpawnHeight / 2f);
                lightColor = Color.White;
            }
            else
            {
                string idlePath = skinFolder != null
                    ? skinFolder + "MAGICIANSREDSTAND"
                    : data.IdleTexture;

                tex = ModContent.Request<Texture2D>(idlePath).Value;
                r = new Rectangle(frame * 88, 0, 88, 76);
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