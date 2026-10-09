using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Microsoft.Xna.Framework.Graphics;
using Jojo.Content.Buffs;          
using Jojo.Content.Clases;
using Jojo.Content.UI;
using Jojo.Content.Buffs.KillerQueen_Buffs;

namespace Jojo.Content.Projectiles.Minions
{
    public class SHA_Minion_Tier_4 : ModProjectile
    {
        private bool ghostMode = false;
        private int stuckTimer = 0;
        private bool flying = false;
        private int attackCooldown = 0;

        public override string Texture =>
            "Jojo/Content/Projectiles/Minions/SHA_Minion_Tier_4";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 24;
            Projectile.tileCollide = true;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 999999;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            bool megumin = StandSlotSystem.HasMeguminSkinFor(owner);

            string texturePath = megumin
                ? "Jojo/Content/Projectiles/Minions/Skins/Megumin/SHA_Minion_Megumin"
                : "Jojo/Content/Projectiles/Minions/SHA_Minion_Tier_4";

            Texture2D texture = ModContent.Request<Texture2D>(texturePath).Value;

            Rectangle frame = texture.Frame();

            SpriteEffects effects =
                Projectile.spriteDirection == -1
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                lightColor,
                Projectile.rotation,
                frame.Size() / 2f,
                Projectile.scale,
                effects,
                0
            );

            return false;
        }

        public override bool? CanCutTiles() => false;
        public override bool MinionContactDamage() => false;

        private bool IsOnGround()
        {
            Vector2 checkPos = new Vector2(
                Projectile.position.X,
                Projectile.position.Y + Projectile.height
            );

            return Collision.SolidCollision(checkPos, Projectile.width, 4)
                && Projectile.velocity.Y >= 0f;
        }

        private bool IsTargetAerial(Vector2 targetCenter)
        {
            Vector2 checkPos = new Vector2(targetCenter.X - 8f, targetCenter.Y + 16f);
            return !Collision.SolidCollision(checkPos, 16, 4);
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            if (!owner.active || owner.dead)
            {
                owner.ClearBuff(ModContent.BuffType<SHA_Buff_Tier_4>());
                return;
            }

            if (owner.HasBuff(ModContent.BuffType<SHA_Buff_Tier_4>()))
                Projectile.timeLeft = 2;

            attackCooldown++;

            AISearchForTarget(owner, out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter);
            AIMovement(owner, foundTarget, distanceFromTarget, targetCenter);
            AIManualAttack(foundTarget);
            AIUpdateAnimation();
        }

        private void AIManualAttack(bool foundTarget)
        {
            if (!foundTarget) return;
            if (attackCooldown < 25) return;

            attackCooldown = 0;

            Player owner = Main.player[Projectile.owner];

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;

                float dist = Vector2.Distance(npc.Center, Projectile.Center);
                if (dist > 70f) continue;

                float variation = Main.rand.NextFloat(0.9f, 1.1f);
                float bDamage = Projectile.damage * variation;

                int finalDamage = (int)owner
                    .GetDamage(ModContent.GetInstance<ClaseStand>())
                    .ApplyTo(bDamage);

                npc.StrikeNPC(new NPC.HitInfo
                {
                    Damage = finalDamage,
                    Knockback = 0f,
                    HitDirection = Math.Sign(npc.Center.X - Projectile.Center.X),
                    Crit = Main.rand.NextBool()
                });

                npc.AddBuff(BuffID.OnFire, 180);

                Vector2 pos = npc.Center;

                // Mismos efectos visuales que la explosión de BOMBA1_Tier_4
                PlayBomba1StyleExplosion(pos);

                for (int k = 0; k < Main.maxNPCs; k++)
                {
                    NPC areaNpc = Main.npc[k];

                    if (!areaNpc.active || areaNpc.friendly || areaNpc.dontTakeDamage || areaNpc == npc)
                        continue;

                    float areaDist = Vector2.Distance(areaNpc.Center, pos);
                    if (areaDist < 90f)
                    {
                        float explosionVariation = Main.rand.NextFloat(0.85f, 1.15f);
                        int explosionDamage = (int)((Projectile.damage / 2f) * explosionVariation);

                        areaNpc.StrikeNPC(new NPC.HitInfo
                        {
                            Damage = explosionDamage,
                            Knockback = 0f,
                            HitDirection = Math.Sign(areaNpc.Center.X - pos.X),
                            Crit = Main.rand.NextBool()
                        });
                    }
                }

                break;
            }
        }

        // Réplica exacta de los efectos visuales de BOMBA1_Tier_4.OnKill,
        // reutilizados aquí para que la explosión del minion SHA se vea igual.
        private void PlayBomba1StyleExplosion(Vector2 center)
        {
            SoundEngine.PlaySound(SoundID.Item14, center);

            // 1. CORTINA DE HUMO MASIVA
            int smokeParticles = 135;
            for (int i = 0; i < smokeParticles; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(9.0f, 9.0f);
                Dust smoke = Dust.NewDustDirect(center, 0, 0, DustID.Smoke,
                    velocity.X, velocity.Y, 130, Color.DarkGray, Main.rand.NextFloat(2.5f, 3.8f));

                smoke.noGravity = true;
                smoke.fadeIn = 1.4f;
            }

            // 2. FUEGO ALEATORIO SUELTO EXPANDIDO
            int scatterFire = 80;
            for (int i = 0; i < scatterFire; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(8.5f, 8.5f);
                Dust fire = Dust.NewDustDirect(center, 0, 0, DustID.Torch,
                    velocity.X, velocity.Y, 100, default, Main.rand.NextFloat(2.0f, 3.0f));

                fire.noGravity = true;
                fire.velocity = velocity * 1.35f;
            }

            // 3. ARO DE FUEGO PERFECTO
            int fireRingParticles = 90;
            float ringSpeed = 6.8f;

            for (int i = 0; i < fireRingParticles; i++)
            {
                float angle = i * (MathHelper.TwoPi / fireRingParticles);
                Vector2 velocity = angle.ToRotationVector2() * ringSpeed;

                Dust ringDust = Dust.NewDustDirect(center, 0, 0, DustID.Torch,
                    velocity.X, velocity.Y, 100, default, Main.rand.NextFloat(1.8f, 2.5f));

                ringDust.noGravity = true;
                ringDust.velocity = velocity;
            }

            // 4. Escombros físicos (Gore)
            for (int g = 0; g < 3; g++)
            {
                Terraria.Gore.NewGore(
                    Projectile.GetSource_Death(),
                    center,
                    new Vector2(Main.rand.NextFloat(-3.0f, 3.0f), Main.rand.NextFloat(-3.0f, 3.0f)),
                    Main.rand.Next(61, 64),
                    1.1f
                );
            }
        }

        private void AISearchForTarget(
            Player owner,
            out bool foundTarget,
            out float distanceFromTarget,
            out Vector2 targetCenter)
        {
            distanceFromTarget = 700f;
            targetCenter = Projectile.position;
            foundTarget = false;

            float bestDist = 500f;
            Vector2 bestPos = Vector2.Zero;
            bool hasAny = false;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;

                float dist = Vector2.Distance(npc.Center, Projectile.Center);
                if (dist < bestDist) { bestDist = dist; bestPos = npc.Center; hasAny = true; }
            }

            if (owner.HasMinionAttackTargetNPC &&
                owner.MinionAttackTargetNPC >= 0 &&
                owner.MinionAttackTargetNPC < Main.maxNPCs)
            {
                NPC npc = Main.npc[owner.MinionAttackTargetNPC];
                if (npc.active)
                {
                    float dist = Vector2.Distance(npc.Center, Projectile.Center);
                    if (dist < 1200f)
                    {
                        distanceFromTarget = dist;
                        targetCenter = npc.Center;
                        foundTarget = true;
                        return;
                    }
                }
            }

            if (hasAny)
            {
                distanceFromTarget = bestDist;
                targetCenter = bestPos;
                foundTarget = true;
            }
        }

        private void AIMovement(
            Player owner,
            bool foundTarget,
            float distanceFromTarget,
            Vector2 targetCenter)
        {
            float gravity = 0.4f;
            float maxFall = 12f;
            float speed = 4f;
            float jumpSpeed = -9.5f;
            float flySpeed = 6f;

            float distanceToOwner = Vector2.Distance(owner.Center, Projectile.Center);
            bool onGround = IsOnGround();
            bool targetIsAerial = foundTarget && IsTargetAerial(targetCenter);
            bool targetCloseEnoughToFly = foundTarget && distanceFromTarget < 500f;

            if (targetIsAerial && targetCloseEnoughToFly)
                flying = true;
            else if (!foundTarget || !targetIsAerial)
            {
                if (flying && onGround) flying = false;
                else if (!foundTarget) flying = false;
            }

            bool barelyMoving = Math.Abs(Projectile.velocity.X) < 0.5f;
            bool tryingToMove = distanceToOwner > 200f;

            if (!flying && onGround && barelyMoving && tryingToMove)
                stuckTimer++;
            else
                stuckTimer = 0;

            if (distanceToOwner > 400f || stuckTimer > 120)
                ghostMode = true;

            if (ghostMode && distanceToOwner < 180f)
                ghostMode = false;

            Projectile.tileCollide = !ghostMode && !flying;

            if (ghostMode)
            {
                Vector2 dir = owner.Center - Projectile.Center;
                float dist = dir.Length();
                if (dist > 0f)
                {
                    dir.Normalize();
                    dir *= 15f;
                    Projectile.velocity = (Projectile.velocity * 10f + dir) / 11f;
                }
                return;
            }

            if (flying && foundTarget)
            {
                Vector2 dir = targetCenter - Projectile.Center;
                float dist = dir.Length();
                if (dist > 0f)
                {
                    dir.Normalize();
                    dir *= flySpeed;
                    Projectile.velocity = (Projectile.velocity * 10f + dir) / 11f;
                }
                return;
            }

            if (!flying)
            {
                Projectile.velocity.Y += gravity;
                if (Projectile.velocity.Y > maxFall) Projectile.velocity.Y = maxFall;
            }

            Vector2 toOwner = owner.Center - Projectile.Center;

            if (Math.Abs(toOwner.X) > 40f)
            {
                float moveDir = Math.Sign(toOwner.X);
                Projectile.velocity.X = (Projectile.velocity.X * 10f + moveDir * speed) / 11f;
            }
            else
            {
                Projectile.velocity.X *= 0.85f;
            }

            if (onGround && toOwner.Y < -50f)
                Projectile.velocity.Y = jumpSpeed;
        }

        private void AIUpdateAnimation()
        {
            Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
            Projectile.rotation = Projectile.velocity.X * 0.05f;
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.65f);
        }
    }
}