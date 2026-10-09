using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using Jojo.Content.Clases;
using Jojo.Content.Buffs.MagiciansRed_Buffs;

namespace Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_3
{
    public static class MagiciansFire2Helper
    {
        public const int OrbCount = 8;
        public const float BaseDamage = 70f;

        public static void Activar(Player p)
        {
            SoundEngine.PlaySound(SoundID.Item20, p.Center);
            p.AddBuff(ModContent.BuffType<MagiciansFire2>(), MagiciansFire2.DurationTicks);

            int damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(BaseDamage);

            float stepAngle = MathHelper.TwoPi / OrbCount;

            for (int i = 0; i < OrbCount; i++)
            {
                float anguloInicial = stepAngle * i;

                Projectile.NewProjectile(
                    p.GetSource_Misc("MagiciansFire2"),
                    p.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<Fuego2_Tier_3>(),
                    damage,
                    0f,
                    p.whoAmI,
                    anguloInicial
                );
            }
        }
    }

    public class Fuego2_Tier_3 : ModProjectile
    {
        const string SkinBasePath = "Jojo/Content/Projectiles/Skins/MagiciansRed/";

        static string GetMagiciansRedSkinFolder(Player p)
        {
            if (UI.StandSlotSystem.HasMagiciansRedPinkSkinFor(p))
                return SkinBasePath + "Magicias_Pink/";

            if (UI.StandSlotSystem.HasMagiciansRedGreenSkinFor(p))
                return SkinBasePath + "Magicias_Green/";

            if (UI.StandSlotSystem.HasMagiciansRedBlueSkinFor(p))
                return SkinBasePath + "Magicias_Blue/";

            return null;
        }

        // --- NUEVO: Helper para obtener los colores de las partículas y la luz según la skin ---
        private void GetSkinVisuals(Player p, out int mainDust, out int explodeDust, out Vector3 lightColor)
        {
            if (UI.StandSlotSystem.HasMagiciansRedPinkSkinFor(p))
            {
                mainDust = DustID.PinkTorch;
                explodeDust = DustID.PinkTorch;
                lightColor = new Vector3(0.8f, 0.1f, 0.6f); // Luz rosa
            }
            else if (UI.StandSlotSystem.HasMagiciansRedGreenSkinFor(p))
            {
                mainDust = DustID.CursedTorch;
                explodeDust = DustID.CursedTorch;
                lightColor = new Vector3(0.2f, 0.8f, 0.2f); // Luz verde
            }
            else if (UI.StandSlotSystem.HasMagiciansRedBlueSkinFor(p))
            {
                mainDust = DustID.IceTorch;
                explodeDust = DustID.IceTorch;
                lightColor = new Vector3(0.1f, 0.4f, 0.8f); // Luz azul
            }
            else
            {
                mainDust = DustID.Torch;
                explodeDust = DustID.SolarFlare;
                lightColor = new Vector3(0.8f, 0.4f, 0.1f); // Luz naranja original
            }
        }

        private const float OrbitRadius = 132f;
        private const float AngularSpeed = 0.05f;
        private const float ExpansionTime = 25f;

        // Ángulo de órbita (ai[0])
        public float Angle
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        // Estado: 0 = Órbita, 1 = Teledirigido, 2 = Explotando
        public float State
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public bool IsExploding
        {
            get => Projectile.ai[2] == 1f;
            set => Projectile.ai[2] = value ? 1f : 0f;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.penetrate = -1;
            Projectile.tileCollide = false; // Desactivado durante la órbita
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 36000;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override void AI()
        {
            if (IsExploding) return;

            Player owner = Main.player[Projectile.owner];

            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            // Obtenemos los visuales para esta skin
            GetSkinVisuals(owner, out int mainDust, out int explodeDust, out Vector3 lightColor);

            // --- FASE 0: ÓRBITA ALREDEDOR DEL JUGADOR ---
            if (State == 0f)
            {
                if (Projectile.owner == Main.myPlayer)
                {
                    if (!owner.HasBuff(ModContent.BuffType<MagiciansFire2>()))
                    {
                        State = 1f; // Pasamos a Fase 1
                        Projectile.timeLeft = 240; // 4 segundos de persecución
                        Projectile.tileCollide = true; // Se activa colisión con bloques
                        Projectile.velocity = Vector2.UnitX.RotatedBy(Angle) * 8f;
                        Projectile.netUpdate = true;
                        return;
                    }
                }

                Projectile.localAI[0]++;
                float progress = MathHelper.Clamp(Projectile.localAI[0] / ExpansionTime, 0f, 1f);
                float currentRadius = MathHelper.SmoothStep(0f, OrbitRadius, progress);

                Angle += AngularSpeed;
                if (Angle > MathHelper.TwoPi) Angle -= MathHelper.TwoPi;

                Vector2 offset = Vector2.UnitX.RotatedBy(Angle) * currentRadius;
                Projectile.Center = owner.Center + offset;
                Projectile.velocity = Vector2.Zero;

                Projectile.rotation = Angle + MathHelper.PiOver2;

                Lighting.AddLight(Projectile.Center, lightColor.X, lightColor.Y, lightColor.Z);

                if (Main.rand.NextBool(3))
                {
                    Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, mainDust, 0f, 0f, 100, default, 1.3f);
                    dust.noGravity = true;
                    dust.velocity *= 0.3f;
                }
            }
            // --- FASE 1: PERSECUCIÓN TELEDIRIGIDA ---
            else if (State == 1f)
            {
                NPC target = FindTarget();

                if (target != null)
                {
                    Vector2 direction = Projectile.DirectionTo(target.Center);
                    Vector2 desiredVelocity = direction * 14f;
                    Projectile.velocity = (Projectile.velocity * 25f + desiredVelocity) / 26f;
                }

                Projectile.rotation = Projectile.velocity.ToRotation();

                Lighting.AddLight(Projectile.Center, lightColor.X, lightColor.Y, lightColor.Z);

                // Rastro de Fuego 1 adaptado
                if (Main.rand.NextBool(2))
                {
                    Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, mainDust, 0f, 0f, 100, default, 1.6f);
                    dust.noGravity = true;
                    dust.velocity = Projectile.velocity * -0.1f;
                }

                if (Projectile.timeLeft <= 4)
                {
                    Explode();
                }
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 4;
            }
        }

        private NPC FindTarget()
        {
            NPC closest = null;
            float minDistance = 800f;

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && !npc.dontTakeDamage && npc.lifeMax > 5 && npc.CanBeChasedBy())
                {
                    float dist = Vector2.Distance(Projectile.Center, npc.Center);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closest = npc;
                    }
                }
            }
            return closest;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!IsExploding && State == 1f)
            {
                Explode();
            }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<MagiciansFire_Tier_3>(), 720);

            if (!IsExploding && State == 1f)
            {
                Explode();
            }
        }

        // --- FASE 2: EXPLOSIÓN ---
        private void Explode()
        {
            IsExploding = true;
            State = 2f;

            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.timeLeft = 3;

            Vector2 center = Projectile.Center;
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.Center = center;

            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            Player owner = Main.player[Projectile.owner];
            GetSkinVisuals(owner, out int mainDust, out int explodeDust, out Vector3 lightColor);

            // Anillo Expansivo adaptado
            for (int i = 0; i < 70; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(Projectile.width / 2f, Projectile.height / 2f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + (direction * 0.2f), mainDust, direction * 0.25f, 100, default, 2.8f);
                dust.noGravity = true;
            }

            // Explosión central adaptada
            for (int i = 0; i < 50; i++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, explodeDust, 0f, 0f, 100, default, 1.0f);
                dust.velocity *= 3f;
                dust.noGravity = true;
            }

            // Humo (Se mantiene universal para cualquier fuego)
            for (int i = 0; i < 30; i++)
            {
                Dust smoke = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 1.5f);
                smoke.velocity *= 1.5f;
            }

            Projectile.netUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            string skinFolder = GetMagiciansRedSkinFolder(owner);

            Texture2D texture = skinFolder != null
                ? ModContent.Request<Texture2D>(skinFolder + "Fuego2").Value
                : Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;

            int frameWidth = texture.Width / 4;
            int frameHeight = texture.Height;

            Rectangle sourceRectangle = new Rectangle(Projectile.frame * frameWidth, 0, frameWidth, frameHeight);
            Vector2 origin = new Vector2(frameWidth * 0.5f, frameHeight * 0.5f);

            SpriteEffects effects = SpriteEffects.None;

            if (State == 1f && Projectile.velocity.X < 0)
            {
                effects = SpriteEffects.FlipVertically;
            }

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                sourceRectangle,
                lightColor * ((255 - Projectile.alpha) / 255f),
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0
            );

            return false;
        }
    }
}