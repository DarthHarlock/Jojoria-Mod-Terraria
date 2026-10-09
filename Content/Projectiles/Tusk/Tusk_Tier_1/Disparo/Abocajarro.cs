using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using System.IO;
using Jojo.Content.Clases;
using Jojo.Content.Systems;

namespace Jojo.Content.Projectiles.Tusk.Tusk_Tier_1.Disparo
{
    // HABILIDAD: Abocajarro (click derecho)
    // Dispara balas UNA TRAS OTRA, a toda velocidad, con puntería baja,
    // hasta vaciar el cargador. Cada disparo tiene su sonido, aro de
    // partículas, brazo y explosión (la explosión la hace TuskBala).
    public class Abocajarro
    {
        // ---------- AJUSTES (modificables) ----------
        const int BalaDamage = 10;
        const float BalaVelocidad = 11f;
        const float BalaKnockback = 2f;

        // Sonido de cada disparo (mismo que la bala normal)
        static readonly SoundStyle ShotSound = new("Jojo/Content/Sonidos/Tusk/Shot1");

        // Ticks entre disparo y disparo (60 = 1 s). 5 => 12 balas/segundo
        const int TicksEntreDisparos = 5;
        // Pequeña pausa antes del primer disparo (ticks)
        const int RetrasoInicial = 6;
        // Cooldown tras acabar la ráfaga antes de poder disparar de nuevo (ticks)
        const int CooldownFinal = 20;

        // Precisión (0 a 100). BAJA a propósito.
        const int Precision = 25;
        // Desviación máxima (radianes) con precisión 0. 0.7 rad ≈ 40 grados
        const float DesvioMaximo = 0.7f;

        // Brazo y aro (mismos valores que TuskAimController)
        const float ArmLength = 30f;
        static readonly Vector2 ShoulderOffset = new Vector2(0f, -6f);
        const int PulseDur = 12;            // más corto: los disparos son rápidos
        const float RingBaseRadius = 5f;
        const float PulseExpand = 16f;
        const int RingPoints = 12;

        // ---------- ESTADO ----------
        bool active;
        float aimAngle;      // ángulo mostrado del brazo (con desvío)
        sbyte facing = 1;    // hacia dónde mira el cuerpo (1 = derecha, -1 = izquierda). SINCRONIZADO
        int fireTimer;
        int cooldown;
        int pulseTimer;
        int netTimer;
        bool prevClick;

        public byte shotCount;            // sincronizado
        byte lastShotCount;               // para detectar disparos en remotos
        byte previousShotCountClient;     // para gastar balas en remotos

        public bool Active => active;

        // ---------- RED ----------
        public void Write(BinaryWriter writer)
        {
            writer.Write(active);
            writer.Write(aimAngle);
            writer.Write(shotCount);
            writer.Write(facing);
        }

        public void Read(BinaryReader reader)
        {
            active = reader.ReadBoolean();
            aimAngle = reader.ReadSingle();
            byte newShot = reader.ReadByte();
            facing = reader.ReadSByte();

            if (newShot != lastShotCount)
            {
                lastShotCount = newShot;
                pulseTimer = PulseDur;
            }
            shotCount = newShot;
        }

        Vector2 GetShoulder(Player p) => p.MountedCenter + ShoulderOffset;
        Vector2 GetArmEnd(Player p) => GetShoulder(p) + aimAngle.ToRotationVector2() * ArmLength;

        // ---------- UPDATE ----------
        // otherAiming: true si el apuntado normal (click izquierdo) está en curso.
        public void Update(Projectile proj, Player p, bool canUse, int baseCritChance, bool otherAiming)
        {
            TuskAimPlayer tuskP = p.GetModPlayer<TuskAimPlayer>();

            if (proj.owner == Main.myPlayer)
            {
                HandleBarrage(proj, p, canUse, baseCritChance, tuskP, otherAiming);
            }
            else
            {
                // Multijugador: gasta visualmente las balas que el otro jugador disparó
                byte delta = (byte)(shotCount - previousShotCountClient);
                for (int i = 0; i < delta; i++)
                    tuskP.ConsumirBala();
                previousShotCountClient = shotCount;

                // Multijugador: el cuerpo del jugador remoto mira hacia donde apunta
                if (active)
                    p.direction = facing;
            }

            // Mientras dura la ráfaga, el brazo/onda de recarga salen de aquí
            if (active)
            {
                tuskP.armAiming = true;
                tuskP.armAngle = aimAngle;
            }

            if (pulseTimer > 0) pulseTimer--;
        }

        void HandleBarrage(Projectile proj, Player p, bool canUse, int baseCritChance, TuskAimPlayer tuskP, bool otherAiming)
        {
            if (cooldown > 0) cooldown--;

            bool click = Main.mouseRight;

            if (!active)
            {
                if (click && !prevClick && canUse && !otherAiming && cooldown <= 0 && !p.mouseInterface)
                {
                    if (tuskP.municion > 0)
                    {
                        active = true;
                        fireTimer = RetrasoInicial;
                        netTimer = 0;
                        UpdateFacing(p);
                        aimAngle = (Main.MouseWorld - GetShoulder(p)).ToRotation();
                        proj.netUpdate = true;
                    }
                    else
                    {
                        tuskP.TriggerEmpty();
                        SoundEngine.PlaySound(SoundID.Camera with { Pitch = -0.5f, Volume = 0.5f }, p.Center);
                    }
                }
            }
            else
            {
                // Stand guardado/desequipado/muerto, etc. -> cancela y conserva las balas
                if (!canUse)
                {
                    Cancel(proj);
                }
                // Cargador vacío -> fin natural de la ráfaga, vuelves a la normalidad
                else if (tuskP.municion <= 0)
                {
                    active = false;
                    cooldown = CooldownFinal;
                    tuskP.aiming = false;
                    proj.netUpdate = true;
                }
                else
                {
                    sbyte oldFacing = facing;
                    UpdateFacing(p);

                    // Si giras el cuerpo, avisa enseguida a los demás clientes
                    if (facing != oldFacing)
                        proj.netUpdate = true;

                    // El brazo sigue al ratón entre disparos
                    Vector2 toMouse = Main.MouseWorld - GetShoulder(p);
                    if (toMouse != Vector2.Zero)
                        aimAngle = toMouse.ToRotation();

                    if (--fireTimer <= 0)
                    {
                        fireTimer = TicksEntreDisparos;
                        Fire(proj, p, baseCritChance);
                        tuskP.ConsumirBala();
                        proj.netUpdate = true;
                    }

                    // Mientras dura la ráfaga, sincroniza brazo y dirección con frecuencia
                    if (++netTimer >= 3)
                    {
                        netTimer = 0;
                        proj.netUpdate = true;
                    }
                }
            }

            // Mientras dura, bloquea el uso del objeto de la mano (CanUseItem)
            if (active) tuskP.aiming = true;

            prevClick = click;
        }

        void UpdateFacing(Player p)
        {
            int dir = Main.MouseWorld.X >= p.Center.X ? 1 : -1;
            facing = (sbyte)dir;
            p.ChangeDir(dir);
        }

        float GetDeviation()
        {
            int precision = Math.Clamp(Precision, 0, 100);
            float maxDev = DesvioMaximo * (1f - precision / 100f);
            return Main.rand.NextFloat(-maxDev, maxDev);
        }

        // ---------- DISPARO (1 bala por llamada) ----------
        void Fire(Projectile proj, Player p, int baseCritChance)
        {
            // Apunta al ratón y le suma un desvío aleatorio
            float baseAngle = (Main.MouseWorld - GetShoulder(p)).ToRotation();
            aimAngle = baseAngle + GetDeviation();   // el brazo se ve apuntando al desvío

            Vector2 dir = aimAngle.ToRotationVector2();
            Vector2 spawnPos = GetArmEnd(p);

            int dmg = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(BalaDamage);

            int idx = Projectile.NewProjectile(
                proj.GetSource_FromThis(),
                spawnPos,
                dir * BalaVelocidad,
                ModContent.ProjectileType<TuskBala>(),
                dmg,
                BalaKnockback,
                proj.owner,
                0f // bala normal
            );

            if (idx >= 0 && idx < Main.maxProjectiles)
                Main.projectile[idx].CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);

            shotCount++;
            lastShotCount = shotCount;
            previousShotCountClient = shotCount; // el dueño ya gastó la bala
            pulseTimer = PulseDur;

            // Sonido nuevo Shot1 (el mismo que la bala normal)
            SoundEngine.PlaySound(ShotSound, spawnPos);

            // Chispas de boca de cañón
            for (int i = 0; i < 4; i++)
            {
                Vector2 v = dir.RotatedByRandom(0.4f) * Main.rand.NextFloat(1.5f, 4f);
                Dust d = Dust.NewDustPerfect(spawnPos, DustID.BlueTorch, v, 100, default, 1.0f);
                d.noGravity = true;
            }
        }

        // ---------- CANCELAR ----------
        // Se conservan las balas que quedaban (no se toca la munición).
        public void Cancel(Projectile proj)
        {
            active = false;
            fireTimer = 0;
            ReleasePlayer(proj.owner);
            proj.netUpdate = true;
        }

        public void ReleasePlayer(int owner)
        {
            if (owner != Main.myPlayer) return;
            Player p = Main.player[owner];
            if (p != null && p.active)
                p.GetModPlayer<TuskAimPlayer>().aiming = false;
        }

        // ---------- BRAZO ----------
        public void ApplyArm(Player p)
        {
            if (active)
                p.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, aimAngle - MathHelper.PiOver2);
        }

        // ---------- ARO DE PARTÍCULAS ----------
        public void DrawRing(Player p)
        {
            bool pulsing = pulseTimer > 0;
            if (!active && !pulsing) return;

            Vector2 center = GetArmEnd(p);

            float radius = active ? RingBaseRadius + 1.5f : RingBaseRadius;

            if (pulsing)
            {
                float t = 1f - pulseTimer / (float)PulseDur;
                radius += (float)Math.Sin(t * MathHelper.Pi) * PulseExpand;
            }

            float intensity = active ? 1f : 0.9f;

            Color col = new Color(90, 200, 255, 0) * intensity;
            Color core = new Color(200, 240, 255, 0) * intensity;

            Texture2D px = TextureAssets.MagicPixel.Value;
            Vector2 pxOrigin = new Vector2(0.5f, 0.5f);

            int points = RingPoints + 4;

            for (int i = 0; i < points; i++)
            {
                float ang = MathHelper.TwoPi * i / points;
                Vector2 pos = center + ang.ToRotationVector2() * radius;

                Main.EntitySpriteDraw(px, pos - Main.screenPosition, new Rectangle(0, 0, 1, 1), col, 0f, pxOrigin, new Vector2(3f, 3f), SpriteEffects.None, 0f);
                Main.EntitySpriteDraw(px, pos - Main.screenPosition, new Rectangle(0, 0, 1, 1), core, 0f, pxOrigin, new Vector2(1.5f, 1.5f), SpriteEffects.None, 0f);
            }
        }
    }
}