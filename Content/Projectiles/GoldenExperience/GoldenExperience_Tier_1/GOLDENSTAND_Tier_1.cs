using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using Terraria.GameInput;
using System;
using System.IO;
using System.Collections.Generic;
using Jojo.Systems.StandFuncionComunes;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.GoldenExperience_Buffs;
using Jojo.Content.Habilidades;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Items;
using Jojo.Content.Items.Potenciadores;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_1.Arbol_Tier_1;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_1
{
    public class GOLDENSTAND_Tier_1 : ModProjectile
    {
        enum State { Idle, Attack }
        State state;

        bool auto, init, dying, fDash;
        NPC target;

        int frame, animT, cd, dashT;
        int attackTimer, swingSoundTimer;

        float manR = 120f, autoR = 120f;
        float innerRadius = 90f;

        // ==========================================
        // --- CONFIGURACIÓN DE HABILIDADES ---
        // ==========================================
        public float rangoMaximoHabilidadF = 180f;
        public float velocidadTransformacion = 0.2f;
        public float distanciaAtaqueX = 11f;
        public float distanciaAtaqueY = 21f;

        public float curacionCantidad = 22f;
        public int curacionCooldownDuracion = 1800;
        // ==========================================

        bool isTransforming;
        byte activeTransformSkill;
        int transformPhase;
        int transformTimer;
        Vector2 transformTargetPos;

        bool curacionRedActiva;
        int curacionRedTimer;
        const int CURACION_RED_DURACION = 6;

        bool pedidoTransformSpawn;
        int pedidoTransformSpawnTimer;
        const int PEDIDO_TRANSFORM_SPAWN_DURACION = 3;

        ParticulasStands.StandRuntime runtime = new();

        public int baseCritChance = 5;
        public int attackSpeed = 1;

        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");
        static readonly SoundStyle SwingSoundBase = new("Terraria/Sounds/Item_1");

        float syncOffX, syncOffY;
        int netTimer = 0;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)state);
            writer.Write(auto);
            writer.Write(fDash);
            writer.Write(syncOffX);
            writer.Write(syncOffY);
            writer.Write(isTransforming);
            writer.Write(activeTransformSkill);
            writer.Write((byte)transformPhase);
            writer.WriteVector2(transformTargetPos);
            writer.Write(curacionRedActiva);
            writer.Write(pedidoTransformSpawn);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            state = (State)reader.ReadByte();
            auto = reader.ReadBoolean();
            fDash = reader.ReadBoolean();
            syncOffX = reader.ReadSingle();
            syncOffY = reader.ReadSingle();
            isTransforming = reader.ReadBoolean();
            activeTransformSkill = reader.ReadByte();
            transformPhase = reader.ReadByte();
            transformTargetPos = reader.ReadVector2();

            bool curacionRedPrevio = curacionRedActiva;
            curacionRedActiva = reader.ReadBoolean();

            if (curacionRedActiva && !curacionRedPrevio && Projectile.owner != Main.myPlayer)
            {
                Player dueño = Main.player[Projectile.owner];
                if (dueño.active)
                    CuracionManager_Tier_1.ReproducirEfectosVisuales(dueño);
            }

            pedidoTransformSpawn = reader.ReadBoolean();
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
            isTransforming = false;
            transformPhase = 0;
            Projectile.friendly = false;
            Projectile.timeLeft = 25;
            SoundEngine.PlaySound(DespawnSound, Projectile.Center);

            Arbol_Torre_Controller_Tier_1.MatarArbolesDe(Projectile.owner);
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

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];

            if (p.active && !p.dead && !dying) Projectile.timeLeft = 2;

            UpdateAttackSpeed(p);

            if (dying)
            {
                isTransforming = false;
                transformPhase = 0;
                ParticulasStands.Despawn(Projectile, p);
                return;
            }
            if (!p.active || p.dead) { StartDying(); return; }

            if (!init)
            {
                init = true;
                Projectile.alpha = 0;
                Projectile.friendly = false;
                Projectile.damage = 0;
                SoundEngine.PlaySound(SpawnSound, Projectile.Center);
                ParticulasStands.Spawn(p, ParticulasStands.Stands.Golden1);
            }

            var data = ParticulasStands.Stands.Golden1;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);

            bool spawning = runtime.spawning;
            bool isOwner = Projectile.owner == Main.myPlayer;

            if (pedidoTransformSpawn && activeTransformSkill == 1 && isOwner)
            {
                pedidoTransformSpawn = false;
                ArbolTierManager_Tier_1.SpawnArbol(p, transformTargetPos);
                Projectile.netUpdate = true;
            }

            if (!spawning && isOwner)
            {
                HandleSkills(p);
                HandleToggle(p);
            }

            if (isOwner && curacionRedActiva)
            {
                curacionRedTimer--;
                if (curacionRedTimer <= 0)
                {
                    curacionRedActiva = false;
                    Projectile.netUpdate = true;
                }
            }

            if (isOwner && pedidoTransformSpawn)
            {
                pedidoTransformSpawnTimer--;
                if (pedidoTransformSpawnTimer <= 0)
                {
                    pedidoTransformSpawn = false;
                }
            }

            auto = Projectile.ai[0] == 1f;

            if (!spawning)
            {
                UpdateSkills(p);
            }

            if (isTransforming)
            {
                transformTimer++;

                float dirX = transformTargetPos.X - p.Center.X;
                syncOffX = dirX;
                int attackDir = dirX >= 0 ? 1 : -1;

                Vector2 posStandAtaque = transformTargetPos - new Vector2(attackDir * distanciaAtaqueX, distanciaAtaqueY);

                if (transformPhase == 1)
                {
                    if (velocidadTransformacion > 0f)
                    {
                        Projectile.Center = Vector2.Lerp(Projectile.Center, posStandAtaque, MathHelper.Clamp(velocidadTransformacion, 0f, 1f));
                    }
                    frame = 4;

                    Vector2 offRot = transformTargetPos - Projectile.Center;
                    if (offRot != Vector2.Zero)
                    {
                        float rot = offRot.ToRotation();
                        if (attackDir < 0) rot += MathHelper.Pi;
                        Projectile.rotation = rot;
                    }

                    if (Vector2.Distance(Projectile.Center, posStandAtaque) < 16f || transformTimer > 20)
                    {
                        Projectile.Center = posStandAtaque;
                        transformPhase = 2;
                        transformTimer = 0;
                    }
                }
                else if (transformPhase == 2)
                {
                    Projectile.Center = posStandAtaque;
                    frame = 4;

                    Vector2 offRot = transformTargetPos - Projectile.Center;
                    if (offRot != Vector2.Zero)
                    {
                        float rot = offRot.ToRotation();
                        if (attackDir < 0) rot += MathHelper.Pi;
                        Projectile.rotation = rot;
                    }

                    if (transformTimer == 1 && isOwner)
                    {
                        pedidoTransformSpawn = true;
                        pedidoTransformSpawnTimer = PEDIDO_TRANSFORM_SPAWN_DURACION;
                        Projectile.netUpdate = true;
                    }

                    if (transformTimer >= 15)
                    {
                        isTransforming = false;
                        activeTransformSkill = 0;
                        transformPhase = 0;
                        transformTimer = 0;
                        Projectile.rotation = 0f;
                        if (isOwner) Projectile.netUpdate = true;
                    }
                }
            }

            if (isOwner)
            {
                bool canAttack = !isTransforming && ParticulasStands.CanAttack(runtime) && (auto ? (target = FindEnemy(p)) != null : Main.mouseLeft);

                state = canAttack ? State.Attack : State.Idle;
                Projectile.friendly = canAttack;

                if (!isTransforming)
                {
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
            }
            else
            {
                if (!isTransforming)
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
            }

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);

            float baseDamage = 15f;
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);

            if (!spawning && !isTransforming)
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
                if (Main.netMode != Terraria.ID.NetmodeID.Server)
                    Main.NewText(auto ? "AutoStand-ON" : "AutoStand-OFF", 255, 255, 0);
                Projectile.netUpdate = true;
            }
        }

        void HandleSkills(Player p)
        {
            if (p.whoAmI != Main.myPlayer) return;
            if (!ParticulasStands.CanUseSkills(runtime)) return;
            if (p.HasBuff(ModContent.BuffType<CD>())) return;
            if (isTransforming) return;

            StandStatsPlayer stats = p.GetModPlayer<StandStatsPlayer>();

            if (JojoKeybinds.SkillF.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown1>()))
            {
                float factorTiempo = Math.Max(0f, 1f - stats.standCooldown1Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown1>(), (int)(700 * factorTiempo));
                p.AddBuff(ModContent.BuffType<CD>(), 60);
                fDash = false;

                float maxRangoF = rangoMaximoHabilidadF;
                if (p.TryGetModPlayer(out RangoStandPlayer rp)) maxRangoF *= rp.multiplicadorRango;

                Vector2 offsetMouse = Main.MouseWorld - p.Center;
                if (offsetMouse.Length() > maxRangoF) offsetMouse = Vector2.Normalize(offsetMouse) * maxRangoF;

                transformTargetPos = p.Center + offsetMouse;
                activeTransformSkill = 1;
                isTransforming = true;
                transformPhase = 1;
                transformTimer = 0;
                Projectile.netUpdate = true;
            }

            if (PlayerInput.Triggers.JustPressed.MouseRight && !p.HasBuff(ModContent.BuffType<Curacion>()))
            {
                p.AddBuff(ModContent.BuffType<CD>(), 60);
                CuracionManager_Tier_1.AplicarCuracion(p, curacionCantidad, curacionCooldownDuracion);

                curacionRedActiva = true;
                curacionRedTimer = CURACION_RED_DURACION;

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
            var data = ParticulasStands.Stands.Golden1;
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
            else if (isTransforming)
            {
                tex = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Tier_1/Tranformar_Tier_1").Value;
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