using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

using Jojo.Content.Buffs.Justice_Buffs;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.Justice.Justice_Tier_4
{
    public class JusticeGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public bool bajoControlMental = false;
        public int insideNieblaTimer = 0;
        public int justiceMarkTimer = 0;
        public int markFrame = 0;
        public int markFrameCounter = 0;
        public int manualAttackCooldown = 0;

        // Fase acumulada para el movimiento "idle" (paseo/vuelo en vaivén) del modo pacífico
        public float idlePhase = 0f;

        // ====================================================================
        // BOSSES PRINCIPALES: nunca se pueden controlar.
        // ====================================================================
        public static bool EsBossPrincipal(NPC npc)
        {
            if (npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type])
                return true;

            if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs)
            {
                NPC cabeza = Main.npc[npc.realLife];
                if (cabeza.boss || NPCID.Sets.ShouldBeCountedAsBoss[cabeza.type])
                    return true;
            }

            if (npc.dontTakeDamage || npc.type == NPCID.TargetDummy)
                return true;

            return false;
        }

        // Variables para el truco de "Suplantación de Identidad" (Spoofing)
        private bool isSpoofing = false;
        private int spoofedPlayerIndex = -1;
        private Vector2 originalPlayerPosition;
        private Vector2 originalPlayerVelocity;

        public override void ResetEffects(NPC npc)
        {
            bajoControlMental = false;
        }

        public override bool CheckActive(NPC npc)
        {
            if (bajoControlMental) return false;
            return base.CheckActive(npc);
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (npc.HasBuff(ModContent.BuffType<ControlMental1>()))
            {
                if (EsBossPrincipal(npc))
                {
                    int buffIndex = npc.FindBuffIndex(ModContent.BuffType<ControlMental1>());
                    if (buffIndex != -1) npc.DelBuff(buffIndex);
                    bajoControlMental = false;
                    return;
                }

                bajoControlMental = true;

                Player owner = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)];
                float standDamageMult = 1f;

                if (owner != null && owner.active)
                {
                    standDamageMult = owner.GetDamage(DamageClass.Generic).Additive * owner.GetDamage(DamageClass.Generic).Multiplicative;

                    DamageClass claseStand = ModContent.GetInstance<ClaseStand>();
                    if (claseStand != null)
                    {
                        standDamageMult *= owner.GetDamage(claseStand).Additive * owner.GetDamage(claseStand).Multiplicative;
                    }
                }

                npc.defense = npc.defDefense * 2;
                npc.damage = (int)(npc.defDamage * 2f * standDamageMult);
            }
        }

        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (bajoControlMental) return false;
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }

        public override bool? CanBeHitByItem(NPC npc, Player player, Item item)
        {
            if (bajoControlMental && player.whoAmI == Main.myPlayer) return false;
            return base.CanBeHitByItem(npc, player, item);
        }

        public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
        {
            if (bajoControlMental && projectile.owner == Main.myPlayer) return false;
            return base.CanBeHitByProjectile(npc, projectile);
        }

        public override bool CanHitNPC(NPC npc, NPC target)
        {
            if (bajoControlMental && !IsValidTarget(target)) return false;
            return base.CanHitNPC(npc, target);
        }

        public static bool IsValidTarget(NPC target)
        {
            if (!target.active || target.friendly || target.dontTakeDamage) return false;
            if (target.lifeMax <= 5) return false;
            if (NPCID.Sets.CountsAsCritter[target.type] || !target.chaseable) return false;
            if (target.GetGlobalNPC<JusticeGlobalNPC>().bajoControlMental) return false;
            return true;
        }

        public override bool PreAI(NPC npc)
        {
            // 1. GESTIÓN DE SEGMENTOS (Worms / Wyverns)
            if (npc.realLife >= 0)
            {
                NPC cabeza = Main.npc[npc.realLife];
                if (cabeza.active)
                {
                    var globalCabeza = cabeza.GetGlobalNPC<JusticeGlobalNPC>();

                    if (npc.HasBuff(ModContent.BuffType<ControlMental1>()) && !cabeza.HasBuff(ModContent.BuffType<ControlMental1>()))
                    {
                        if (!EsBossPrincipal(npc) && !EsBossPrincipal(cabeza))
                        {
                            cabeza.AddBuff(ModContent.BuffType<ControlMental1>(), 3600000);
                            globalCabeza.bajoControlMental = true;
                        }
                        else
                        {
                            int bi = npc.FindBuffIndex(ModContent.BuffType<ControlMental1>());
                            if (bi != -1) npc.DelBuff(bi);
                        }
                    }

                    if (globalCabeza.bajoControlMental)
                    {
                        bajoControlMental = true;
                        npc.friendly = true;
                        npc.chaseable = false;
                        npc.timeLeft = Math.Max(npc.timeLeft, 3000);

                        if (npc.whoAmI != npc.realLife) return true;
                    }
                }
            }

            // 2. ANIMACIÓN DE LA MARCA VISUAL
            if (justiceMarkTimer > 0)
            {
                justiceMarkTimer--;
                if (bajoControlMental) justiceMarkTimer = 0;

                markFrameCounter++;
                if (markFrameCounter >= 6)
                {
                    markFrameCounter = 0;
                    markFrame++;
                    if (markFrame >= 4) markFrame = 0;
                }
            }

            // 3. INFECCIÓN EN LA NIEBLA
            if (!bajoControlMental && !npc.friendly && !npc.boss && npc.lifeMax > 5 && !EsBossPrincipal(npc))
            {
                bool dentroDeNiebla = false;
                foreach (Projectile proj in Main.projectile)
                {
                    if (proj.active && proj.type == ModContent.ProjectileType<Justice_Niebla>())
                    {
                        if (Vector2.Distance(npc.Center, proj.Center) <= Justice_Niebla.AuraRadius)
                        {
                            dentroDeNiebla = true;
                            break;
                        }
                    }
                }

                if (dentroDeNiebla && npc.life < npc.lifeMax)
                {
                    insideNieblaTimer++;
                    if (insideNieblaTimer >= 180)
                    {
                        npc.AddBuff(ModContent.BuffType<ControlMental1>(), 3600000);
                        bajoControlMental = true;

                        npc.lifeMax = npc.lifeMax * 2;
                        npc.life = npc.lifeMax;

                        if (npc.realLife >= 0) Main.npc[npc.realLife].AddBuff(ModContent.BuffType<ControlMental1>(), 3600000);
                    }
                }
                else
                {
                    insideNieblaTimer = 0;
                }
            }

            // 4. LÓGICA DE CONTROL MENTAL ACTIVA
            if (bajoControlMental)
            {
                npc.friendly = true;
                npc.chaseable = false;
                npc.timeLeft = Math.Max(npc.timeLeft, 3000);

                Player owner = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)];
                if (owner == null || !owner.active) return false;

                float distToPlayer = Vector2.Distance(npc.Center, owner.Center);
                if (distToPlayer > 1800f)
                {
                    npc.position = owner.Center - new Vector2(0, 100);
                    return false;
                }

                NPC targetEnemy = null;
                NPC closestEnemy = null;
                float minDist = 1600f;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.active && n.whoAmI != npc.whoAmI && IsValidTarget(n))
                    {
                        if (n.GetGlobalNPC<JusticeGlobalNPC>().justiceMarkTimer > 0)
                        {
                            targetEnemy = n;
                            break;
                        }

                        float dist = Vector2.Distance(npc.Center, n.Center);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestEnemy = n;
                        }
                    }
                }

                if (targetEnemy == null) targetEnemy = closestEnemy;

                // ====================================================================
                // MODO COMBATE
                // ====================================================================
                if (targetEnemy != null)
                {
                    int pIndex = npc.target;
                    if (pIndex < 0 || pIndex >= 255) pIndex = owner.whoAmI;
                    Player p = Main.player[pIndex];

                    originalPlayerPosition = p.position;
                    originalPlayerVelocity = p.velocity;
                    isSpoofing = true;
                    spoofedPlayerIndex = pIndex;

                    p.position = targetEnemy.position;
                    p.velocity = targetEnemy.velocity;

                    if (npc.Hitbox.Intersects(targetEnemy.Hitbox))
                    {
                        if (manualAttackCooldown <= 0)
                        {
                            int hitDirection = Math.Sign(targetEnemy.Center.X - npc.Center.X);
                            if (hitDirection == 0) hitDirection = 1;

                            NPC.HitInfo hit = new NPC.HitInfo
                            {
                                Damage = Math.Max(1, npc.damage),
                                Knockback = 5f,
                                HitDirection = hitDirection
                            };
                            targetEnemy.StrikeNPC(hit);
                            manualAttackCooldown = 5;
                        }
                    }

                    if (manualAttackCooldown > 0) manualAttackCooldown--;

                    return true;
                }

                // ====================================================================
                // MODO PACÍFICO (movimiento propio, SIN IA nativa)
                // ====================================================================
                else
                {
                    idlePhase += 0.035f;
                    if (idlePhase > MathHelper.TwoPi) idlePhase -= MathHelper.TwoPi;

                    bool isWormHead = (npc.aiStyle == 6 || npc.realLife >= 0);

                    if (npc.noGravity)
                    {
                        Vector2 orbitOffset = new Vector2(
                            (float)Math.Cos(idlePhase) * 150f,
                            (float)Math.Sin(idlePhase) * 60f - 70f
                        );
                        Vector2 wanderTarget = owner.Center + orbitOffset;

                        Vector2 toTarget = distToPlayer > 500f
                            ? owner.Center - npc.Center
                            : wanderTarget - npc.Center;

                        if (toTarget.Length() > 1f) toTarget.Normalize();
                        npc.velocity = (npc.velocity * 20f + toTarget * 5f) / 21f;

                        if (isWormHead)
                        {
                            if (npc.velocity.Length() > 0.1f)
                                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                        }
                        else
                        {
                            if (npc.velocity.X != 0) npc.spriteDirection = Math.Sign(npc.velocity.X);
                            float bank = MathHelper.Clamp(-npc.velocity.Y * 0.03f, -0.4f, 0.4f);
                            npc.rotation = npc.rotation.AngleLerp(bank, 0.1f);
                        }
                    }
                    else
                    {
                        float paceOffset = (float)Math.Sin(idlePhase) * 90f;
                        float targetX = owner.Center.X + (distToPlayer > 300f ? 0f : paceOffset);

                        int dirX = Math.Sign(targetX - npc.Center.X);
                        if (Math.Abs(targetX - npc.Center.X) < 6f || dirX == 0)
                        {
                            dirX = npc.direction != 0 ? npc.direction : 1;
                        }

                        npc.direction = dirX;
                        npc.spriteDirection = dirX;

                        // LÓGICA DE ENEMIGOS TERRESTRES MEJORADA (SALTO ALTO)
                        if (npc.velocity.Y == 0)
                        {
                            npc.velocity.X = dirX * (distToPlayer > 300f ? 4f : 2.5f);

                            // Comprueba si el dueño está bastante por encima del NPC
                            bool duenoArriba = owner.Center.Y < npc.Center.Y - 100f;

                            // Si choca con pared, es tipo slime/zombie, o el dueño está volando/arriba: ¡SALTO ALTO!
                            if (npc.collideX || npc.aiStyle == 1 || npc.aiStyle == 41 || duenoArriba)
                            {
                                npc.velocity.Y = -10.5f; // Fuerza de salto enormemente incrementada
                            }
                        }

                        npc.rotation = npc.rotation.AngleLerp(npc.velocity.X * 0.03f, 0.15f);
                    }

                    npc.frameCounter++;
                    if (npc.frameCounter >= 6)
                    {
                        npc.frameCounter = 0;
                        int numFrames = Main.npcFrameCount[npc.type];
                        if (numFrames > 1)
                        {
                            int currentFrameY = npc.frame.Y / npc.frame.Height;
                            currentFrameY = (currentFrameY + 1) % numFrames;
                            npc.frame.Y = currentFrameY * npc.frame.Height;
                        }
                    }

                    return false;
                }
            }

            return base.PreAI(npc);
        }

        public override void PostAI(NPC npc)
        {
            // DESHACER EL SPOOFING DE JUGADOR
            if (isSpoofing && spoofedPlayerIndex != -1)
            {
                Player p = Main.player[spoofedPlayerIndex];
                p.position = originalPlayerPosition;
                p.velocity = originalPlayerVelocity;
                isSpoofing = false;
                spoofedPlayerIndex = -1;
            }
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (bajoControlMental)
            {
                Texture2D texture = ModContent.Request<Texture2D>("Jojo/Content/Buffs/Justice_Buffs/ControlMental1").Value;
                Vector2 drawPos = new Vector2(npc.Center.X, npc.position.Y - 24) - screenPos;
                Vector2 origin = texture.Size() / 2f;
                spriteBatch.Draw(texture, drawPos, null, Color.White, 0f, origin, 1.0f, SpriteEffects.None, 0f);
            }

            if (justiceMarkTimer > 0 && !bajoControlMental)
            {
                Texture2D texture = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/Justice/Justice_Tier_4/Justice_Marca_Tier_4").Value;
                int frameHeight = texture.Height / 4;
                Rectangle sourceRect = new Rectangle(0, markFrame * frameHeight, texture.Width, frameHeight);
                Vector2 origin = new Vector2(texture.Width / 2f, frameHeight / 2f);
                Vector2 drawPos = npc.Center - screenPos;

                spriteBatch.Draw(texture, drawPos, sourceRect, Color.White, 0f, origin, 1.0f, SpriteEffects.None, 0f);
            }
        }
    }
}