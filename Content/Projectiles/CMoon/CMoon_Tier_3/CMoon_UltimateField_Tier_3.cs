using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace Jojo.Content.Projectiles.CMoon.CMoon_Tier_3
{
    // PROYECTIL DEL CAMPO DE FUERZA ESPACIAL (HABILIDAD H)
    public class CMoon_UltimateField_Tier_3 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public float Radius => 1000f;

        public const int DurationTicks = 600; // 600 ticks = 10 segundos

        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = DurationTicks;
            Projectile.hide = true;
        }

        public override void AI()
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(Radius, Radius);
                Dust ringDust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.GreenFairy, Vector2.Zero, 100, Color.LimeGreen, 1.5f);
                ringDust.noGravity = true;
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(Radius, Radius);
                Dust starDust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.MagicMirror, Main.rand.NextVector2Circular(1f, 1f), 100, Color.White, 1.2f);
                starDust.noGravity = true;
            }

            // Detectar NPCs
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.type != NPCID.TargetDummy && !CMoonSkillHandler_Tier_3.TiposMoonLord.Contains(npc.type))
                {
                    if (Vector2.Distance(Projectile.Center, npc.Center) <= Radius)
                    {
                        // FIX BUG: si la Habilidad F (Gravedad Invertida) tiene control activo
                        // sobre este NPC (el jugador lo está levantando/estampando a propósito),
                        // el campo de Gravedad Cero NO debe reclamarlo. Antes de este fix, el
                        // campo llamaba a ActivateZeroGravity() sobre TODOS los NPCs en su radio
                        // cada tick, incluso los que la F acababa de empezar a controlar,
                        // cancelándola de inmediato — por eso la F "no hacía nada" mientras el
                        // campo H estaba activo. Ahora simplemente lo dejamos en paz mientras
                        // la F lo tenga, y el campo lo vuelve a atrapar en cuanto ella lo suelte.
                        if (npc.GetGlobalNPC<CMoonGravityNPC_Tier_3>().affectedByCMoon)
                            continue;

                        var globalNPC = npc.GetGlobalNPC<CMoonZeroGravityNPC_Tier3>();
                        globalNPC.ActivateZeroGravity(npc);
                    }
                }
            }

            // Detectar Proyectiles Enemigos
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.hostile && !proj.friendly)
                {
                    if (Vector2.Distance(Projectile.Center, proj.Center) <= Radius)
                    {
                        // FIX BUG: mismo criterio que con los NPCs.
                        if (proj.GetGlobalProjectile<CMoonGravityProjectile_Tier_3>().affectedByCMoon)
                            continue;

                        var globalProj = proj.GetGlobalProjectile<CMoonZeroGravityProjectile_Tier3>();
                        globalProj.ActivateZeroGravity(proj);
                    }
                }
            }

            // Detectar Jugadores Enemigos (PvP)
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player targetPlayer = Main.player[i];
                if (targetPlayer.active && !targetPlayer.dead && targetPlayer.whoAmI != Projectile.owner)
                {
                    Player ownerPlayer = Main.player[Projectile.owner];
                    if (ownerPlayer.hostile && targetPlayer.hostile && (ownerPlayer.team == 0 || ownerPlayer.team != targetPlayer.team))
                    {
                        if (Vector2.Distance(Projectile.Center, targetPlayer.Center) <= Radius)
                        {
                            // FIX BUG: mismo criterio que con los NPCs.
                            if (targetPlayer.GetModPlayer<CMoonGravityPlayer_Tier_3>().affectedByCMoon)
                                continue;

                            targetPlayer.GetModPlayer<CMoonZeroGravityPlayer_Tier3>().ActivateZeroGravity(targetPlayer);
                        }
                    }
                }
            }
        }
    }

    // LÓGICA GLOBAL PARA NPCs AFECTADOS (Habilidad H) — EXCLUSIVA DE TIER 3
    public class CMoonZeroGravityNPC_Tier3 : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public int zeroGravityTimer = 0;

        public bool wasInZeroGravity = false;
        public bool cachedNoGravity = false;

        public void ActivateZeroGravity(NPC npc)
        {
            // FIX BUG: si el NPC estaba siendo "estampado" por la Habilidad F (Gravedad
            // Invertida), cancelamos ese efecto de forma segura ANTES de activar Gravedad
            // Cero. Antes de este fix, ambos sistemas quedaban activos a la vez sobre el
            // mismo NPC (por ejemplo al usar F y luego H mientras el enemigo aún caía),
            // y cada uno pisaba la velocidad/gravedad/rotación del otro cada tick —
            // eso era lo que provocaba el movimiento errático y que el NPC saliera
            // disparado fuera del mapa hasta despawnear.
            npc.GetGlobalNPC<CMoonGravityNPC_Tier_3>().CancelSilently(npc);

            zeroGravityTimer = 5;
        }

        /// <summary>
        /// FIX BUG: cancela la Gravedad Cero (H) de forma "silenciosa" sobre este NPC,
        /// restaurando su noGravity/rotation original tal como lo haría el fin natural
        /// del efecto, pero SIN esperar a que el timer llegue a 0 y sin aplicar ningún
        /// golpe extra. Se usa cuando la Habilidad F necesita tomar control exclusivo
        /// del NPC para poder levantarlo y estamparlo aunque el campo H siga activo.
        /// </summary>
        public void CancelSilently(NPC npc)
        {
            if (zeroGravityTimer <= 0 && !wasInZeroGravity) return;

            zeroGravityTimer = 0;

            if (wasInZeroGravity)
            {
                npc.noGravity = cachedNoGravity;

                if (npc.type != NPCID.WallofFlesh && npc.type != NPCID.WallofFleshEye)
                {
                    npc.rotation = 0f;
                    npc.velocity.X *= 0.2f;
                }

                wasInZeroGravity = false;
            }

            if (Main.netMode != NetmodeID.SinglePlayer)
                npc.netUpdate = true;
        }

        private bool IsWormBody(NPC npc)
        {
            if (npc.realLife >= 0 && npc.realLife != npc.whoAmI) return true;
            return npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail ||
                   npc.type == NPCID.DevourerBody || npc.type == NPCID.DevourerTail ||
                   npc.type == NPCID.DiggerBody || npc.type == NPCID.DiggerTail ||
                   npc.type == NPCID.TombCrawlerBody || npc.type == NPCID.TombCrawlerTail ||
                   npc.type == NPCID.BoneSerpentBody || npc.type == NPCID.BoneSerpentTail ||
                   npc.type == NPCID.WyvernBody || npc.type == NPCID.WyvernBody2 || npc.type == NPCID.WyvernBody3 || npc.type == NPCID.WyvernLegs || npc.type == NPCID.WyvernTail ||
                   npc.type == NPCID.StardustWormBody || npc.type == NPCID.StardustWormTail ||
                   npc.type == NPCID.LeechBody || npc.type == NPCID.LeechTail ||
                   npc.type == NPCID.SolarCrawltipedeBody || npc.type == NPCID.SolarCrawltipedeTail ||
                   npc.type == NPCID.DuneSplicerBody || npc.type == NPCID.DuneSplicerTail ||
                   npc.type == NPCID.BloodEelBody || npc.type == NPCID.BloodEelTail;
        }

        private bool IsWormHead(NPC npc)
        {
            if (npc.realLife >= 0 && npc.realLife == npc.whoAmI) return true;
            return npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.DevourerHead ||
                   npc.type == NPCID.DiggerHead || npc.type == NPCID.TombCrawlerHead ||
                   npc.type == NPCID.BoneSerpentHead || npc.type == NPCID.WyvernHead ||
                   npc.type == NPCID.StardustWormHead || npc.type == NPCID.LeechHead ||
                   npc.type == NPCID.SolarCrawltipedeHead || npc.type == NPCID.DuneSplicerHead ||
                   npc.type == NPCID.BloodEelHead;
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            ForcePushZeroGravity(npc, projectile, hit);
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            ForcePushZeroGravity(npc, player, hit);
        }

        private Vector2 GetAttackerCenter(Entity attacker)
        {
            if (attacker is Projectile proj)
            {
                if (proj.Center != Vector2.Zero)
                    return proj.Center;
                if (proj.owner >= 0 && proj.owner < Main.maxPlayers && Main.player[proj.owner].active)
                    return Main.player[proj.owner].Center;
            }
            else if (attacker is Player player)
            {
                if (player.Center != Vector2.Zero)
                    return player.Center;
            }
            return Vector2.Zero;
        }

        private void ForcePushZeroGravity(NPC npc, Entity attacker, NPC.HitInfo hit)
        {
            if (zeroGravityTimer > 0 && npc.type != NPCID.WallofFlesh && npc.type != NPCID.WallofFleshEye)
            {
                NPC targetPush = npc;

                if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs)
                {
                    NPC head = Main.npc[npc.realLife];
                    if (head.active)
                        targetPush = head;
                }
                else if (IsWormBody(npc))
                {
                    return;
                }

                // FIX: Comprobar firmemente si se trata de un gusano
                bool isWorm = IsWormHead(targetPush) || IsWormBody(targetPush);

                Vector2 attackerCenter = GetAttackerCenter(attacker);
                Vector2 pushDirection;

                if (attackerCenter != Vector2.Zero && Vector2.DistanceSquared(attackerCenter, targetPush.Center) > 0.1f)
                {
                    pushDirection = (targetPush.Center - attackerCenter).SafeNormalize(new Vector2(hit.HitDirection != 0 ? hit.HitDirection : 1, 0));
                }
                else
                {
                    pushDirection = new Vector2(hit.HitDirection != 0 ? hit.HitDirection : 1, -0.2f).SafeNormalize(Vector2.UnitX);
                }

                float pushForce = Math.Min(hit.Knockback * 0.35f, 6f);
                if (pushForce < 1.5f) pushForce = 1.5f;

                // FIX: Reducir drásticamente la fuerza si es un gusano para que cueste moverlo
                if (isWorm)
                {
                    pushForce *= 0.2f; // Reducción del 80% de fuerza
                }

                targetPush.velocity += pushDirection * pushForce;

                // FIX: Límite estricto de velocidad para evitar el acumulado de impactos simultáneos
                float maxVelocity = isWorm ? 7f : 25f; // Limita los gusanos a 7 de velocidad, resto a 25
                if (targetPush.velocity.Length() > maxVelocity)
                {
                    targetPush.velocity = Vector2.Normalize(targetPush.velocity) * maxVelocity;
                }

                // SISTEMA DE SINCRONIZACIÓN DE RED
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    ModPacket packet = ModContent.GetInstance<Jojo>().GetPacket();
                    packet.Write(Jojo.PacketType_ZeroGravityPush);
                    packet.Write(targetPush.whoAmI);
                    packet.Write(targetPush.velocity.X);
                    packet.Write(targetPush.velocity.Y);
                    packet.Send();
                }
                else if (Main.netMode == NetmodeID.Server)
                {
                    targetPush.netUpdate = true;
                }
            }
        }

        public override bool PreAI(NPC npc)
        {
            if (zeroGravityTimer > 0)
            {
                if (!wasInZeroGravity)
                {
                    CMoonSkillHandler_Tier_3.CancelDash(npc);
                    cachedNoGravity = npc.noGravity;
                    wasInZeroGravity = true;
                }

                zeroGravityTimer--;

                if (npc.type == NPCID.WallofFlesh || npc.type == NPCID.WallofFleshEye)
                {
                    npc.velocity = Vector2.Zero;
                    return false;
                }

                if (IsWormBody(npc))
                {
                    if (Main.rand.NextBool(10)) Dust.NewDust(npc.position, npc.width, npc.height, DustID.MagicMirror, 0, 0, 100, Color.White, 0.8f);
                    return base.PreAI(npc);
                }

                npc.noGravity = true;
                npc.velocity *= 0.985f;

                if (float.IsNaN(npc.velocity.X) || float.IsNaN(npc.velocity.Y) || float.IsInfinity(npc.velocity.X) || float.IsInfinity(npc.velocity.Y))
                {
                    npc.velocity = Vector2.Zero;
                }

                if (IsWormHead(npc))
                {
                    if (npc.velocity.Length() > 0.1f)
                        npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                }
                else
                {
                    float speed = npc.velocity.Length();
                    if (float.IsNaN(speed) || float.IsInfinity(speed)) speed = 0f;

                    float rotationSpeed = (npc.velocity.X > 0 ? 0.04f : -0.04f) * Math.Max(0.5f, speed * 0.2f);
                    npc.rotation += rotationSpeed;

                    if (float.IsNaN(npc.rotation) || float.IsInfinity(npc.rotation)) npc.rotation = 0f;
                    npc.rotation = npc.rotation % MathHelper.TwoPi;
                }

                Vector2 bounceVelocity = Collision.TileCollision(npc.position, npc.velocity, npc.width, npc.height, false, false, 1);

                if (!float.IsNaN(bounceVelocity.X) && !float.IsNaN(bounceVelocity.Y))
                {
                    if (bounceVelocity.X != npc.velocity.X) npc.velocity.X = -npc.velocity.X * 0.8f;
                    if (bounceVelocity.Y != npc.velocity.Y) npc.velocity.Y = -npc.velocity.Y * 0.8f;
                }

                if (Main.rand.NextBool(10)) Dust.NewDust(npc.position, npc.width, npc.height, DustID.MagicMirror, 0, 0, 100, Color.White, 0.8f);

                return false;
            }
            else if (wasInZeroGravity)
            {
                npc.noGravity = cachedNoGravity;

                if (npc.type != NPCID.WallofFlesh && npc.type != NPCID.WallofFleshEye)
                {
                    npc.rotation = 0f;
                    npc.velocity.X *= 0.2f;
                }

                wasInZeroGravity = false;

                if (Main.netMode != NetmodeID.SinglePlayer)
                    npc.netUpdate = true;
            }

            return base.PreAI(npc);
        }
    }

    // LÓGICA GLOBAL PARA PROYECTILES AFECTADOS — EXCLUSIVA DE TIER 3
    public class CMoonZeroGravityProjectile_Tier3 : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public int zeroGravityTimer = 0;

        public void ActivateZeroGravity(Projectile proj)
        {
            // FIX BUG: mismo criterio que en NPCs — cancela la Gravedad Invertida (F)
            // antes de tomar control con Gravedad Cero (H).
            proj.GetGlobalProjectile<CMoonGravityProjectile_Tier_3>().CancelSilently(proj);

            zeroGravityTimer = 5;
        }

        /// <summary>
        /// FIX BUG: cancela la Gravedad Cero (H) sobre este proyectil sin aplicar
        /// ningún efecto adicional, para cederle el control a la Habilidad F.
        /// </summary>
        public void CancelSilently(Projectile proj)
        {
            zeroGravityTimer = 0;

            if (Main.netMode == NetmodeID.Server)
                proj.netUpdate = true;
        }

        public override bool PreAI(Projectile projectile)
        {
            if (zeroGravityTimer > 0)
            {
                zeroGravityTimer--;
                projectile.velocity *= 0.98f;

                if (float.IsNaN(projectile.velocity.X) || float.IsNaN(projectile.velocity.Y))
                    projectile.velocity = Vector2.Zero;

                if (projectile.velocity.Length() > 0.1f)
                {
                    projectile.rotation += (projectile.velocity.X > 0 ? 0.05f : -0.05f);
                    if (float.IsNaN(projectile.rotation)) projectile.rotation = 0f;
                    projectile.rotation = projectile.rotation % MathHelper.TwoPi;
                }

                Vector2 bounceVelocity = Collision.TileCollision(projectile.position, projectile.velocity, projectile.width, projectile.height, false, false, 1);

                if (!float.IsNaN(bounceVelocity.X) && !float.IsNaN(bounceVelocity.Y))
                {
                    if (bounceVelocity.X != projectile.velocity.X) projectile.velocity.X = -projectile.velocity.X * 0.8f;
                    if (bounceVelocity.Y != projectile.velocity.Y) projectile.velocity.Y = -projectile.velocity.Y * 0.8f;
                }

                return false;
            }
            return base.PreAI(projectile);
        }
    }

    // COMPORTAMIENTO DE GRAVEDAD CERO PARA JUGADORES (PvP) — EXCLUSIVO DE TIER 3
    public class CMoonZeroGravityPlayer_Tier3 : ModPlayer
    {
        public int zeroGravityTimer = 0;

        public void ActivateZeroGravity(Player player)
        {
            // FIX BUG: mismo criterio que en NPCs/proyectiles — cancela la Gravedad
            // Invertida (F) antes de tomar control con Gravedad Cero (H).
            player.GetModPlayer<CMoonGravityPlayer_Tier_3>().CancelSilently();

            zeroGravityTimer = 5;
        }

        /// <summary>
        /// FIX BUG: cancela la Gravedad Cero (H) sobre este jugador, para cederle
        /// el control a la Habilidad F.
        /// </summary>
        public void CancelSilently()
        {
            zeroGravityTimer = 0;
        }

        public override void PreUpdate()
        {
            if (zeroGravityTimer > 0)
            {
                zeroGravityTimer--;
                Player.gravity = 0f;
                Player.maxFallSpeed = 0f;
                Player.velocity *= 0.985f;
                if (Main.rand.NextBool(10)) Dust.NewDust(Player.position, Player.width, Player.height, DustID.MagicMirror, 0, 0, 100, Color.White, 0.8f);
            }
        }
    }
}