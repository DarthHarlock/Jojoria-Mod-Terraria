using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using System;
using System.IO;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Buffs.MagiciansRed_Buffs; // Asegurado para que reconozca el debufo

namespace Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_4
{
    public class Tornado_Tier_4 : ModProjectile
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

        // --- Helper para obtener el polvo del tornado según la skin ---
        private int GetTornadoDust(Player p)
        {
            if (UI.StandSlotSystem.HasMagiciansRedPinkSkinFor(p)) return DustID.PinkTorch;
            if (UI.StandSlotSystem.HasMagiciansRedGreenSkinFor(p)) return DustID.CursedTorch;
            if (UI.StandSlotSystem.HasMagiciansRedBlueSkinFor(p)) return DustID.IceTorch;
            return DustID.Torch;
        }

        // =======================================================================
        // ---------- CONFIGURACIÓN GENERAL (Se setea al invocarlo) ----------
        // =======================================================================

        public int GrowTime = 70;
        public int Duration = 420;
        public int DyingTime = 80;
        public int SizeSegments = 12;
        public int TornadoBaseDamage = 200;
        public int TickDamageCooldown = 20;
        public float Scale = 1f;
        public int baseCritChance = 5;
        public float BottomScale = 0.5f;
        public float TopScale = 1.6f;
        public int SpinAnimationDelay = 3;
        public int OpacityPercent = 70;
        public float WavePhaseStep = 0.9f;
        public float WaveSpeed = 0.12f;
        public float WaveAmplitude = 35f;

        // =======================================================================
        // ---------- CONFIGURACIÓN DE LAS BOLAS DE FUEGO (Betsy's Wrath) ----------
        // =======================================================================
        // Cada cierto tiempo, un segmento aleatorio visible del tornado dispara
        // una bola de fuego (ApprenticeStaffT3Shot) en trayectoria de parábola.
        // El proyectil vanilla ya trae su propia gravedad incorporada.
        public int FireballMinCooldown = 10; //12
        public int FireballMaxCooldown = 10; //28
        public int FireballBaseDamage = 50;
        public float FireballMinLaunchSpeedX = 3f;
        public float FireballMaxLaunchSpeedX = 7f;
        public float FireballMinLaunchSpeedY = 9f;
        public float FireballMaxLaunchSpeedY = 13f;

        int fireballTimer;

        const int FrameWidth = 162;
        const int FrameHeight = 44;
        const int TotalFrames = 6;

        const string TexturePath = "Jojo/Content/Projectiles/MagiciansRed/MagiciansRed_Tier_4/Tornado_Tier_4";

        bool init;
        float anchorX;
        float baseBottomY;
        int firstVisible;
        int lastVisible;
        int spinTimer;
        int spinFrame;

        int prevFirstVisible;
        int prevLastVisible;

        float[] segYOffsets;
        float[] segScales;

        static readonly SoundStyle SpawnWhoosh = new("Terraria/Sounds/Item_37") { Pitch = -0.2f };

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(GrowTime);
            writer.Write(Duration);
            writer.Write(DyingTime);
            writer.Write(SizeSegments);
            writer.Write(TornadoBaseDamage);
            writer.Write(TickDamageCooldown);
            writer.Write(Scale);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            GrowTime = reader.ReadInt32();
            Duration = reader.ReadInt32();
            DyingTime = reader.ReadInt32();
            SizeSegments = reader.ReadInt32();
            TornadoBaseDamage = reader.ReadInt32();
            TickDamageCooldown = reader.ReadInt32();
            Scale = reader.ReadSingle();
        }

        public override void SetDefaults()
        {
            Projectile.width = FrameWidth;
            Projectile.height = FrameHeight;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            if (!p.active || p.dead)
            {
                Projectile.Kill();
                return;
            }

            // Determinar la skin actual para las partículas
            int dustType = GetTornadoDust(p);

            if (!init)
            {
                init = true;
                anchorX = Projectile.Center.X;
                baseBottomY = Projectile.Center.Y;
                Projectile.localNPCHitCooldown = Math.Max(1, TickDamageCooldown);
                Projectile.timeLeft = GrowTime + Duration + DyingTime;
                SoundEngine.PlaySound(SpawnWhoosh, Projectile.Center);

                prevFirstVisible = 0;
                prevLastVisible = 0;

                fireballTimer = Main.rand.Next(FireballMinCooldown, FireballMaxCooldown + 1);

                segYOffsets = new float[SizeSegments];
                segScales = new float[SizeSegments];

                float currentY = 0f;
                for (int j = 0; j < SizeSegments; j++)
                {
                    float t = SizeSegments <= 1 ? 1f : j / (float)(SizeSegments - 1);
                    segScales[j] = MathHelper.Lerp(BottomScale, TopScale, t) * Scale;
                    float segH = FrameHeight * segScales[j];

                    segYOffsets[j] = currentY + (segH / 2f);
                    currentY += segH;
                }

                // EXPLOSIÓN INICIAL DE FUEGO (En la base)
                for (int d = 0; d < 45; d++)
                {
                    Vector2 velocity = new Vector2(0, -9f).RotatedBy(MathHelper.ToRadians(d * 8));
                    Dust dust = Dust.NewDustPerfect(new Vector2(anchorX, baseBottomY), dustType, velocity, 100, default, 3.5f);
                    dust.noGravity = true;
                    dust.velocity *= Main.rand.NextFloat(1f, 2f);
                }
            }

            Projectile.velocity = Vector2.Zero;

            int totalLife = GrowTime + Duration + DyingTime;
            int elapsed = totalLife - Projectile.timeLeft;

            bool growing = elapsed < GrowTime;
            bool dying = elapsed >= GrowTime + Duration;

            if (growing)
            {
                float growProgress = MathHelper.Clamp(elapsed / (float)Math.Max(1, GrowTime), 0f, 1f);
                firstVisible = 0;
                lastVisible = Math.Max(1, (int)Math.Ceiling(growProgress * SizeSegments));
            }
            else if (dying)
            {
                float dyingElapsed = elapsed - (GrowTime + Duration);
                float dyingProgress = MathHelper.Clamp(dyingElapsed / (float)Math.Max(1, DyingTime), 0f, 1f);
                firstVisible = Math.Min(SizeSegments, (int)Math.Floor(dyingProgress * SizeSegments));
                lastVisible = SizeSegments;
            }
            else
            {
                firstVisible = 0;
                lastVisible = SizeSegments;
            }

            int segCount = Math.Max(0, lastVisible - firstVisible);

            spinTimer++;
            spinFrame = (spinTimer / SpinAnimationDelay) % TotalFrames;

            if (Main.netMode != NetmodeID.Server)
            {
                if (lastVisible > prevLastVisible)
                {
                    for (int j = prevLastVisible; j < lastVisible; j++)
                    {
                        SpawnSegmentParticles(j, dustType);
                    }
                }
                if (firstVisible > prevFirstVisible)
                {
                    for (int j = prevFirstVisible; j < firstVisible; j++)
                    {
                        SpawnSegmentParticles(j, dustType);
                    }
                }
            }

            prevFirstVisible = firstVisible;
            prevLastVisible = lastVisible;

            Projectile.friendly = segCount > 0;

            float activeHeight = 0f;
            float maxWidth = 0f;
            float topY = baseBottomY;

            for (int j = 0; j < lastVisible; j++)
            {
                float segH = FrameHeight * segScales[j];
                float segW = FrameWidth * segScales[j];
                topY -= segH;

                if (j >= firstVisible)
                {
                    activeHeight += segH;
                    if (segW > maxWidth) maxWidth = segW;
                }
            }

            Projectile.width = Math.Max(2, (int)maxWidth);
            Projectile.height = Math.Max(2, (int)activeHeight);
            Projectile.position.X = anchorX - Projectile.width / 2f;
            Projectile.position.Y = topY;

            // Partículas constantes "Huracán de Fuego" adaptadas
            if (segCount > 0 && Main.rand.NextBool(2))
            {
                for (int j = firstVisible; j < lastVisible; j++)
                {
                    if (Main.rand.NextBool(2))
                    {
                        float wavePhase = (j * WavePhaseStep) - (spinTimer * WaveSpeed);
                        float wave = (float)Math.Sin(wavePhase) * WaveAmplitude * Scale;

                        Vector2 segCenter = new Vector2(anchorX + wave, baseBottomY - segYOffsets[j]);
                        float currentSegWidth = FrameWidth * segScales[j];

                        Vector2 dustPos = segCenter + new Vector2(Main.rand.NextFloat(-currentSegWidth / 2.5f, currentSegWidth / 2.5f), Main.rand.NextFloat(-FrameHeight / 2f, FrameHeight / 2f));

                        Dust dust = Dust.NewDustPerfect(dustPos, dustType, new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-6f, -2f)), 100, default, 1.5f + segScales[j]);
                        dust.noGravity = true;
                    }
                }
            }

            // ==============================================================
            // ---------- DISPARO DE BOLAS DE FUEGO EN PARÁBOLA ----------
            // ==============================================================
            // Solo el dueño decide cuándo y desde dónde se dispara, para evitar
            // duplicados en multijugador (Projectile.NewProjectile ya se sincroniza).
            if (segCount > 0 && Projectile.owner == Main.myPlayer)
            {
                fireballTimer--;
                if (fireballTimer <= 0)
                {
                    fireballTimer = Main.rand.Next(FireballMinCooldown, FireballMaxCooldown + 1);

                    // Elegimos un segmento visible al azar como origen del disparo
                    int segIndex = Main.rand.Next(firstVisible, lastVisible);

                    float wavePhase = (segIndex * WavePhaseStep) - (spinTimer * WaveSpeed);
                    float wave = (float)Math.Sin(wavePhase) * WaveAmplitude * Scale;
                    Vector2 launchPos = new Vector2(anchorX + wave, baseBottomY - segYOffsets[segIndex]);

                    // Velocidad aleatoria: hacia un lado (izq/der al azar) y hacia arriba,
                    // la gravedad propia del proyectil vanilla se encarga de la caída.
                    float dirX = Main.rand.NextBool() ? 1f : -1f;
                    float velX = dirX * Main.rand.NextFloat(FireballMinLaunchSpeedX, FireballMaxLaunchSpeedX);
                    float velY = -Main.rand.NextFloat(FireballMinLaunchSpeedY, FireballMaxLaunchSpeedY);

                    Vector2 launchVel = new Vector2(velX, velY);

                    int fireballDamage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(FireballBaseDamage);

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        launchPos,
                        launchVel,
                        ProjectileID.ApprenticeStaffT3Shot,
                        fireballDamage,
                        1f,
                        p.whoAmI
                    );

                    SoundEngine.PlaySound(SoundID.Item20, launchPos);
                }
            }

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);

            // Aquí el daño se escala con la clase, no con la velocidad del Stand
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(TornadoBaseDamage);
        }

        private void SpawnSegmentParticles(int j, int dustType)
        {
            float wavePhase = (j * WavePhaseStep) - (spinTimer * WaveSpeed);
            float wave = (float)Math.Sin(wavePhase) * WaveAmplitude * Scale;
            Vector2 segCenter = new Vector2(anchorX + wave, baseBottomY - segYOffsets[j]);

            // Disparo violento de fuego hacia la DERECHA
            for (int d = 0; d < 12; d++)
            {
                Vector2 vel = new Vector2(Main.rand.NextFloat(4f, 9f), Main.rand.NextFloat(-1.5f, 1.5f));
                Dust dust = Dust.NewDustPerfect(segCenter, dustType, vel, 100, default, 2f + (segScales[j] * 0.5f));
                dust.noGravity = true;
            }

            // Disparo violento de fuego hacia la IZQUIERDA
            for (int d = 0; d < 12; d++)
            {
                Vector2 vel = new Vector2(Main.rand.NextFloat(-9f, -4f), Main.rand.NextFloat(-1.5f, 1.5f));
                Dust dust = Dust.NewDustPerfect(segCenter, dustType, vel, 100, default, 2f + (segScales[j] * 0.5f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (segYOffsets == null || segScales == null) return false;

            Player owner = Main.player[Projectile.owner];
            string skinFolder = GetMagiciansRedSkinFolder(owner);
            string texPath = skinFolder != null ? skinFolder + "Tornado" : TexturePath;

            Texture2D tex = ModContent.Request<Texture2D>(texPath).Value;
            Vector2 origin = new Vector2(FrameWidth / 2f, FrameHeight / 2f);

            Rectangle src = new Rectangle(0, (TotalFrames - 1 - spinFrame) * FrameHeight, FrameWidth, FrameHeight);

            float alphaMult = MathHelper.Clamp(OpacityPercent / 100f, 0f, 1f);
            Color drawColor = lightColor * alphaMult;

            for (int i = firstVisible; i < lastVisible; i++)
            {
                float baseSegScale = segScales[i];

                float wavePhase = (i * WavePhaseStep) - (spinTimer * WaveSpeed);
                float sinVal = (float)Math.Sin(wavePhase);

                float wave = sinVal * WaveAmplitude * Scale;

                Vector2 drawScale = new Vector2(baseSegScale, baseSegScale);

                Vector2 segPos = new Vector2(
                    Projectile.Center.X + wave,
                    baseBottomY - segYOffsets[i]
                ) - Main.screenPosition;

                Main.EntitySpriteDraw(tex, segPos, src, drawColor, 0f, origin, drawScale, SpriteEffects.None, 0f);
            }

            return false;
        }

        // --- NUEVO: Aplica el debufo al enemigo cuando el tornado lo golpea ---
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Cambia el "MagiciansFire_Tier_4" por el nombre real de tu clase del debufo si es diferente (Ej. MagiciansFire_Tier_1)
            target.AddBuff(ModContent.BuffType<MagiciansFire_Tier_4>(), 180); // 180 ticks = 3 segundos
        }
    }
}