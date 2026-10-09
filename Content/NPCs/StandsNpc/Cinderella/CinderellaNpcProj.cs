using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Audio;
using System;
using System.IO;
using System.Collections.Generic;
using Jojo.Content.Buffs;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Habilidades;
using Jojo.Content.Items;
using Jojo.Content.Items.Potenciadores;
using Jojo.Content.Projectiles.StandsNpc;

namespace Jojo.Content.NPCs.StandsNpc.Cinderella
{
    public class StandNPCEnfriamiento_Cinderella : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public int tiempoEsperaStand = 0;

        public override void PostAI(NPC npc)
        {
            if (tiempoEsperaStand > 0) tiempoEsperaStand--;
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write((short)tiempoEsperaStand);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            int nuevoValor = binaryReader.ReadInt16();
            if (Main.netMode == NetmodeID.MultiplayerClient && nuevoValor > 0 && tiempoEsperaStand <= 0)
            {
                SpawnBreakEffects(npc);
                SoundEngine.PlaySound(CinderellaNpcProj.DespawnSound, npc.Center);
            }
            tiempoEsperaStand = nuevoValor;
        }

        public static void SpawnBreakEffects(NPC npc)
        {
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 8; i++)
            {
                Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Electric, 0f, 0f, 100, Color.Blue, 2f);
                d.velocity *= 2f;
                d.noGravity = true;
            }
        }
    }

    public class CinderellaNpcProj : BaseStandNpcProjectile, IStandNpcProjectile
    {
        internal enum StandState { Idle, Attack, Despawning }
        internal StandState state;

        public NPC OwnerNpc
        {
            get
            {
                int index = (int)Projectile.ai[0];
                if (index >= 0 && index < Main.maxNPCs)
                {
                    NPC npc = Main.npc[index];
                    if (npc != null && npc.active) return npc;
                }
                return null;
            }
        }

        private Entity currentTarget;

        private int frame;
        private int animTimer;
        private int attackTimer;
        private int despawnTimer;
        private bool init;

        private int standDirection = 1;

        public int posture = 400;
        public int postureRegenTimer = 0;
        public int hitCooldown = 0;
        public int slowdownTimer = 0;
        private Vector2 pushVelocity = Vector2.Zero;
        private int shakeTimer = 0;

        public static readonly SoundStyle SpawnSound = new SoundStyle("Jojo/Content/Sonidos/Stand_Spawn");
        public static readonly SoundStyle DespawnSound = new SoundStyle("Jojo/Content/Sonidos/Stand_Spawn");
        public static readonly SoundStyle PostureHitSound = SoundID.NPCHit1;

        private const float ThreatRange = 800f;
        private const float DetectionRange = 500f;
        private const float BaseAttackRange = 180f;
        private const float StandTargetRangeBonus = 0f;
        private const float ClashPushForce = 3f;
        private const int ClashSlowdownTicks = 10;
        private const float HealthPenaltyPercent = 0.05f;

        private const float MoveSpeedMultiplier = 1f;
        private const float AttackMoveSpeed = 0.15f;
        private const float IdleFollowSpeed = 0.2f;
        private const float DespawnMoveSpeed = 0.2f;

        private const int AnimTicksAttack = 5;
        private const int AnimTicksIdle = 8;

        private static float Spd(float value) => MathHelper.Clamp(value * MoveSpeedMultiplier, 0f, 1f);

        public const byte StandClashPacketId = 249;
        private int clientReportCooldown = 0;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 88;
            Projectile.height = 80;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            // friendly Y hostile a propósito: CanHitNPC/CanHitPlayer deciden con los bandos del mod.
            Projectile.friendly = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 99999;
            Projectile.alpha = 255;
            Projectile.netImportant = true;

            // CRUCIAL: Marcar como proyectil de NPC para no confundirse con los Stands del jugador
            Projectile.npcProj = true;

            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 7;
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (state != StandState.Attack) return false;

            NPC owner = OwnerNpc;
            if (owner == null) return false;
            if (IAStandNpc.NeverFights(owner)) return false;
            if (target.whoAmI == owner.whoAmI) return false;
            if (target.dontTakeDamage) return false;
            if (!IAStandNpc.AreEnemies(owner, target)) return false;
            return true;
        }

        public override bool CanHitPlayer(Player target)
        {
            if (state != StandState.Attack) return false;

            NPC owner = OwnerNpc;
            if (owner == null) return false;
            if (IAStandNpc.NeverFights(owner)) return false;
            if (IAStandNpc.IsPacific(owner)) return false;
            if (owner.friendly && !target.GetModPlayer<NpcKillerPlayer>().canKillTownNPCs) return false;
            return base.CanHitPlayer(target);
        }

        public override bool CanHitPvp(Player target) => false;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((short)posture);
            writer.Write((short)slowdownTimer);
            writer.Write((short)hitCooldown);
            writer.Write((byte)state);
            writer.Write(pushVelocity.X);
            writer.Write(pushVelocity.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            posture = reader.ReadInt16();
            slowdownTimer = reader.ReadInt16();
            short newHitCooldown = reader.ReadInt16();
            state = (StandState)reader.ReadByte();
            Vector2 receivedPush = new Vector2(reader.ReadSingle(), reader.ReadSingle());

            if (newHitCooldown > hitCooldown)
            {
                pushVelocity = receivedPush;
                HitEffects();
            }
            hitCooldown = newHitCooldown;
        }

        private void UpdateDamageProgression()
        {
            int baseDamage = 20 / 4;                                     // 5  - Sin progresión
            int armorPen = 10;                                           // Penetración de armadura sin progresión

            if (Main.hardMode)
            {
                baseDamage = Math.Max(baseDamage, 15);                   // Muro de Carne
                armorPen = Math.Max(armorPen, 20);
            }
            if (NPC.downedMechBossAny)
            {
                baseDamage = Math.Max(baseDamage, 20);                   // Cualquier mecánico
                armorPen = Math.Max(armorPen, 30);
            }
            if (NPC.downedGolemBoss)
            {
                baseDamage = Math.Max(baseDamage, 30);                   // Gólem
                armorPen = Math.Max(armorPen, 40);
            }

            bool sinPelea = IAStandNpc.NeverFights(OwnerNpc);
            Projectile.damage = sinPelea ? 0 : baseDamage;
            Projectile.ArmorPenetration = sinPelea ? 0 : armorPen;
        }

        public override void AI()
        {
            NPC owner = OwnerNpc;
            bool enEnfriamiento = false;

            if (owner != null && owner.TryGetGlobalNPC(out StandNPCEnfriamiento_Cinderella cooldownNPC))
            {
                if (cooldownNPC.tiempoEsperaStand > 0) enEnfriamiento = true;
            }

            if (owner == null || owner.life <= 0 || enEnfriamiento)
            {
                Projectile.Kill();
                return;
            }

            UpdateDamageProgression();
            SpawnDespawnStandNpc.InitializeSpawn(Projectile, ref init, SpawnSound);

            if (hitCooldown > 0) hitCooldown--;
            if (shakeTimer > 0) shakeTimer--;
            if (slowdownTimer > 0) slowdownTimer--;

            if (hitCooldown <= 0 && posture < 400)
            {
                postureRegenTimer++;
                if (postureRegenTimer >= 6)
                {
                    postureRegenTimer = 0;
                    posture++;
                }
            }

            if (clientReportCooldown > 0) clientReportCooldown--;

            if (hitCooldown <= 0 && clientReportCooldown <= 0) CheckForStandCollisions();

            if (!Projectile.active) return;

            if (pushVelocity != Vector2.Zero)
            {
                Projectile.Center += pushVelocity;
                pushVelocity *= 0.85f;
                if (pushVelocity.LengthSquared() < 0.05f) pushVelocity = Vector2.Zero;
            }

            if (state == StandState.Despawning)
            {
                standDirection = owner.direction;
                SpawnDespawnStandNpc.ProcessDespawn(Projectile, owner, Spd(DespawnMoveSpeed));
                return;
            }

            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= 15;
                if (Projectile.alpha < 0) Projectile.alpha = 0;
            }

            if (IAStandNpc.NeverFights(owner))
            {
                currentTarget = null;
                despawnTimer = 0;
                standDirection = owner.direction;
                ChangeState(StandState.Idle);
                SpawnDespawnStandNpc.ReturnToIdle(Projectile, owner, Spd(IdleFollowSpeed), ref attackTimer);
                Animate();
                return;
            }

            currentTarget = IAStandNpc.FindTarget(owner, Projectile, ThreatRange, DetectionRange);

            if (currentTarget != null)
            {
                despawnTimer = 0;
                float distToTarget = Vector2.Distance(owner.Center, currentTarget.Center);

                if (distToTarget <= DetectionRange)
                {
                    ChangeState(StandState.Attack);
                    standDirection = (currentTarget.Center.X > Projectile.Center.X) ? 1 : -1;

                    float currentAttackRange = (currentTarget is Projectile) ? BaseAttackRange + StandTargetRangeBonus : BaseAttackRange;
                    Vector2 directionToTarget = currentTarget.Center - owner.Center;

                    if (directionToTarget.Length() > currentAttackRange)
                        directionToTarget = Vector2.Normalize(directionToTarget) * currentAttackRange;

                    Vector2 targetPosition = owner.Center + directionToTarget;
                    float currentMoveSpeed = slowdownTimer > 0 ? 0f : Spd(AttackMoveSpeed);

                    if (currentMoveSpeed > 0f)
                        Projectile.Center = Vector2.Lerp(Projectile.Center, targetPosition, currentMoveSpeed);

                    float maxLeashDistance = currentAttackRange + 30f;
                    Vector2 offsetFromOwner = Projectile.Center - owner.Center;
                    if (offsetFromOwner.Length() > maxLeashDistance)
                    {
                        Projectile.Center = owner.Center + Vector2.Normalize(offsetFromOwner) * maxLeashDistance;
                    }

                    StandNpcRotationSystem.ApplyRotation(Projectile, true, directionToTarget, 40f);

                    attackTimer++;
                    if (attackTimer >= 15 && slowdownTimer <= 0)
                    {
                        attackTimer = 0;
                        SoundEngine.PlaySound(new SoundStyle("Terraria/Sounds/Item_1") with { Pitch = 0.05f }, Projectile.Center);
                    }
                }
                else
                {
                    ChangeState(StandState.Idle);
                    standDirection = owner.direction;
                    SpawnDespawnStandNpc.ReturnToIdle(Projectile, owner, Spd(IdleFollowSpeed), ref attackTimer);
                }
            }
            else
            {
                ChangeState(StandState.Idle);
                standDirection = owner.direction;
                SpawnDespawnStandNpc.ReturnToIdle(Projectile, owner, Spd(IdleFollowSpeed), ref attackTimer);
                despawnTimer++;

                if (despawnTimer >= 180)
                {
                    ChangeState(StandState.Despawning);
                    SoundEngine.PlaySound(DespawnSound, Projectile.Center);
                }
            }

            Animate();
        }

        private void ChangeState(StandState newState)
        {
            if (state != newState)
            {
                state = newState;
                if (Main.netMode != NetmodeID.MultiplayerClient) Projectile.netUpdate = true;
            }
        }

        private void CheckForStandCollisions()
        {
            NPC owner = OwnerNpc;
            if (owner == null) return;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];

                if (p.active && p.whoAmI != Projectile.whoAmI && p.damage > 0)
                {
                    bool isEnemyStand = false;

                    if (IAStandNpc.TryGetStandOwner(p, out NPC otherOwner))
                    {
                        if (Main.netMode == NetmodeID.MultiplayerClient) continue;
                        if (otherOwner.whoAmI != owner.whoAmI)
                        {
                            isEnemyStand = IAStandNpc.AreEnemies(owner, otherOwner);
                        }
                    }
                    else if (p.DamageType == ModContent.GetInstance<ClaseStand>() && p.owner >= 0 && p.owner < Main.maxPlayers && !p.npcProj)
                    {
                        bool canDetect = Main.netMode == NetmodeID.SinglePlayer || (Main.netMode == NetmodeID.MultiplayerClient && p.owner == Main.myPlayer);
                        if (!canDetect) continue;

                        if (p.friendly)
                        {
                            Player playerOwner = Main.player[p.owner];
                            bool playerCanHurtNPCs = playerOwner.GetModPlayer<NpcKillerPlayer>().canKillTownNPCs;
                            isEnemyStand = (!owner.friendly) || (owner.friendly && playerCanHurtNPCs);
                        }
                    }

                    if (isEnemyStand && Projectile.Hitbox.Intersects(p.Hitbox))
                    {
                        Vector2 pushDir = Projectile.Center - p.Center;
                        if (pushDir == Vector2.Zero) pushDir = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f));
                        pushDir.Normalize();

                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            pushVelocity = pushDir * ClashPushForce;
                            hitCooldown = 15;
                            slowdownTimer = ClashSlowdownTicks;
                            ReportClashToServer(p.damage, pushDir);
                        }
                        else
                        {
                            TakePostureDamage(p.damage, pushDir);
                        }
                        break;
                    }
                }
            }
        }

        private void ReportClashToServer(int damage, Vector2 pushDir)
        {
            clientReportCooldown = 15;
            HitEffects();

            ModPacket packet = Mod.GetPacket();
            packet.Write(StandClashPacketId);
            packet.Write((short)Projectile.whoAmI);
            packet.Write((short)damage);
            packet.Write(pushDir.X);
            packet.Write(pushDir.Y);
            packet.Send();
        }

        public static void HandleClashPacket(BinaryReader reader)
        {
            int projId = reader.ReadInt16();
            int damage = reader.ReadInt16();
            Vector2 pushDir = new Vector2(reader.ReadSingle(), reader.ReadSingle());

            if (Main.netMode != NetmodeID.Server) return;
            if (projId < 0 || projId >= Main.maxProjectiles) return;

            Projectile proj = Main.projectile[projId];
            if (!proj.active || !(proj.ModProjectile is CinderellaNpcProj stand)) return;
            if (stand.hitCooldown > 0) return;

            damage = System.Math.Clamp(damage, 1, 400);

            if (float.IsNaN(pushDir.X) || float.IsNaN(pushDir.Y) || float.IsInfinity(pushDir.X) || float.IsInfinity(pushDir.Y) || pushDir.LengthSquared() < 0.0001f)
                pushDir = Vector2.Zero;
            else
                pushDir.Normalize();

            stand.TakePostureDamage(damage, pushDir);
        }

        private void HitEffects()
        {
            if (Main.netMode == NetmodeID.Server) return;
            shakeTimer = 15;
            SoundEngine.PlaySound(PostureHitSound, Projectile.Center);

            for (int d = 0; d < 4; d++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.Center, 10, 10, DustID.Electric,
                    Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f), 100, Color.Blue, 1.5f);
                dust.noGravity = true;
            }
        }

        private void TakePostureDamage(int damage, Vector2 pushDir)
        {
            posture -= damage;
            hitCooldown = 15;
            slowdownTimer = ClashSlowdownTicks;
            pushVelocity = pushDir * ClashPushForce;

            HitEffects();
            Projectile.netUpdate = true;

            if (posture <= 0) BreakPosture();
        }

        private void BreakPosture()
        {
            NPC owner = OwnerNpc;
            if (owner != null)
            {
                if (owner.TryGetGlobalNPC(out StandNPCEnfriamiento_Cinderella cooldownNPC))
                    cooldownNPC.tiempoEsperaStand = 120;

                int healthPenalty = System.Math.Max(1, (int)(owner.lifeMax * HealthPenaltyPercent));
                bool oldDontTakeDamage = owner.dontTakeDamage;
                owner.dontTakeDamage = false;

                bool guardarIA = owner.friendly || owner.townNPC;
                float[] aiGuardada = guardarIA ? (float[])owner.ai.Clone() : null;
                Vector2 velGuardada = owner.velocity;
                int dirGuardada = owner.direction;

                NPC.HitInfo hit = owner.CalculateHitInfo(healthPenalty, 0, false, 0f);
                hit.Damage = healthPenalty;
                owner.StrikeNPC(hit);

                owner.dontTakeDamage = oldDontTakeDamage;

                if (owner.active)
                {
                    owner.velocity = velGuardada;
                    owner.direction = dirGuardada;
                    if (aiGuardada != null)
                    {
                        for (int a = 0; a < aiGuardada.Length && a < owner.ai.Length; a++)
                            owner.ai[a] = aiGuardada[a];
                    }

                    owner.netUpdate = true;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, owner.whoAmI);
                }
                StandNPCEnfriamiento_Cinderella.SpawnBreakEffects(owner);
            }

            if (Main.netMode != NetmodeID.Server) SoundEngine.PlaySound(DespawnSound, Projectile.Center);

            Projectile.netUpdate = true;
            Projectile.Kill();
        }

        private void Animate()
        {
            animTimer++;
            int speed = state == StandState.Attack ? AnimTicksAttack : AnimTicksIdle;

            if (animTimer >= speed)
            {
                animTimer = 0;
                if (state == StandState.Idle || state == StandState.Despawning)
                {
                    frame++;
                    if (frame > 3) frame = 0;
                }
                else
                {
                    if (frame < 4) frame = 4;
                    frame++;
                    if (frame > 7) frame = 4;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            NPC owner = OwnerNpc;
            if (owner == null) return false;

            Color drawColor = Projectile.GetAlpha(lightColor);
            Vector2 shakeOffset = Vector2.Zero;
            if (shakeTimer > 0)
            {
                shakeOffset = new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f));
            }

            SpriteEffects effects = SpriteEffects.None;
            if (state == StandState.Idle || state == StandState.Despawning)
            {
                if (standDirection == 1) effects = SpriteEffects.FlipHorizontally;
            }
            else if (state == StandState.Attack)
            {
                if (standDirection == -1) effects = SpriteEffects.FlipHorizontally;
            }

            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            int frameWidth = 88;
            int frameHeight = 80;
            Rectangle rect = new Rectangle(frame * frameWidth, 0, frameWidth, frameHeight);
            Vector2 origin = new Vector2(frameWidth / 2f, frameHeight / 2f);

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition + shakeOffset, rect, drawColor, Projectile.rotation, origin, Projectile.scale, effects, 0);
            return false;
        }
    }
}