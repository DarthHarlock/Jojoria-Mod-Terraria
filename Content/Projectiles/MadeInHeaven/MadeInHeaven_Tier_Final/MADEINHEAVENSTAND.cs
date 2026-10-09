using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using System;
using Jojo.Systems.StandFuncionComunes;
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
using Jojo.Content.Buffs.MadeInHeaven_Buffs;

namespace Jojo.Content.Projectiles.MadeInHeaven.MadeInHeaven_Tier_Final
{
    public class MADEINHEAVENSTAND : ModProjectile
    {
        enum State { Idle, Attack }
        State state;

        bool auto, init, dying;
        NPC target;

        int frame, animT, cd;
        int attackTimer, swingSoundTimer;

        float manR = 160f, autoR = 160f;
        float innerRadius = 90f;

        ParticulasStands.StandRuntime runtime = new();

        public int baseCritChance = 5;
        public int attackSpeed = 1;

        // --- Click derecho para Skill F ---
        bool rightClickPrev;
        bool rightClickJustPressed;

        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");
        static readonly SoundStyle SwingSoundBase = new("Terraria/Sounds/Item_1");

        float syncOffX, syncOffY;

        // --- ARREGLO: posición absoluta sincronizada (elimina el parpadeo A->B->A) ---
        float syncCenterX, syncCenterY;
        bool centerSynced = false;

        int netTimer = 0;

        const int TrailLength = 8;
        const float TrailMaxAlpha = 0.35f;
        Vector2[] trailPos = new Vector2[TrailLength];
        float[] trailRot = new float[TrailLength];
        int[] trailFrame = new int[TrailLength];
        bool[] trailFlip = new bool[TrailLength];
        bool[] trailActive = new bool[TrailLength];

        void UpdateTrail()
        {
            for (int i = TrailLength - 1; i > 0; i--)
            {
                trailPos[i] = trailPos[i - 1];
                trailRot[i] = trailRot[i - 1];
                trailFrame[i] = trailFrame[i - 1];
                trailFlip[i] = trailFlip[i - 1];
                trailActive[i] = trailActive[i - 1];
            }

            trailPos[0] = Projectile.Center;
            trailRot[0] = Projectile.rotation;
            trailFrame[0] = frame;
            trailFlip[0] = syncOffX < 0;
            // ARREGLO: Las sombras SOLO se crean cuando el stand está en estado de Ataque
            trailActive[0] = (state == State.Attack);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)state);
            writer.Write(auto);
            writer.Write(syncOffX);
            writer.Write(syncOffY);

            // ARREGLO: enviamos la posición ABSOLUTA ya calculada por el dueño,
            // en vez de dejar que cada cliente la re-derive con su propia copia
            // (posiblemente desincronizada) de la posición del jugador dueño.
            writer.Write(Projectile.Center.X);
            writer.Write(Projectile.Center.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            state = (State)reader.ReadByte();
            auto = reader.ReadBoolean();
            syncOffX = reader.ReadSingle();
            syncOffY = reader.ReadSingle();

            syncCenterX = reader.ReadSingle();
            syncCenterY = reader.ReadSingle();
            centerSynced = true;
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
            if (s < 50f) { attackSpeed = 1; Projectile.localNPCHitCooldown = 5; }
            else if (s < 100f) { attackSpeed = 2; Projectile.localNPCHitCooldown = 4; }
            else if (s < 150f) { attackSpeed = 3; Projectile.localNPCHitCooldown = 3; }
            else { attackSpeed = 4; Projectile.localNPCHitCooldown = 2; }
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

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];

            if (Projectile.owner == Main.myPlayer)
            {
                rightClickJustPressed = Main.mouseRight && !rightClickPrev && !Main.mouseText;
                rightClickPrev = Main.mouseRight;
            }

            UpdateAttackSpeed(p);

            if (dying) { ParticulasStands.Despawn(Projectile, p); return; }

            if (!p.active || (p.dead && !p.HasBuff(ModContent.BuffType<ResetUniversal>())))
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
                ParticulasStands.Spawn(p, ParticulasStands.Stands.MadeInHeavenParticulas);
            }

            var data = ParticulasStands.Stands.MadeInHeavenParticulas;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);

            bool spawning = runtime.spawning;
            bool isOwner = Projectile.owner == Main.myPlayer;

            // --- VISUALES MULTIJUGADOR Y VELOCIDAD ---
            if (!spawning)
            {
                // El rastro de clones lo ven TODOS los jugadores (sincronizado)
                p.armorEffectDrawShadow = true;

                // ARREGLO: Aumentar la velocidad del jugador un 50% mientras el stand esté fuera
                p.moveSpeed += 0.50f;
                p.maxRunSpeed *= 1.50f;
                p.runAcceleration *= 1.50f;

                // Informamos al MIHDashPlayer para que aplique la velocidad física
                MIHDashPlayer modPlayer = p.GetModPlayer<MIHDashPlayer>();
                modPlayer.mihActive = true;
                modPlayer.mihUltimate = p.HasBuff(ModContent.BuffType<ResetUniversal>());

                if (isOwner)
                {
                    HandleSkills(p);
                    HandleToggle(p);
                }
            }

            auto = Projectile.ai[0] == 1f;

            if (isOwner)
            {
                bool canAttack = ParticulasStands.CanAttack(runtime) && (auto ? (target = FindEnemy(p)) != null : Main.mouseLeft);

                state = canAttack ? State.Attack : State.Idle;
                Projectile.friendly = canAttack;

                Vector2 off = GetOffset(p);

                float multRango = 1f;
                if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer)) multRango = rangoPlayer.multiplicadorRango;

                float max = (auto ? autoR : manR) * multRango;

                if (off.Length() > max) off = Vector2.Normalize(off) * max;

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
                    if (attackTimer >= delay) { attackTimer = 0; Projectile.friendly = true; }
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

                if (++netTimer >= 5) { netTimer = 0; Projectile.netUpdate = true; }
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

                // ARREGLO: usamos la posición absoluta ya sincronizada por el dueño
                // (llega en el MISMO paquete que off/state), en vez de recalcular con
                // la copia local -y potencialmente desactualizada- del jugador remoto.
                // Esto elimina el parpadeo A->B->A tras un dash/teleport (Skill F).
                if (centerSynced)
                {
                    Vector2 target = new Vector2(syncCenterX, syncCenterY);
                    Projectile.Center = Vector2.Lerp(Projectile.Center, target, 0.25f);
                }
                else
                {
                    // Fallback SOLO para los primerísimos frames, antes de recibir el primer paquete
                    ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);
                }
            }

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);
            float baseDamage = 80f;
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);

            if (!spawning) Animate();
            if (!spawning) UpdateTrail();
        }

        Vector2 GetOffset(Player p)
        {
            if (state == State.Attack)
                return auto && target != null ? target.Center - p.Center : Main.MouseWorld - p.Center;
            return new Vector2(-40 * p.direction, -10);
        }

        void HandleToggle(Player p)
        {
            if (cd-- > 0 || p.whoAmI != Main.myPlayer) return;

            if (p.GetModPlayer<MIHDashPlayer>().isOmniDashing) return;

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

            if (p.GetModPlayer<MIHDashPlayer>().isOmniDashing) return;

            StandStatsPlayer stats = p.GetModPlayer<StandStatsPlayer>();

            // Skill F
            if ((JojoKeybinds.SkillF.JustPressed || rightClickJustPressed) && !p.HasBuff(ModContent.BuffType<Cooldown1>()))
            {
                float factorTiempoF = Math.Max(0f, 1f - stats.standCooldown1Reduction);

                if (p.HasBuff(ModContent.BuffType<ResetUniversal>()))
                {
                    factorTiempoF *= 0.5f;
                    factorTiempoF -= MadeInHeavenTimeSystem.TotalCooldown1ReductionPercent;
                    factorTiempoF = Math.Max(0f, factorTiempoF);
                }

                p.AddBuff(ModContent.BuffType<Cooldown1>(), (int)(150 * factorTiempoF));
                p.AddBuff(ModContent.BuffType<CD>(), 1);

                Vector2 target = Main.MouseWorld;
                int dashDamage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(300f); //DASH de ma muerte

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), p.Center, Vector2.Zero,
                    ModContent.ProjectileType<MIHDashSkill>(), dashDamage, 8f, p.whoAmI, 0, target.X, target.Y
                );
            }

            // Skill G
            if (JojoKeybinds.SkillG.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown2>()))
            {
                float factorTiempoG = Math.Max(0f, 1f - stats.standCooldown2Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown2>(), (int)(700 * factorTiempoG));
                p.AddBuff(ModContent.BuffType<CD>(), 60);

                int dashDamage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(100f); //DASH habilida ''G''

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), p.Center, Vector2.Zero,
                    ModContent.ProjectileType<MIH_OmnidirectionalDash>(), dashDamage, 0f, p.whoAmI, p.Center.X, p.Center.Y
                );
            }

            // Skill H
            if (JojoKeybinds.SkillH.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown3>()) && !p.HasBuff(ModContent.BuffType<ResetUniversal>()))
            {
                float factorTiempoH = Math.Max(0f, 1f - stats.standCooldown3Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown3>(), (int)(10800 * factorTiempoH));
                p.AddBuff(ModContent.BuffType<CD>(), 60);

                p.AddBuff(ModContent.BuffType<ResetUniversal>(), 1200);
                SoundEngine.PlaySound(new SoundStyle("Jojo/Content/Sonidos/GravedadInvertida") with { Volume = 0.9f }, p.Center);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), p.Center, Vector2.Zero,
                    ModContent.ProjectileType<MadeInHeaven_UltimateField>(), 0, 0f, p.whoAmI
                );
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
            frame = state == State.Idle ? (frame + 1) % 4 : (frame < 4 || frame > 7 ? 4 : (frame + 1 > 7 ? 4 : frame + 1));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player p = Main.player[Projectile.owner];
            var data = ParticulasStands.Stands.MadeInHeavenParticulas;
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
                r = new Rectangle(frame * 88, 0, 88, 76);
                o = new Vector2(44, 49);
            }

            if (!spawning)
            {
                Texture2D idleTex = ModContent.Request<Texture2D>(data.IdleTexture).Value;

                for (int i = TrailLength - 1; i >= 0; i--)
                {
                    if (!trailActive[i]) continue;

                    float progress = 1f - i / (float)TrailLength;
                    float trailAlpha = progress * TrailMaxAlpha;

                    Rectangle tr = new Rectangle(trailFrame[i] * 88, 0, 88, 80); //alto
                    Vector2 to = new Vector2(44, 49);

                    Main.EntitySpriteDraw(
                        idleTex, trailPos[i] - Main.screenPosition, tr,
                        Color.White * trailAlpha, trailRot[i], to, Projectile.scale,
                        trailFlip[i] ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f
                    );
                }
            }

            bool flipVisual = syncOffX < 0;

            Main.EntitySpriteDraw(
                tex, Projectile.Center - Main.screenPosition, r,
                lightColor * ((255 - Projectile.alpha) / 255f),
                Projectile.rotation, o, Projectile.scale,
                flipVisual ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f
            );

            return false;
        }
    }
}