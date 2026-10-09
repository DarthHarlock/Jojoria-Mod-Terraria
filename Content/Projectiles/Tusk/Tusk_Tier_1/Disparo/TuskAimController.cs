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
    // Controla apuntado, carga, disparo, brazo, aro de partículas de Tusk y gestión de balas.
    public class TuskAimController
    {
        const bool UsarClickDerecho = false;

        const int BalaDamageNormal = 15; //dañode la bala
        const int BalaDamagePesada = 25;
        const float VelocidadNormal = 11f;
        const float VelocidadPesada = 15f;

        // Sonido del disparo de la bala normal (la pesada usa los suyos)
        static readonly SoundStyle ShotSound = new("Jojo/Content/Sonidos/Tusk/Shot1");

        // ---------- PRECISIÓN (0 a 100, 100 = perfecta) ----------
        const int PrecisionNormal = 60;
        const int PrecisionPesada = 95;   // la pesada es MUY precisa
        // Desviación máxima (radianes) cuando la precisión es 0. 0.35 rad ≈ 20 grados
        const float DesvioMaximo = 0.35f;

        const int ChargeMax = 60;
        const int ShootCooldownTicks = 10;

        const float ArmLength = 30f;
        static readonly Vector2 ShoulderOffset = new Vector2(0f, -6f);
        const int PulseDur = 24;
        const float RingBaseRadius = 5f;
        const float PulseExpand = 24f;
        const int RingPoints = 12;

        bool aiming;
        float aimAngle;
        sbyte facing = 1;   // hacia dónde mira el cuerpo (1 = derecha, -1 = izquierda). SINCRONIZADO
        int charge;
        public byte shotCount; // Ahora es public
        byte lastShotCount;
        byte previousShotCountClient; // Para sincronizar la barra en clientes
        int pulseTimer;

        int shootCooldown;
        bool prevClick;
        bool readySoundPlayed;
        int netTimer;

        public bool Aiming => aiming;

        public void Write(BinaryWriter writer)
        {
            writer.Write(aiming);
            writer.Write(aimAngle);
            writer.Write((short)charge);
            writer.Write(shotCount);
            writer.Write(facing);
        }

        public void Read(BinaryReader reader)
        {
            aiming = reader.ReadBoolean();
            aimAngle = reader.ReadSingle();
            charge = reader.ReadInt16();
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

        public void Update(Projectile proj, Player p, bool canUse, int baseCritChance)
        {
            TuskAimPlayer tuskP = p.GetModPlayer<TuskAimPlayer>();

            if (proj.owner == Main.myPlayer)
            {
                HandleAim(proj, p, canUse, baseCritChance, tuskP);
                tuskP.aiming = aiming;
            }
            else // Multijugador: si vemos que disparó, deducimos su barra visualmente
            {
                if (shotCount != previousShotCountClient)
                {
                    tuskP.ConsumirBala();
                    previousShotCountClient = shotCount;
                }

                // Multijugador: el cuerpo del jugador remoto mira hacia donde apunta
                if (aiming)
                    p.direction = facing;
            }

            // Datos del brazo para que la onda de recarga salga de ahí (local y remoto)
            tuskP.armAiming = aiming;
            tuskP.armAngle = aimAngle;

            if (pulseTimer > 0) pulseTimer--;
        }

        public void ApplyArm(Player p)
        {
            if (aiming)
                p.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, aimAngle - MathHelper.PiOver2);
        }

        void HandleAim(Projectile proj, Player p, bool canUse, int baseCritChance, TuskAimPlayer tuskP)
        {
            if (shootCooldown > 0) shootCooldown--;

            bool click = UsarClickDerecho ? Main.mouseRight : Main.mouseLeft;

            if (!aiming)
            {
                if (click && !prevClick && canUse && shootCooldown <= 0 && !p.mouseInterface)
                {
                    if (tuskP.municion > 0) // <--- Comprobación de munición
                    {
                        aiming = true;
                        charge = 0;
                        readySoundPlayed = false;
                        UpdateAimAngle(p);
                        proj.netUpdate = true;
                    }
                    else
                    {
                        // Se quedó sin balas, tiembla y suena a error
                        tuskP.TriggerEmpty();
                        SoundEngine.PlaySound(SoundID.Camera with { Pitch = -0.5f, Volume = 0.5f }, p.Center);
                    }
                }
            }
            else
            {
                // Si el arma deja de poder usarse o la munición llega a 0 mágicamente, cancela
                if (!canUse || tuskP.municion <= 0)
                {
                    Cancel(proj);
                }
                else
                {
                    sbyte oldFacing = facing;
                    UpdateAimAngle(p);

                    // Si giras el cuerpo, avisa enseguida a los demás clientes
                    if (facing != oldFacing)
                        proj.netUpdate = true;

                    if (click)
                    {
                        charge++;

                        if (charge >= ChargeMax && !readySoundPlayed)
                        {
                            readySoundPlayed = true;
                            SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 1f, Pitch = 0.3f }, p.Center);
                            proj.netUpdate = true;
                        }

                        if (++netTimer >= 3)
                        {
                            netTimer = 0;
                            proj.netUpdate = true;
                        }
                    }
                    else
                    {
                        Fire(proj, p, charge >= ChargeMax, baseCritChance);
                        tuskP.ConsumirBala(); // <--- Gasta una bala al disparar
                        aiming = false;
                        charge = 0;
                        shootCooldown = ShootCooldownTicks;
                        proj.netUpdate = true;
                    }
                }
            }

            prevClick = click;
        }

        void UpdateAimAngle(Player p)
        {
            Vector2 toMouse = Main.MouseWorld - GetShoulder(p);
            if (toMouse != Vector2.Zero)
                aimAngle = toMouse.ToRotation();

            int dir = Main.MouseWorld.X >= p.Center.X ? 1 : -1;
            facing = (sbyte)dir;
            p.ChangeDir(dir);
        }

        // Desvío aleatorio (arriba o abajo) según la precisión: 100 = 0 desvío
        float GetDeviation(int precision)
        {
            precision = Math.Clamp(precision, 0, 100);
            float maxDev = DesvioMaximo * (1f - precision / 100f);
            return Main.rand.NextFloat(-maxDev, maxDev);
        }

        void Fire(Projectile proj, Player p, bool heavy, int baseCritChance)
        {
            // La dirección apuntada se desvía según la precisión
            int precision = heavy ? PrecisionPesada : PrecisionNormal;
            Vector2 dir = (aimAngle + GetDeviation(precision)).ToRotationVector2();

            Vector2 spawnPos = GetArmEnd(p);

            int baseDmg = heavy ? BalaDamagePesada : BalaDamageNormal;
            float speed = heavy ? VelocidadPesada : VelocidadNormal;
            int dmg = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDmg);

            int idx = Projectile.NewProjectile(
                proj.GetSource_FromThis(),
                spawnPos,
                dir * speed,
                ModContent.ProjectileType<TuskBala>(),
                dmg,
                heavy ? 5f : 2f,
                proj.owner,
                heavy ? 1f : 0f
            );

            if (idx >= 0 && idx < Main.maxProjectiles)
                Main.projectile[idx].CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);

            shotCount++;
            lastShotCount = shotCount;
            previousShotCountClient = shotCount; // Para el host
            pulseTimer = PulseDur;

            // El efecto visual del disparo sigue la dirección real de la bala
            Vector2 fxDir = dir;

            if (heavy)
            {
                // Bala grande: mantiene sus sonidos originales
                SoundEngine.PlaySound(SoundID.Item38 with { Volume = 0.9f, Pitch = -0.3f }, spawnPos);
                SoundEngine.PlaySound(SoundID.Item40 with { Volume = 0.8f, Pitch = -0.1f }, spawnPos);

                for (int i = 0; i < 12; i++)
                {
                    Vector2 v = fxDir.RotatedByRandom(0.5f) * Main.rand.NextFloat(2f, 6f);
                    Dust d = Dust.NewDustPerfect(spawnPos, DustID.BlueTorch, v, 100, default, 1.4f);
                    d.noGravity = true;
                }
            }
            else
            {
                // Bala normal: sonido nuevo Shot1
                SoundEngine.PlaySound(ShotSound, spawnPos);
            }
        }

        public void Cancel(Projectile proj)
        {
            aiming = false;
            charge = 0;
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

        public void DrawRing(Player p)
        {
            bool pulsing = pulseTimer > 0;
            if (!aiming && !pulsing) return;

            Vector2 center = GetArmEnd(p);
            bool ready = aiming && charge >= ChargeMax;

            float radius = ready ? RingBaseRadius + 1.5f : RingBaseRadius;

            if (pulsing)
            {
                float t = 1f - pulseTimer / (float)PulseDur;
                radius += (float)Math.Sin(t * MathHelper.Pi) * PulseExpand;
            }

            float intensity = ready ? 1f : 0.75f;
            if (!aiming) intensity = 0.9f;

            Color col = new Color(90, 200, 255, 0) * intensity;
            Color core = new Color(200, 240, 255, 0) * intensity;

            Texture2D px = TextureAssets.MagicPixel.Value;
            Vector2 pxOrigin = new Vector2(0.5f, 0.5f);

            int points = ready ? RingPoints + 4 : RingPoints;

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