using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using System;
using System.IO;
using System.Collections.Generic;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.StarPlatinum_Buffs;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Habilidades;
using Jojo.Content.Items;
using Jojo.Content.Items.Potenciadores;
using Jojo.Content.Projectiles.StarPlatinum;
using Jojo.Systems.StandFuncionComunes;

namespace Jojo.Content.Projectiles.StarPlatinum.StarPlatinum_Tier_2
{
    public class STARPLATINUMSTAND_Tier_2 : ModProjectile
    {
        enum State { Idle, Attack }
        State state;

        bool auto, init, dying, starFinger;
        NPC target;

        int frame, animT, cd;
        int swingSoundTimer;

        float manR = 135f, autoR = 135f, innerRadius = 90f;

        const int NormalW = 88, NormalH = 76;

        const float StarFingerDamage = 60f;

        public float starFingerOffsetX = 26f;
        public float starFingerOffsetY = -13f;

        // ================== HABILIDAD G (Furia de Platinum) ==================
        // Buff que da la furia. Si tienes PlatinumRage_Tier_2 o _Tier_3, cambia solo esta línea.
        int BuffFuria => ModContent.BuffType<PlatinumRage_Tier_4>();
        const int DuracionFuria = 600;     // ticks que dura la furia (60 = 1 segundo)
        const int CooldownFuria = 3600;    // ticks de cooldown de la habilidad G

        readonly StarFingerController fingerController = new StarFingerController()
        {
            MaxSegments = 14,
            SegmentLength = 14f,
            HoldTimeTicks = 20,
            TicksPerSegmentGrow = 1,
            TicksPerSegmentRetract = 1
        };

        const float StandFollowSpeed = 0.25f;
        const float StarFingerFollowSpeedMultiplier = 0.25f;

        public int baseCritChance = 5;
        public int attackSpeed = 1;

        private static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        private static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");
        private static readonly SoundStyle SwingSoundBase = new("Terraria/Sounds/Item_1");
        private static readonly SoundStyle PunchSound = new("Jojo/Content/Sonidos/¡ORA!");

        float syncOffX, syncOffY;
        int netTimer = 0;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)state);
            writer.Write(starFinger);
            writer.Write(syncOffX);
            writer.Write(syncOffY);
            fingerController.Write(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            state = (State)reader.ReadByte();
            starFinger = reader.ReadBoolean();
            syncOffX = reader.ReadSingle();
            syncOffY = reader.ReadSingle();
            fingerController.Read(reader);
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

            Projectile.ArmorPenetration = 1000;
        }

        public void StartDying()
        {
            if (dying) return;
            dying = true;
            Projectile.friendly = false;
            Projectile.timeLeft = 25;
            Player p = Main.player[Projectile.owner];
            if (p.active) p.ClearBuff(BuffFuria);   // quita la furia al desaparecer el stand
            SoundEngine.PlaySound(DespawnSound, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            Player p = Main.player[Projectile.owner];
            if (p.active) p.ClearBuff(BuffFuria);   // quita la furia al desaparecer el stand
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

        int GetSwingDelay(float speed)
        {
            if (speed >= 100f) return 6;
            if (speed >= 50f) return 8;
            return 11;
        }

        SoundStyle GetSwingSound(float speed)
        {
            float pitch = speed >= 100f ? 0.05f : speed >= 50f ? 0.08f : 0f;
            return SwingSoundBase with { Pitch = pitch };
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            UpdateAttackSpeed(p);

            if (dying)
            {
                ParticulasStands.Despawn(Projectile, p);
                return;
            }

            if (!p.active || p.dead)
            {
                StartDying();
                return;
            }

            if (!init)
            {
                init = true;
                Projectile.alpha = 0;
                Projectile.friendly = false;
                Projectile.damage = 0;
                SoundEngine.PlaySound(SpawnSound, Projectile.Center);
                ParticulasStands.Spawn(p, ParticulasStands.Stands.StarPlatinum2);
            }

            var data = ParticulasStands.Stands.StarPlatinum2;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);

            bool spawning = runtime.spawning;
            bool isOwner = Projectile.owner == Main.myPlayer;

            auto = Projectile.ai[0] == 1f;

            if (!spawning && isOwner)
            {
                HandleToggle(p);
                HandleSkills(p);
            }

            float followSpeed = starFinger ? StandFollowSpeed * StarFingerFollowSpeedMultiplier : StandFollowSpeed;

            if (isOwner)
            {
                bool canAttack = starFinger || (ParticulasStands.CanAttack(runtime) && (auto ? (target = FindEnemy(p)) != null : Main.mouseLeft));

                state = canAttack ? State.Attack : State.Idle;
                Projectile.friendly = canAttack;

                Vector2 off = GetOffset(p);

                float multRango = 1f;
                if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
                {
                    multRango = rangoPlayer.multiplicadorRango;
                }

                float max = (auto ? autoR : manR) * multRango;
                if (off.Length() > max) off = Vector2.Normalize(off) * max;

                syncOffX = off.X;
                syncOffY = off.Y;

                if (state == State.Attack && !spawning && !starFinger)
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

                    Projectile.friendly = true;
                }
                else
                {
                    swingSoundTimer = 0;
                }

                Projectile.width = NormalW;
                Projectile.height = NormalH;

                Projectile.rotation = state == State.Attack
                    ? (off.Length() <= innerRadius ? 0f : (off.ToRotation() + (off.X < 0 ? MathHelper.Pi : 0f)))
                    : 0f;

                ParticulasStands.FollowPlayer(Projectile, p, off, followSpeed);

                if (starFinger)
                {
                    Vector2 fingerOrigin = GetFingerBaseOrigin();
                    bool standMirandoIzquierda = syncOffX < 0;
                    Vector2 currentAimDir;

                    if (off.Length() <= innerRadius)
                    {
                        currentAimDir = new Vector2(standMirandoIzquierda ? -1f : 1f, 0f);
                    }
                    else
                    {
                        currentAimDir = Vector2.Normalize(off);
                    }

                    if (!fingerController.Active)
                        fingerController.Begin(fingerOrigin, currentAimDir);
                    else
                    {
                        fingerController.origin = fingerOrigin;
                        fingerController.direction = currentAimDir;
                    }

                    bool stillGoing = fingerController.Update();
                    if (!stillGoing)
                    {
                        starFinger = false;
                        state = State.Idle;
                        frame = 0;
                        animT = 0;
                        Projectile.friendly = false;
                    }

                    Projectile.netUpdate = true;
                }

                if (++netTimer >= 5)
                {
                    netTimer = 0;
                    Projectile.netUpdate = true;
                }
            }
            else
            {
                Vector2 off = new Vector2(syncOffX, syncOffY);

                Projectile.width = NormalW;
                Projectile.height = NormalH;

                Projectile.rotation = state == State.Attack
                    ? (off.Length() <= innerRadius ? 0f : (off.ToRotation() + (off.X < 0 ? MathHelper.Pi : 0f)))
                    : 0f;

                ParticulasStands.FollowPlayer(Projectile, p, off, followSpeed);
            }

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);

            float baseDamage = starFinger ? StarFingerDamage : 30f;
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);

            if (!spawning) Animate();
        }

        void HandleSkills(Player p)
        {
            if (p.whoAmI != Main.myPlayer) return;
            if (p.HasBuff(ModContent.BuffType<CD>())) return;

            StandStatsPlayer stats = p.GetModPlayer<StandStatsPlayer>();

            // HABILIDAD F (Star Finger)
            if (JojoKeybinds.SkillF.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown1>()) && !starFinger)
            {
                starFinger = true;
                SoundEngine.PlaySound(PunchSound, Projectile.Center);

                float factorTiempoF = Math.Max(0f, 1f - stats.standCooldown1Reduction);
                int cooldownFinalF = (int)(600 * factorTiempoF);

                p.AddBuff(ModContent.BuffType<Cooldown1>(), cooldownFinalF);
                p.AddBuff(ModContent.BuffType<CD>(), 90);
                Projectile.netUpdate = true;
            }

            // HABILIDAD G (Furia de Platinum): igual que en el Tier 4, pero más corta y con más cooldown
            if (JojoKeybinds.SkillG.JustPressed)
            {
                if (p.HasBuff(ModContent.BuffType<CD>()) || p.HasBuff(ModContent.BuffType<Cooldown2>())) return;

                p.AddBuff(BuffFuria, DuracionFuria);

                float factorTiempoG = Math.Max(0f, 1f - stats.standCooldown2Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown2>(), (int)(CooldownFuria * factorTiempoG));
                p.AddBuff(ModContent.BuffType<CD>(), 180);
                cd = 180;
            }
        }

        void HandleToggle(Player p)
        {
            if (cd > 0) { cd--; return; }
            if (p.whoAmI != Main.myPlayer) return;

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

        void Animate()
        {
            if (starFinger) return;

            if (state == State.Idle && frame >= 4)
            {
                frame = 0;
                animT = 0;
            }

            if (++animT < (state == State.Attack ? 5 : 9)) return;
            animT = 0;

            if (state == State.Idle)
            {
                frame++;
                if (frame > 3) frame = 0;
            }
            else
            {
                frame++;
                if (frame < 4 || frame > 7) frame = 4;
            }
        }

        NPC FindEnemy(Player p)
        {
            return StandTargeting.FindHostileEnemy(p, autoR);
        }

        Vector2 GetOffset(Player p)
        {
            if (starFinger) return Main.MouseWorld - p.Center;

            if (state == State.Attack)
                return auto && target != null ? target.Center - p.Center : Main.MouseWorld - p.Center;
            return new Vector2(-40 * p.direction, -10);
        }

        Vector2 GetFingerBaseOrigin()
        {
            bool flipVisual = syncOffX < 0;

            Vector2 localOffset = new Vector2(starFingerOffsetX, starFingerOffsetY);
            if (flipVisual) localOffset.X *= -1f;

            Vector2 rotatedOffset = localOffset.RotatedBy(Projectile.rotation) * Projectile.scale;

            return Projectile.Center + rotatedOffset;
        }

        ParticulasStands.StandRuntime runtime = new();

        public override bool PreDraw(ref Color lightColor)
        {
            Player p = Main.player[Projectile.owner];
            var data = ParticulasStands.Stands.StarPlatinum2;

            bool OVASkin = UI.StandSlotSystem.HasOVASkinFor(p);
            bool RedSkin = UI.StandSlotSystem.HasStarPlatinumRedSkinFor(p);
            bool GreenSkin = UI.StandSlotSystem.HasStarPlatinumGreenSkinFor(p);
            bool BlueSkin = UI.StandSlotSystem.HasStarPlatinumBlueSkinFor(p);

            string pathNormal = "Jojo/Content/Projectiles/StarPlatinum/StarPlatinum_Tier_2/";
            string pathOVA = "Jojo/Content/Projectiles/Skins/StarPlatinum/Star_Platinum_OVA/";
            string pathRed = "Jojo/Content/Projectiles/Skins/StarPlatinum/Star_Platinum_Red/";
            string pathGreen = "Jojo/Content/Projectiles/Skins/StarPlatinum/Star_Platinum_Green/";
            string pathBlue = "Jojo/Content/Projectiles/Skins/StarPlatinum/Star_Platinum_Blue/";

            string fingerBaseTexPath;
            string segmentTexPath;
            string endTexPath;

            if (OVASkin)
            {
                fingerBaseTexPath = pathOVA + "StarFinger_OVA";
                segmentTexPath = pathOVA + "Finger_Segmento_OVA";
                endTexPath = pathOVA + "Finger_End_OVA";
            }
            else if (RedSkin)
            {
                fingerBaseTexPath = pathRed + "StarFinger_Red";
                segmentTexPath = pathRed + "Finger_Segmento_Red";
                endTexPath = pathRed + "Finger_End_Red";
            }
            else if (GreenSkin)
            {
                fingerBaseTexPath = pathGreen + "StarFinger_Green";
                segmentTexPath = pathGreen + "Finger_Segmento_Green";
                endTexPath = pathGreen + "Finger_End_Green";
            }
            else if (BlueSkin)
            {
                fingerBaseTexPath = pathBlue + "StarFinger_Blue";
                segmentTexPath = pathBlue + "Finger_Segmento_Blue";
                endTexPath = pathBlue + "Finger_End_Blue";
            }
            else
            {
                string fingerFolder = pathNormal + "StarFinger_Tier_2/";
                fingerBaseTexPath = fingerFolder + "StarFinger_Tier_2";
                segmentTexPath = fingerFolder + "Finger_Segmento";
                endTexPath = fingerFolder + "Finger_End";
            }

            Texture2D tex;
            Rectangle r;
            Vector2 o;

            if (starFinger)
            {
                tex = ModContent.Request<Texture2D>(fingerBaseTexPath).Value;
                r = new Rectangle(0, 0, 60, 80);
                o = new Vector2(30, 40);
            }
            else if (runtime.spawning)
            {
                string spawnTex;
                if (OVASkin) spawnTex = pathOVA + "SP_Spawn_OVA";
                else if (RedSkin) spawnTex = pathRed + "SP_Spawn_Red";
                else if (GreenSkin) spawnTex = pathGreen + "SP_Spawn_Green";
                else if (BlueSkin) spawnTex = pathBlue + "SP_Spawn_Blue";
                else spawnTex = data.SpawnTexture;

                tex = ModContent.Request<Texture2D>(spawnTex).Value;
                r = new Rectangle(runtime.frame * data.FrameWidth, 0, data.FrameWidth, data.SpawnHeight);
                o = new Vector2(data.FrameWidth / 2f, data.SpawnHeight / 2f);
            }
            else
            {
                string texName;
                if (OVASkin) texName = pathOVA + "STARPLATINUMSTAND_OVA";
                else if (RedSkin) texName = pathRed + "STARPLATINUMSTAND_Red";
                else if (GreenSkin) texName = pathGreen + "STARPLATINUMSTAND_Green";
                else if (BlueSkin) texName = pathBlue + "STARPLATINUMSTAND_Blue";
                else texName = pathNormal + "STARPLATINUMSTAND_Tier_2";

                tex = ModContent.Request<Texture2D>(texName).Value;
                r = new Rectangle(frame * 88, 0, 88, 80);
                o = new Vector2(44, 38);
            }

            bool flipVisual = syncOffX < 0;

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, r,
                lightColor * ((255 - Projectile.alpha) / 255f),
                Projectile.rotation, o, Projectile.scale,
                flipVisual ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

            if (starFinger)
            {
                Texture2D segmentTex = ModContent.Request<Texture2D>(segmentTexPath).Value;
                Texture2D endTex = ModContent.Request<Texture2D>(endTexPath).Value;

                Color fingerColor = lightColor * ((255 - Projectile.alpha) / 255f);
                fingerController.Draw(segmentTex, endTex, fingerColor, Projectile.scale);
            }

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (starFinger)
            {
                return fingerController.CheckHit(targetHitbox, NormalH);
            }

            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 size = new Vector2(Projectile.width, Projectile.height);
            Vector2 start = Projectile.Center - dir * (size.X * 0.5f);
            Vector2 end = Projectile.Center + dir * (size.X * 0.5f);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(), targetHitbox.Size(),
                start, end, size.Y, ref collisionPoint);
        }
    }
}