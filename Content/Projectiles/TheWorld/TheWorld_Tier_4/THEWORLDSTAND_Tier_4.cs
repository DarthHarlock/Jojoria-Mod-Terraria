using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Jojo.Systems.StandFuncionComunes;
using System;
using System.IO;
using Jojo.Content.Buffs;
using Jojo.Systems.StandFuncionComunes;
using Jojo.Content.Habilidades;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Items;
using Jojo.Content.Items.Potenciadores;

namespace Jojo.Content.Projectiles.TheWorld.TheWorld_Tier_4
{
    public class THEWORLDSTAND_Tier_4 : ModProjectile
    {
        enum State { Idle, Attack }
        State state;

        bool auto, init, dying, hFreeze;
        NPC target;

        int frame, animT, cd;
        int freezeT;
        int attackTimer, swingSoundTimer;

        float manR = 155f, autoR = 155f, innerRadius = 90f;

        int tsFrame, tsTimer;

        const int tsSpeed = 10;

        bool knifeAnim;
        int knifeFrame;
        int knifeTimer;
        int knifeCycleCount;
        Vector2 knifeTargetDir;

        const float knifeRadius = 40f;

        ParticulasStands.StandRuntime runtime = new();

        public int baseCritChance = 5;
        public int attackSpeed = 1;

        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");
        static readonly SoundStyle SwingSoundBase = new("Terraria/Sounds/Item_1");
        static readonly SoundStyle TeleportSound = new("Jojo/Content/Sonidos/Teleport");

        float syncOffX, syncOffY;
        int netTimer = 0;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)state);
            writer.Write(hFreeze);
            writer.Write(knifeAnim);
            writer.Write(knifeFrame);
            writer.Write(knifeCycleCount);
            writer.Write(knifeTargetDir.X);
            writer.Write(knifeTargetDir.Y);
            writer.Write(syncOffX);
            writer.Write(syncOffY);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            state = (State)reader.ReadByte();
            hFreeze = reader.ReadBoolean();
            knifeAnim = reader.ReadBoolean();
            knifeFrame = reader.ReadInt32();
            knifeCycleCount = reader.ReadInt32();
            knifeTargetDir = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            syncOffX = reader.ReadSingle();
            syncOffY = reader.ReadSingle();
        }

        public override void SetDefaults()
        {
            Projectile.width = 88;
            Projectile.height = 80;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;

            // PENETRACIÓN DE ARMADURA AÑADIDA AQUÍ
            Projectile.ArmorPenetration = 1000; // Penetración masiva para el Tier 4
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
                ParticulasStands.Spawn(p, ParticulasStands.Stands.TheWorld_4);
            }

            var data = ParticulasStands.Stands.TheWorld_4;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);

            bool spawning = runtime.spawning;
            bool isOwner = Projectile.owner == Main.myPlayer;

            if (!spawning && isOwner)
            {
                HandleSkills(p);
                HandleToggle(p);
                UpdateKnifeAbility(p);
            }

            auto = Projectile.ai[0] == 1f;

            if (isOwner)
            {
                if (knifeAnim)
                {
                    Vector2 dir = Vector2.Normalize(Main.MouseWorld - p.Center);
                    if (dir != Vector2.Zero) knifeTargetDir = dir;
                }

                bool canAttack =
                    ParticulasStands.CanAttack(runtime) &&
                    !hFreeze &&
                    !knifeAnim &&
                    (auto ? (target = FindEnemy(p)) != null : Main.mouseLeft);

                state = canAttack ? State.Attack : State.Idle;
                Projectile.friendly = canAttack;

                Vector2 off = GetOffset(p);

                float multRango = 1f;
                if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
                {
                    multRango = rangoPlayer.multiplicadorRango;
                }

                float max = (auto ? autoR : manR) * multRango;

                if (off.Length() > max && !knifeAnim)
                    off = Vector2.Normalize(off) * max;

                syncOffX = off.X;
                syncOffY = off.Y;

                if (knifeAnim)
                {
                    float rot = knifeTargetDir.ToRotation();
                    if (knifeTargetDir.X < 0) rot += MathHelper.Pi;
                    Projectile.rotation = rot;
                }
                else if (state == State.Attack)
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

                if (state == State.Attack && !knifeAnim)
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

                if (knifeAnim)
                {
                    float rot = knifeTargetDir.ToRotation();
                    if (knifeTargetDir.X < 0) rot += MathHelper.Pi;
                    Projectile.rotation = rot;
                }
                else if (state == State.Attack)
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

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);

            float baseDamage = 80f;
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);

            if (!spawning)
            {
                if (hFreeze) AnimateTimeStop();
                else if (!knifeAnim) Animate();

                UpdateSkills(p);
            }
        }

        public void StartDying()
        {
            if (dying || knifeAnim) return;
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
                Projectile.localNPCHitCooldown = 7;
            }
            else if (s < 100f)
            {
                attackSpeed = 2;
                Projectile.localNPCHitCooldown = 6;
            }
            else if (s < 150f)
            {
                attackSpeed = 3;
                Projectile.localNPCHitCooldown = 5;
            }
            else
            {
                attackSpeed = 4;
                Projectile.localNPCHitCooldown = 4;
            }
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

        Vector2 GetOffset(Player p)
        {
            if (knifeAnim) return knifeTargetDir * knifeRadius;
            if (hFreeze) return new Vector2(30 * p.direction, -10);

            if (state == State.Attack)
                return auto && target != null ? target.Center - p.Center : Main.MouseWorld - p.Center;

            return new Vector2(-40 * p.direction, -10);
        }

        void UpdateSkills(Player p)
        {
            if (hFreeze && TimeEraseNetHandler.IsTimeManipulationActive())
            {
                hFreeze = false;
                freezeT = 0;
                tsFrame = tsTimer = 0;
                p.GetModPlayer<ShaderPlayer>().shaderActivo = false;
                if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
                return;
            }

            if (Projectile.owner == Main.myPlayer)
            {
                if (hFreeze && ++freezeT >= 60)
                {
                    hFreeze = false;
                    freezeT = 0;
                    tsFrame = tsTimer = 0;
                    p.GetModPlayer<ShaderPlayer>().shaderActivo = false;
                    Projectile.netUpdate = true;
                }
            }
            else
            {
                if (hFreeze && ++freezeT >= 60)
                {
                    hFreeze = false;
                    freezeT = 0;
                    tsFrame = tsTimer = 0;
                    p.GetModPlayer<ShaderPlayer>().shaderActivo = false;
                }
            }
        }

        void UpdateKnifeAbility(Player p)
        {
            if (!knifeAnim) return;

            knifeTimer++;
            if (knifeTimer >= 8)
            {
                knifeTimer = 0;
                knifeFrame++;

                if (knifeFrame > 2)
                {
                    knifeFrame = 0;
                    knifeCycleCount++;
                }

                if (knifeCycleCount >= 2)
                {
                    knifeAnim = false;
                    knifeFrame = 0;
                    knifeTimer = 0;
                    knifeCycleCount = 0;
                    Projectile.rotation = 0f;
                    Projectile.netUpdate = true;
                }
            }
        }

        void HandleToggle(Player p)
        {
            if (p.whoAmI != Main.myPlayer) return;
            if (cd-- > 0) return;

            if (JojoKeybinds.ToggleAuto.JustPressed)
            {
                auto = !auto;
                Projectile.ai[0] = auto ? 1f : 0f;
                cd = 30;
                target = null;

                string estadoTexto = auto ? "AutoStand-ON" : "AutoStand-OFF";
                Main.NewText(estadoTexto, Color.Yellow);

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
                if (TimeStopSystem.timeStopped || TimeEraseNetHandler.IsTimeManipulationActive())
                {
                    if (TimeEraseNetHandler.IsTimeManipulationActive())
                    {
                        Main.NewText(TimeStop_TW_Tier_4.GetTimeManipulatedText(), Color.Yellow);
                    }
                    return;
                }

                float factorTiempoF = Math.Max(0f, 1f - stats.standCooldown1Reduction);
                int cooldownFinalF = (int)(2940 * factorTiempoF);

                p.AddBuff(ModContent.BuffType<Cooldown1>(), cooldownFinalF);
                p.AddBuff(ModContent.BuffType<CD>(), 0);
                hFreeze = true;
                freezeT = 0;

                TimeStop_TW_Tier_4.Use(p);
                Projectile.netUpdate = true;
            }

            if (JojoKeybinds.SkillG.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown2>()))
            {
                float factorTiempoG = Math.Max(0f, 1f - stats.standCooldown2Reduction);
                int cooldownFinalG = (int)(420 * factorTiempoG);

                p.AddBuff(ModContent.BuffType<Cooldown2>(), cooldownFinalG);
                p.AddBuff(ModContent.BuffType<CD>(), 60);

                Vector2 dir = Vector2.Normalize(Main.MouseWorld - p.Center);
                if (dir == Vector2.Zero) dir = new Vector2(p.direction, 0);

                knifeTargetDir = dir;
                knifeAnim = true;
                knifeFrame = 0;
                knifeTimer = 0;
                knifeCycleCount = 0;

                SistemaCuchillos.Lanzar(
                    p,
                    ModContent.ProjectileType<Cuchillo_Tier_4>(),
                    daño: Cuchillo_Tier_4.Daño,
                    cuchillosPorRafaga: Cuchillo_Tier_4.CuchillosEnAbanico,
                    totalRafagas: Cuchillo_Tier_4.NumeroDeRafagas,
                    delayEntreRafagas: Cuchillo_Tier_4.DelayEntreRafagas,
                    anguloSeparacion: Cuchillo_Tier_4.AnguloSeparacion,
                    velocidad: Cuchillo_Tier_4.Velocidad,
                    knockback: Cuchillo_Tier_4.Knockback
                );

                Projectile.netUpdate = true;
            }

            if (JojoKeybinds.SkillH.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown3>()))
            {
                Vector2 targetPosition = Main.MouseWorld - p.Size * 0.5f;

                if (!Collision.SolidCollision(targetPosition, p.width, p.height))
                {
                    float factorTiempoH = Math.Max(0f, 1f - stats.standCooldown3Reduction);
                    int cooldownFinalH = (int)(780 * factorTiempoH);

                    p.AddBuff(ModContent.BuffType<Cooldown3>(), cooldownFinalH);
                    p.AddBuff(ModContent.BuffType<CD>(), 60);

                    p.velocity = Vector2.Zero;
                    p.position = targetPosition;

                    SoundEngine.PlaySound(TeleportSound, p.Center);

                    p.fallStart = (int)(p.position.Y / 16f);
                    Projectile.netUpdate = true;
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

            frame = state == State.Idle ? (frame + 1) % 4 :
                (frame < 4 || frame > 7 ? 4 : (frame + 1 > 7 ? 4 : frame + 1));
        }

        void AnimateTimeStop()
        {
            if (++tsTimer < tsSpeed) return;
            tsTimer = 0;

            tsFrame = (tsFrame + 1) % 4;
        }

        static (string folder, string suffix) GetColorSkin(Player p)
        {
            if (UI.StandSlotSystem.HasTheWorldBlueSkinFor(p))
                return ("Jojo/Content/Projectiles/Skins/TheWorld/TheWorld_Blue/", "_Blue");

            if (UI.StandSlotSystem.HasTheWorldRedSkinFor(p))
                return ("Jojo/Content/Projectiles/Skins/TheWorld/TheWorld_Red/", "_Red");

            if (UI.StandSlotSystem.HasTheWorldGreenSkinFor(p))
                return ("Jojo/Content/Projectiles/Skins/TheWorld/TheWorld_Green/", "_Green");

            return (null, null);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player p = Main.player[Projectile.owner];
            var data = ParticulasStands.Stands.TheWorld_4;
            bool spawning = runtime.spawning;

            bool ovaSkin = UI.StandSlotSystem.HasOVASkinFor(p);
            var (colorFolder, colorSuffix) = GetColorSkin(p);

            Texture2D tex;
            Rectangle r;
            Vector2 o;

            if (spawning)
            {
                string spawnTex = ovaSkin
                    ? "Jojo/Content/Projectiles/Skins/TheWorld/The_World_OVA/TW_Spawn"
                    : colorFolder != null
                        ? colorFolder + "TW_Spawn" + colorSuffix
                        : data.SpawnTexture;

                tex = ModContent.Request<Texture2D>(spawnTex).Value;
                r = new Rectangle(runtime.frame * data.FrameWidth, 0, data.FrameWidth, data.SpawnHeight);
                o = new Vector2(data.FrameWidth / 2f, data.SpawnHeight / 2f);
            }
            else if (knifeAnim)
            {
                string pathKnives = colorFolder != null
                    ? colorFolder + "Cuchillos" + colorSuffix
                    : "Jojo/Content/Projectiles/TheWorld/TheWorld_Tier_4/Cuchillos_Tier_4";

                tex = ModContent.Request<Texture2D>(pathKnives).Value;

                r = new Rectangle(knifeFrame * 88, 0, 88, 92);
                o = new Vector2(44, 46);
            }
            else if (hFreeze)
            {
                string pathTS = ovaSkin
                    ? "Jojo/Content/Projectiles/Skins/TheWorld/The_World_OVA/TimeStop_Tier_4"
                    : colorFolder != null
                        ? colorFolder + "TimeStop" + colorSuffix
                        : "Jojo/Content/Projectiles/TheWorld/TheWorld_Tier_4/TimeStop_Tier_4";

                tex = ModContent.Request<Texture2D>(pathTS).Value;
                r = new Rectangle(tsFrame * 88, 0, 88, 74);
                o = new Vector2(44, 37);
            }
            else
            {
                string pathIdle = ovaSkin
                    ? "Jojo/Content/Projectiles/Skins/TheWorld/The_World_OVA/THEWORLDSTAND_Tier_4"
                    : colorFolder != null
                        ? colorFolder + "THEWORLDSTAND" + colorSuffix
                        : data.IdleTexture;

                tex = ModContent.Request<Texture2D>(pathIdle).Value;
                r = new Rectangle(frame * 88, 0, 88, 80);
                o = new Vector2(44, 35);
            }

            SpriteEffects effects = SpriteEffects.None;
            if (knifeAnim)
            {
                if (knifeTargetDir.X < 0) effects = SpriteEffects.FlipHorizontally;
            }
            else
            {
                bool goRight = syncOffX >= 0;
                if (!goRight) effects = SpriteEffects.FlipHorizontally;
            }

            Main.EntitySpriteDraw(
                tex,
                Projectile.Center - Main.screenPosition,
                r,
                lightColor * ((255 - Projectile.alpha) / 255f),
                Projectile.rotation,
                o,
                Projectile.scale,
                effects,
                0f
            );

            return false;
        }
    }
}