using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;
using Jojo.Content.Clases;
using Jojo.Content.Items;
using Jojo.Content.Projectiles.StandsNpc;

namespace Jojo.Content.NPCs.StandsNpc.SilverChariot
{
    public class ShotNpc : BaseStandNpcProjectile
    {
        // ai[0] = número de rebotes usados
        // ai[1] = índice del NPC dueño
        // ai[2] = 1 si está clavada en el suelo

        const float MaxSpeed = 22f;
        const float Accel = 0.35f;
        const int MaxBounces = 5;          // <- rebota 5 veces
        const int StuckTicks = 180;

        // ===== DAÑO PROPIO DE LA ESTOCADA (configurable) =====
        // Interruptores independientes: gana el más alto activo.
        public const int ShotDamageBase = 40;      // Sin progresión
        public const int ShotDamageHardmode = 80;  // Muro de Carne
        public const int ShotDamageMech = 120;     // Cualquier mecánico
        public const int ShotDamageGolem = 140;    // Gólem

        // Penetración de armadura de la estocada, por tier
        public const int ShotPenBase = 20;
        public const int ShotPenHardmode = 30;
        public const int ShotPenMech = 40;
        public const int ShotPenGolem = 50;

        public static int GetShotDamage()
        {
            int d = ShotDamageBase;
            if (Main.hardMode) d = Math.Max(d, ShotDamageHardmode);
            if (NPC.downedMechBossAny) d = Math.Max(d, ShotDamageMech);
            if (NPC.downedGolemBoss) d = Math.Max(d, ShotDamageGolem);
            return d;
        }

        public static int GetShotArmorPen()
        {
            int p = ShotPenBase;
            if (Main.hardMode) p = Math.Max(p, ShotPenHardmode);
            if (NPC.downedMechBossAny) p = Math.Max(p, ShotPenMech);
            if (NPC.downedGolemBoss) p = Math.Max(p, ShotPenGolem);
            return p;
        }

        /// <summary>NPC dueño del Stand (almacenado en ai[1] al crearse el proyectil)</summary>
        public NPC OwnerNpc
        {
            get
            {
                int index = (int)Projectile.ai[1];
                if (index >= 0 && index < Main.maxNPCs)
                {
                    NPC npc = Main.npc[index];
                    if (npc != null && npc.active) return npc;
                }
                return null;
            }
        }

        bool Stuck => Projectile.ai[2] == 1f;

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 10;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 600;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (Stuck) return false;
            NPC owner = OwnerNpc;
            if (owner == null) return false;
            if (target.whoAmI == owner.whoAmI) return false;
            if (target.dontTakeDamage) return false;
            if (!IAStandNpc.AreEnemies(owner, target)) return false;
            return true;
        }

        public override bool CanHitPlayer(Player target)
        {
            if (Stuck) return false;
            NPC owner = OwnerNpc;
            if (owner == null) return false;
            if (IAStandNpc.IsPacific(owner)) return false;
            if (owner.friendly && !target.GetModPlayer<NpcKillerPlayer>().canKillTownNPCs) return false;
            return base.CanHitPlayer(target);
        }

        public override bool CanHitPvp(Player target) => false;

        public override void AI()
        {
            // Daño y penetración propios, recalculados cada tick (igual en todas las máquinas)
            Projectile.damage = GetShotDamage();
            Projectile.ArmorPenetration = GetShotArmorPen();

            if (Stuck)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.friendly = false;
                Projectile.hostile = false;

                if (Projectile.timeLeft <= 60)
                    Projectile.alpha = (int)(255 * (1f - Projectile.timeLeft / 60f));

                return;
            }

            float currentSpeed = Projectile.velocity.Length();
            if (currentSpeed < MaxSpeed)
            {
                float newSpeed = Math.Min(currentSpeed + Accel, MaxSpeed);
                Projectile.velocity = Vector2.Normalize(Projectile.velocity) * newSpeed;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity * 0.5f + Main.rand.NextVector2Circular(3f, 3f),
                    DustID.WhiteTorch,
                    -Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.3f, 0.3f),
                    180,
                    Color.White,
                    Main.rand.NextFloat(0.6f, 1.1f)
                );
                d.noGravity = true;
                d.fadeIn = 0.4f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ProcesarRebote(target);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            ProcesarRebote(target);
        }

        private void ProcesarRebote(Entity hitEntity)
        {
            int bounces = (int)Projectile.ai[0];

            if (bounces < MaxBounces)
            {
                NPC owner = OwnerNpc;
                Entity nextTarget = FindClosestTarget(owner, 800f, hitEntity);

                if (nextTarget != null)
                {
                    Vector2 direction = (nextTarget.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                    Projectile.velocity = direction * MaxSpeed;

                    Projectile.ai[0]++;
                    Projectile.netUpdate = true;

                    SoundEngine.PlaySound(SoundID.Item154, Projectile.Center);
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            int bounces = (int)Projectile.ai[0];

            if (bounces >= MaxBounces)
            {
                Projectile.ai[2] = 1f;
                Projectile.velocity = Vector2.Zero;
                Projectile.tileCollide = false;
                Projectile.timeLeft = StuckTicks;
                Projectile.netUpdate = true;

                SoundEngine.PlaySound(SoundID.Tink, Projectile.Center);
                return false;
            }

            if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > 0.1f)
                Projectile.velocity.X = -oldVelocity.X;
            if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > 0.1f)
                Projectile.velocity.Y = -oldVelocity.Y;

            NPC owner = OwnerNpc;
            Entity nextTarget = FindClosestTarget(owner, 800f, null);
            if (nextTarget != null)
            {
                Vector2 direction = (nextTarget.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                Projectile.velocity = direction * MaxSpeed;
            }

            Projectile.ai[0]++;
            SoundEngine.PlaySound(SoundID.Tink, Projectile.Center);
            Projectile.netUpdate = true;
            return false;
        }

        private Entity FindClosestTarget(NPC owner, float maxDetectDistance, Entity ignoreEntity)
        {
            if (owner == null) return null;

            Entity closest = null;
            float sqrMax = maxDetectDistance * maxDetectDistance;
            bool ownerIsFriendly = owner.friendly || owner.townNPC;

            // Buscar NPCs enemigos
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC target = Main.npc[i];
                if (target.active && !target.dontTakeDamage && (ignoreEntity == null || target.whoAmI != ignoreEntity.whoAmI) && target.whoAmI != owner.whoAmI)
                {
                    if (IAStandNpc.AreEnemies(owner, target))
                    {
                        float sqrDist = Vector2.DistanceSquared(target.Center, Projectile.Center);
                        if (sqrDist < sqrMax && Collision.CanHit(Projectile.Center, 1, 1, target.Center, 1, 1))
                        {
                            sqrMax = sqrDist;
                            closest = target;
                        }
                    }
                }
            }

            // Buscar Jugadores si el NPC es hostil o el jugador puede hacerle daño
            for (int p = 0; p < Main.maxPlayers; p++)
            {
                Player player = Main.player[p];
                if (player.active && !player.dead && (ignoreEntity == null || player.whoAmI != ignoreEntity.whoAmI))
                {
                    bool canTargetPlayer = false;
                    if (!ownerIsFriendly && !IAStandNpc.IsPacific(owner)) canTargetPlayer = true;
                    if (ownerIsFriendly && player.GetModPlayer<NpcKillerPlayer>().canKillTownNPCs) canTargetPlayer = true;

                    if (canTargetPlayer)
                    {
                        float sqrDist = Vector2.DistanceSquared(player.Center, Projectile.Center);
                        if (sqrDist < sqrMax && Collision.CanHit(Projectile.Center, 1, 1, player.Center, 1, 1))
                        {
                            sqrMax = sqrDist;
                            closest = player;
                        }
                    }
                }
            }

            return closest;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() / 2f;
            Color finalColor = lightColor * ((255 - Projectile.alpha) / 255f);

            Vector2 scale = new Vector2(1f, 0.7f);

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                null,
                finalColor,
                Projectile.rotation,
                origin,
                scale,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }
}