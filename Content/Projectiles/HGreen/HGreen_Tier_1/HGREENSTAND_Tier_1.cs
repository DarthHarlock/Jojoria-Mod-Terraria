using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using System;
using System.IO;
using Terraria.ID;
using Jojo.Content.Buffs;
using Jojo.Content.Habilidades;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Systems.StandFuncionComunes;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Items;
using Jojo.Content.Items.Potenciadores;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_1.ControlMarioneta_Tier_1;

namespace Jojo.Content.Projectiles.HGreen.HGreen_Tier_1
{
    public class HGREENSTAND_Tier_1 : ModProjectile
    {
        enum State { Idle, Attack }
        State state;

        bool auto, init, dying;
        NPC target;

        int frame, animT, cd, attackTimer;

        // --- LÓGICA DE DASH ADAPTADA Y ACELERADA ---
        bool fDash, shotFired, dashReturning;
        int dashT, dashAnimFrame, dashAnimTimer;
        Vector2 dashDir;
        // -------------------------------------------

        float manR = 160f, autoR = 160f;

        ParticulasStands.StandRuntime runtime = new();

        public int baseCritChance = 5;
        public int attackSpeed = 1;

        public int fanProjectileCount = 3; //CANTIDAD DE DISPAROS
        public float fanSpreadDegrees = 16f;
        public float fanRandomSpreadDegrees = 4f;
        public float fanMinSpeed = 7f;
        public float fanMaxSpeed = 11f;

        public float marionetaShotSpeed = 14f;

        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");
        static readonly SoundStyle SwingSoundBase = new("Terraria/Sounds/Item_1");

        // --- SONIDOS NUEVOS PARA LAS HABILIDADES ---
        static readonly SoundStyle SkillFSound = new("Jojo/Content/Sonidos/HGreen/Latigos_F");

        static readonly Color StandGlowColor = new Color(60, 255, 90);

        float syncOffX, syncOffY;
        int netTimer = 0;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)state);
            writer.Write(auto);
            writer.Write(fDash);
            writer.Write(dashReturning);
            writer.Write(dashDir.X);
            writer.Write(dashDir.Y);
            writer.Write(syncOffX);
            writer.Write(syncOffY);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            state = (State)reader.ReadByte();
            auto = reader.ReadBoolean();
            fDash = reader.ReadBoolean();
            dashReturning = reader.ReadBoolean();
            dashDir = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            syncOffX = reader.ReadSingle();
            syncOffY = reader.ReadSingle();
        }

        public override void SetDefaults()
        {
            Projectile.width = 88;
            Projectile.height = 88;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            // --- PENETRACIÓN DE ARMADURA AÑADIDA AQUÍ ---
            Projectile.ArmorPenetration = 44;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override bool? CanDamage() => false;

        public void StartDying()
        {
            if (dying) return;
            dying = true;
            Projectile.friendly = false;
            Projectile.timeLeft = 25;
            SoundEngine.PlaySound(DespawnSound, Projectile.Center);

            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
            {
                ReleaseAllTetheredEnemies(Main.player[Projectile.owner]);
            }
        }

        private void ReleaseAllTetheredEnemies(Player p)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active)
                {
                    var marioneta = npc.GetGlobalNPC<MarionetaGlobalNPC>();
                    if (marioneta.tethered && marioneta.tetherPlayer == p.whoAmI)
                    {
                        marioneta.Release(npc);
                    }
                }
            }
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
                case 1: return 16;
                case 2: return 12;
                case 3: return 9;
                case 4: return 7;
                default: return 16;
            }
        }

        SoundStyle GetSwingSound(float speed)
        {
            float pitch = speed >= 100f ? 0.1f : speed >= 50f ? 0.05f : 0f;
            return SwingSoundBase with { Pitch = pitch, Volume = 0.5f };
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
                ParticulasStands.Spawn(p, ParticulasStands.Stands.HGreenParticulas1);
            }

            var data = ParticulasStands.Stands.HGreenParticulas1;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);

            bool spawning = runtime.spawning;
            bool isOwner = Projectile.owner == Main.myPlayer;

            if (!spawning)
            {
                if (isOwner)
                {
                    HandleSkills(p);
                    HandleToggle(p);
                }

                UpdateDash(p, isOwner);
            }

            auto = Projectile.ai[0] == 1f;

            if (isOwner)
            {
                bool canAttack = !fDash && ParticulasStands.CanAttack(runtime) && (auto ? (target = FindEnemy(p)) != null : Main.mouseLeft);

                state = (canAttack || fDash) ? State.Attack : State.Idle;
                Projectile.friendly = false;

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
                if (off.Length() > max && state != State.Attack && !fDash) off = Vector2.Normalize(off) * max;

                syncOffX = off.X;
                syncOffY = off.Y;

                if (canAttack)
                {
                    int delay = GetAttackDelay();
                    attackTimer++;
                    if (attackTimer >= delay)
                    {
                        attackTimer = 0;
                        SoundEngine.PlaySound(GetSwingSound(p.GetModPlayer<StandStatsPlayer>().standSpeed), Projectile.Center);
                        ShootFan(Projectile.Center, aimDir, p);
                    }
                }
                else
                {
                    attackTimer = 0;
                }

                if (state == State.Attack || fDash)
                {
                    Vector2 rotDir = fDash ? dashDir : aimDir;
                    float rot = rotDir.ToRotation();
                    if (rotDir.X < 0) rot += MathHelper.Pi;
                    Projectile.rotation = rot;
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
                Vector2 off = fDash ? (dashDir * 60f) : new Vector2(syncOffX, syncOffY);

                if (state == State.Attack || fDash)
                {
                    Vector2 rotDir = fDash ? dashDir : off;
                    float rot = rotDir.ToRotation();
                    if (rotDir.X < 0) rot += MathHelper.Pi;
                    Projectile.rotation = rot;
                }
                else Projectile.rotation = 0f;

                ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);
            }

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);
            float baseDamage = 8f;
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);

            Lighting.AddLight(Projectile.Center, 0.25f, 1f, 0.35f);

            if (!spawning && Main.netMode != NetmodeID.Server && Main.rand.NextBool(6))
            {
                Dust standDust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.GreenFairy, 0f, 0f, 100, default, 0.6f);
                standDust.noGravity = true;
                standDust.color = StandGlowColor;
                standDust.velocity *= 0.3f;
                standDust.fadeIn = 0.3f;
            }

            if (!spawning) Animate();
        }

        void UpdateDash(Player p, bool isOwner)
        {
            if (!fDash) return;

            if (dashT == 0)
            {
                SoundEngine.PlaySound(SkillFSound, Projectile.Center);
            }

            dashT++;
            dashAnimTimer++;

            if (dashAnimTimer >= 3)
            {
                dashAnimTimer = 0;
                if (!dashReturning)
                {
                    if (dashAnimFrame < 3) dashAnimFrame++;

                    if (dashAnimFrame == 2 && !shotFired)
                    {
                        shotFired = true;
                        if (isOwner)
                        {
                            SoundEngine.PlaySound(GetSwingSound(p.GetModPlayer<StandStatsPlayer>().standSpeed), Projectile.Center);

                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                dashDir * marionetaShotSpeed,
                                ModContent.ProjectileType<Esmeralda_3_Tier_1>(),
                                Projectile.damage,
                                Projectile.knockBack,
                                p.whoAmI
                            );
                        }
                    }

                    if (dashAnimFrame >= 3)
                    {
                        dashReturning = true;
                        if (isOwner) Projectile.netUpdate = true;
                    }
                }
                else
                {
                    dashAnimFrame -= 2;
                }
            }

            if (dashReturning && dashAnimFrame <= 0)
            {
                fDash = false;
                dashReturning = false;
                dashT = 0;
                dashAnimFrame = 0;
                dashAnimTimer = 0;
                shotFired = false;
                if (isOwner) Projectile.netUpdate = true;
            }
        }

        void ShootFan(Vector2 spawnPos, Vector2 baseDir, Player p)
        {
            int count = Math.Max(1, fanProjectileCount);
            float totalSpreadRad = MathHelper.ToRadians(fanSpreadDegrees);
            float randomSpreadRad = MathHelper.ToRadians(fanRandomSpreadDegrees);

            for (int i = 0; i < count; i++)
            {
                float rotationOffset = 0f;
                if (count > 1) rotationOffset = -totalSpreadRad / 2f + (totalSpreadRad / (count - 1)) * i;

                Vector2 shootDir = baseDir.RotatedBy(rotationOffset);
                shootDir = shootDir.RotatedByRandom(randomSpreadRad);
                float speed = Main.rand.NextFloat(fanMinSpeed, fanMaxSpeed);
                shootDir *= speed;

                Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPos, shootDir, ModContent.ProjectileType<Esmeralda_Tier_1>(), Projectile.damage, Projectile.knockBack, p.whoAmI);
            }
        }

        Vector2 GetOffset(Player p, Vector2 aimDir)
        {
            if (fDash) return dashDir * 60f;
            if (state == State.Attack) return aimDir * 60f;
            return new Vector2(-40 * p.direction, -10);
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
                if (Main.netMode != Terraria.ID.NetmodeID.Server) Main.NewText(auto ? "AutoStand-ON" : "AutoStand-OFF", 255, 255, 0);
                Projectile.netUpdate = true;

                if (auto)
                {
                    ReleaseAllTetheredEnemies(p);
                }
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
                if (auto) return;

                float factorTiempoF = Math.Max(0f, 1f - stats.standCooldown1Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown1>(), (int)(1800 * factorTiempoF));
                p.AddBuff(ModContent.BuffType<CD>(), 0);

                Vector2 dir = Vector2.Normalize(Main.MouseWorld - Projectile.Center);
                if (dir == Vector2.Zero) dir = new Vector2(p.direction, 0);

                dashDir = dir;
                fDash = true;
                dashReturning = false;
                dashT = 0;
                dashAnimFrame = 0;
                dashAnimTimer = 0;
                shotFired = false;

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
            if (++animT < (state == State.Attack ? 8 : 9)) return;
            animT = 0;
            frame = state == State.Idle ? (frame + 1) % 4 : (frame < 4 || frame > 7 ? 4 : (frame + 1 > 7 ? 4 : frame + 1));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player p = Main.player[Projectile.owner];
            var data = ParticulasStands.Stands.HGreenParticulas1;
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