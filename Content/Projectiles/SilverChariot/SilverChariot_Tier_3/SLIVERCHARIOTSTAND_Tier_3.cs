using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using Terraria.DataStructures;
using System;
using System.IO;
using System.Collections.Generic;
using Jojo.Content.Buffs;
using Jojo.Systems.StandFuncionComunes;
using Jojo.Content.Buffs.SilverChariot_Buffs;
using Jojo.Content.Habilidades;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Items;
using Jojo.Content.Items.Potenciadores;
using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_3;
using Jojo.Content.UI;

namespace Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_3
{
    public class SLIVERCHARIOTSTAND_Tier_3 : ModProjectile
    {
        enum State { Idle, Attack }
        State state;

        bool auto, init, dying, fDash;
        bool habilidadHActiva;
        bool prevHabilidadHActiva;
        bool habilidadGActiva;
        NPC target;

        int frame, animT, cd, dashT;
        int attackTimer, swingSoundTimer;

        // === VARIABLES HABILIDAD DISPARO ===
        bool fShoot, shotFired, shootReturning;
        int shootFrame, shootTimer, shootT;
        Vector2 aimDir;
        // ====================================

        float manR = 145f, autoR = 145f;
        float innerRadius = 90f;

        ParticulasStands.StandRuntime runtime = new();

        public int baseCritChance = 5;
        public int attackSpeed = 1;

        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");
        static readonly SoundStyle SwingSoundBase = new("Terraria/Sounds/Item_1");

        float syncOffX, syncOffY;

        // === Vector maestro de apuntado (estabilizado) ===
        float direccionAtaqueX, direccionAtaqueY;
        bool lastConfidentGoingRight = true;
        // ==================================================

        int netTimer = 0;

        // === RUTA BASE DE TEXTURAS SEGÚN SKIN ===
        const string PathNormal = "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Tier_3/";
        const string PathGolden = "Jojo/Content/Projectiles/Skins/SilverChariot/Golden/";

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)state);
            writer.Write(auto);
            writer.Write(fDash);
            writer.Write(syncOffX);
            writer.Write(syncOffY);

            // Sincronización del vector maestro
            writer.Write(direccionAtaqueX);
            writer.Write(direccionAtaqueY);
            writer.Write(lastConfidentGoingRight);

            // Sincronizamos los estados de las habilidades
            writer.Write(habilidadHActiva);
            writer.Write(habilidadGActiva);

            // Sincronización disparo
            writer.Write(fShoot);
            writer.Write(shootReturning);
            writer.Write(shootFrame);
            writer.Write(aimDir.X);
            writer.Write(aimDir.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            state = (State)reader.ReadByte();
            auto = reader.ReadBoolean();
            fDash = reader.ReadBoolean();
            syncOffX = reader.ReadSingle();
            syncOffY = reader.ReadSingle();

            direccionAtaqueX = reader.ReadSingle();
            direccionAtaqueY = reader.ReadSingle();
            lastConfidentGoingRight = reader.ReadBoolean();

            habilidadHActiva = reader.ReadBoolean();
            habilidadGActiva = reader.ReadBoolean();

            fShoot = reader.ReadBoolean();
            shootReturning = reader.ReadBoolean();
            shootFrame = reader.ReadInt32();
            aimDir = new Vector2(reader.ReadSingle(), reader.ReadSingle());
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
            if (s < 50f) { attackSpeed = 1; Projectile.localNPCHitCooldown = 7; }
            else if (s < 100f) { attackSpeed = 2; Projectile.localNPCHitCooldown = 6; }
            else if (s < 150f) { attackSpeed = 3; Projectile.localNPCHitCooldown = 5; }
            else { attackSpeed = 4; Projectile.localNPCHitCooldown = 4; }
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

        // === LÓGICA DE DISPARO ===
        void UpdateShoot(Player p)
        {
            if (!fShoot) return;
            shootT++; shootTimer++;
            if (shootTimer >= 5)
            {
                shootTimer = 0;
                if (!shootReturning)
                {
                    if (shootFrame < 2) shootFrame++;
                    if (shootFrame == 2 && !shotFired)
                    {
                        shotFired = true;
                        SoundEngine.PlaySound(SoundID.Item17, Projectile.Center);
                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            Projectile.Center + aimDir * 20f,
                            aimDir * 14f,
                            ModContent.ProjectileType<ShotChariot_Tier_3>(),
                            1,
                            2f,
                            p.whoAmI
                        );
                    }
                    if (shootFrame >= 2 && shootT > 20)
                    {
                        shootReturning = true;
                        Projectile.netUpdate = true;
                    }
                }
                else
                {
                    if (shootFrame > 0) shootFrame--;
                }
            }
            if (shootReturning && shootFrame <= 0)
            {
                fShoot = false;
                shootReturning = false;
                shootT = 0;
                shootFrame = 0;
                shootTimer = 0;
                shotFired = false;
                Projectile.netUpdate = true;
            }
        }
        // =========================

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            UpdateAttackSpeed(p);

            if (dying) { ParticulasStands.Despawn(Projectile, p); return; }
            if (!p.active || p.dead) { StartDying(); return; }

            bool isOwner = Projectile.owner == Main.myPlayer;

            // EL ARREGLO PRINCIPAL: Solo el dueño comprueba los buffs físicamente.
            if (isOwner)
            {
                bool prevH = habilidadHActiva;
                bool prevG = habilidadGActiva;

                habilidadHActiva = p.HasBuff(ModContent.BuffType<SilverClones>());
                habilidadGActiva = p.HasBuff(ModContent.BuffType<SilverDefensa>());

                if (prevH != habilidadHActiva || prevG != habilidadGActiva)
                {
                    Projectile.netUpdate = true;
                }
            }

            // === SINCRONIZACIÓN VISUAL (GORE, POLVO Y SONIDO) PARA MULTIJUGADOR ===
            if (habilidadHActiva && !prevHabilidadHActiva)
            {
                if (Main.netMode != Terraria.ID.NetmodeID.Server)
                {
                    IEntitySource source = Projectile.GetSource_FromThis();

                    SoundEngine.PlaySound(new SoundStyle("Jojo/Content/Sonidos/SilverChariotArmorOFF"), Projectile.Center);
                    SoundEngine.PlaySound(Terraria.ID.SoundID.Item34, Projectile.Center);

                    Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);

                    Gore.NewGore(source, Projectile.Center, velocity, ModContent.Find<ModGore>("Jojo/SilverChariot1").Type);
                    Gore.NewGore(source, Projectile.Center, velocity, ModContent.Find<ModGore>("Jojo/SilverChariot2").Type);
                    Gore.NewGore(source, Projectile.Center, velocity, ModContent.Find<ModGore>("Jojo/SilverChariot3").Type);
                    Gore.NewGore(source, Projectile.Center, velocity, ModContent.Find<ModGore>("Jojo/SilverChariot4").Type);
                    Gore.NewGore(source, Projectile.Center, velocity, ModContent.Find<ModGore>("Jojo/SilverChariot5").Type);

                    for (int i = 0; i < 40; i++)
                    {
                        Vector2 velocidadHumo = Main.rand.NextVector2Circular(7f, 7f);
                        Dust humo = Dust.NewDustPerfect(
                            Projectile.Center,
                            Terraria.ID.DustID.Smoke,
                            velocidadHumo,
                            100,
                            Color.Gray,
                            Main.rand.NextFloat(1.5f, 3.0f)
                        );
                        humo.noGravity = true;
                    }

                    for (int i = 0; i < 15; i++)
                    {
                        Vector2 velocidadGoreHumo = Main.rand.NextVector2Circular(4f, 4f);
                        Gore.NewGore(source, Projectile.Center, velocidadGoreHumo, Main.rand.Next(61, 64));
                    }

                    int cantidadDeDust = 35;
                    for (int i = 0; i < cantidadDeDust; i++)
                    {
                        Vector2 velocidadDust = Main.rand.NextVector2Circular(6f, 6f);
                        Dust d = Dust.NewDustPerfect(
                            Projectile.Center,
                            Terraria.ID.DustID.SilverCoin,
                            velocidadDust,
                            100,
                            Color.White,
                            Main.rand.NextFloat(1.2f, 2.0f)
                        );
                        d.noGravity = true;
                    }
                }
            }
            prevHabilidadHActiva = habilidadHActiva;
            // ======================================================================

            if (!init)
            {
                init = true;
                Projectile.alpha = 0;
                Projectile.friendly = false;
                Projectile.damage = 0;
                syncOffX = -40 * p.direction;
                lastConfidentGoingRight = p.direction >= 0; // FIX: coherencia con la dirección real al invocar
                SoundEngine.PlaySound(SpawnSound, Projectile.Center);
                ParticulasStands.Spawn(p, ParticulasStands.Stands.SilverCharitoParticulas3);
            }

            var data = ParticulasStands.Stands.SilverCharitoParticulas3;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);

            bool spawning = runtime.spawning;

            if (!spawning && isOwner)
            {
                HandleSkills(p);
                HandleToggle(p);
            }

            auto = Projectile.ai[0] == 1f;

            if (!spawning)
            {
                if (habilidadGActiva && !habilidadHActiva) // No hacer la defensa si están los clones
                {
                    // FIX: ahora también se pasa lastConfidentGoingRight por referencia, para que
                    // se actualice DURANTE la defensa (antes se quedaba congelada porque el AI()
                    // hace return justo debajo de este bloque, sin llegar nunca al código que
                    // normalmente la actualiza).
                    if (ChariotDefensaSkill_Tier_3.EjecutarDefensa(Projectile, p, ref frame, ref animT, ref syncOffX, ref syncOffY, ref lastConfidentGoingRight))
                    {
                        Projectile.friendly = false;
                        if (++netTimer >= 5)
                        {
                            netTimer = 0;
                            Projectile.netUpdate = true;
                        }
                        return;
                    }
                }

                UpdateSkills(p);
                UpdateShoot(p);
            }

            if (isOwner)
            {
                bool canAttack;
                if (auto)
                {
                    target = FindEnemy(p);
                    canAttack = !habilidadHActiva && ParticulasStands.CanAttack(runtime) && target != null;
                }
                else
                {
                    target = null;
                    canAttack = !habilidadHActiva && ParticulasStands.CanAttack(runtime) && Main.mouseLeft;
                }

                state = canAttack ? State.Attack : State.Idle;
                Projectile.friendly = canAttack;

                Vector2 off = GetOffset(p);

                float multRango = 1f;
                if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
                {
                    multRango = rangoPlayer.multiplicadorRango;
                }

                float max = (auto ? autoR : manR) * multRango;

                if (off.Length() > max)
                    off = Vector2.Normalize(off) * max;

                // 1. Posición física del Stand (relativa al JUGADOR)
                syncOffX = off.X;
                syncOffY = off.Y;

                // 2. Estabilización contra el lag de cámara al correr
                if (Math.Abs(off.X) > 40f)
                {
                    lastConfidentGoingRight = off.X >= 0;
                }

                bool isGoingRight = lastConfidentGoingRight;

                // 3. VECTOR MAESTRO DE APUNTADO
                Vector2 aimVector;
                if (off.Length() <= innerRadius)
                {
                    aimVector = new Vector2(isGoingRight ? 1f : -1f, 0f);
                }
                else
                {
                    aimVector = off;
                }

                direccionAtaqueX = aimVector.X;
                direccionAtaqueY = aimVector.Y;

                if (state == State.Attack && !habilidadHActiva)
                {
                    Vector2 direccionObjetivo = aimVector;
                    if (direccionObjetivo == Vector2.Zero) direccionObjetivo = new Vector2(isGoingRight ? 1f : -1f, 0f);
                    direccionObjetivo.Normalize();

                    float s = p.GetModPlayer<StandStatsPlayer>().standSpeed;
                    int cantidadEstocadas = 2;

                    if (s >= 150f) cantidadEstocadas = 5;
                    else if (s >= 100f) cantidadEstocadas = 4;
                    else if (s >= 50f) cantidadEstocadas = 3;

                    IEntitySource source = Projectile.GetSource_FromThis();

                    // Congelamos el ángulo de ataque YA estabilizado (aimVector) en el momento
                    // exacto del disparo, y lo empaquetamos en ai[0] (libre para las estocadas
                    // del stand principal).
                    float attackAngle = direccionObjetivo.ToRotation();
                    float anguloNormalizado = (attackAngle + MathHelper.Pi) / (MathHelper.TwoPi + 0.01f);

                    int swingDelay = GetSwingDelay(s);
                    swingSoundTimer++;
                    if (swingSoundTimer >= swingDelay)
                    {
                        SoundEngine.PlaySound(GetSwingSound(s), Projectile.Center);
                        swingSoundTimer = 0;
                    }

                    attackTimer++;
                    int delay = GetAttackDelay();

                    if (attackTimer >= delay)
                    {
                        attackTimer = 0;
                        Projectile.friendly = true;

                        for (int i = 0; i < cantidadEstocadas; i++)
                        {
                            Projectile.NewProjectile(source, Projectile.Center, direccionObjetivo * 42f, ModContent.ProjectileType<Estocada_Tier_3>(), 0, 0f, p.whoAmI, anguloNormalizado, 0f);
                        }
                    }
                }
                else if (!habilidadHActiva) swingSoundTimer = 0;

                // 4. Rotación del Stand enlazada al vector maestro
                if (state == State.Attack)
                {
                    if (off.Length() <= innerRadius) Projectile.rotation = 0f;
                    else
                    {
                        float rot = aimVector.ToRotation();
                        if (!isGoingRight) rot += MathHelper.Pi;
                        Projectile.rotation = rot;
                    }
                }
                else Projectile.rotation = 0f;

                ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);

                if (++netTimer >= 5)
                {
                    netTimer = 0;
                    Projectile.netUpdate = true;
                }
            }
            else
            {
                Vector2 off = new Vector2(syncOffX, syncOffY);
                bool isGoingRight = lastConfidentGoingRight;
                Vector2 aimVector = (off.Length() <= innerRadius) ? new Vector2(isGoingRight ? 1f : -1f, 0f) : new Vector2(direccionAtaqueX, direccionAtaqueY);

                if (state == State.Attack)
                {
                    if (off.Length() <= innerRadius) Projectile.rotation = 0f;
                    else
                    {
                        float rot = aimVector.ToRotation();
                        if (!isGoingRight) rot += MathHelper.Pi;
                        Projectile.rotation = rot;
                    }
                }
                else Projectile.rotation = 0f;

                ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);
            }

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);

            float baseDamage = 20f;
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);

            // === LÓGICA DE INVISIBILIDAD FÍSICA ===
            if (habilidadHActiva)
            {
                Projectile.alpha = 255;
                Projectile.friendly = false;
            }
            else
            {
                if (!spawning)
                {
                    Projectile.alpha = 0;
                    Animate();
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (habilidadHActiva) return false;

            if (projHitbox.Intersects(targetHitbox))
                return true;

            if (state == State.Attack)
            {
                Vector2 direccionAtaque = new Vector2(direccionAtaqueX, direccionAtaqueY);
                if (direccionAtaque == Vector2.Zero) direccionAtaque = new Vector2(Main.player[Projectile.owner].direction, 0f);
                direccionAtaque.Normalize();

                Vector2 origenEstocada = Projectile.Center + (direccionAtaque * -59f);

                float longitudDeGolpe = 155f;
                int grosorDeLinea = 60;

                Vector2 destinoEstocada = origenEstocada + (direccionAtaque * longitudDeGolpe);

                float point = 0f;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), origenEstocada, destinoEstocada, grosorDeLinea, ref point))
                {
                    return true;
                }
            }
            return false;
        }

        Vector2 GetOffset(Player p)
        {
            if (habilidadHActiva) return new Vector2(-40 * p.direction, -10);

            if (fShoot) return aimDir * 60f;
            if (fDash) return (Main.MouseWorld - p.Center) * 0.35f;

            if (state == State.Attack)
            {
                if (auto && target != null)
                    return target.Center - p.Center;
                else if (!auto)
                    return Main.MouseWorld - p.Center;
                else
                    return new Vector2(-40 * p.direction, -10);
            }

            return new Vector2(-40 * p.direction, -10);
        }

        void UpdateSkills(Player p)
        {
            if (fDash && ++dashT > 6)
            {
                fDash = false;
                dashT = 0;
                if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
            }
        }

        void HandleToggle(Player p)
        {
            if (cd-- > 0 || p.whoAmI != Main.myPlayer) return;

            if (JojoKeybinds.ToggleAuto.JustPressed)
            {
                auto = !auto;
                Projectile.ai[0] = auto ? 1f : 0f;
                cd = 30;
                target = null;

                if (Main.netMode != Terraria.ID.NetmodeID.Server)
                {
                    Main.NewText(auto ? "AutoStand-ON" : "AutoStand-OFF", 255, 255, 0);
                }
                Projectile.netUpdate = true;
            }
        }

        void HandleSkills(Player p)
        {
            if (p.whoAmI != Main.myPlayer) return;
            if (!ParticulasStands.CanUseSkills(runtime)) return;
            if (p.HasBuff(ModContent.BuffType<CD>())) return;

            StandStatsPlayer stats = p.GetModPlayer<StandStatsPlayer>();

            // Habilidad F — Disparo
            if (JojoKeybinds.SkillF.JustPressed && !fShoot && !p.HasBuff(ModContent.BuffType<Cooldown1>()) && !p.HasBuff(ModContent.BuffType<SilverClones>()))
            {
                aimDir = Vector2.Normalize(Main.MouseWorld - Projectile.Center);
                if (aimDir == Vector2.Zero) aimDir = new Vector2(p.direction, 0);
                fShoot = true;
                shootReturning = false;
                shootT = 0;
                shootFrame = 0;
                shootTimer = 0;
                shotFired = false;

                float factorTiempoF = Math.Max(0f, 1f - stats.standCooldown1Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown1>(), (int)(720 * factorTiempoF));
                p.AddBuff(ModContent.BuffType<CD>(), 60);
                fDash = false;
                Projectile.netUpdate = true;
            }

            // Habilidad G — DEFENSA (PARRY)
            if (JojoKeybinds.SkillG.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown2>()) && !p.HasBuff(ModContent.BuffType<SilverClones>()))
            {
                float factorTiempoG = Math.Max(0f, 1f - stats.standCooldown2Reduction);

                p.AddBuff(ModContent.BuffType<SilverDefensa>(), 60);
                p.AddBuff(ModContent.BuffType<Cooldown2>(), (int)(80 * factorTiempoG));
                p.AddBuff(ModContent.BuffType<CD>(), 60);
                Projectile.netUpdate = true;
            }

            // Habilidad H — Clones
            if (JojoKeybinds.SkillH.JustPressed && !p.HasBuff(ModContent.BuffType<SilverClones>()) && !p.HasBuff(ModContent.BuffType<Cooldown3>()))
            {
                float factorTiempoH = Math.Max(0f, 1f - stats.standCooldown3Reduction);

                p.AddBuff(ModContent.BuffType<SilverClones>(), 600);
                p.AddBuff(ModContent.BuffType<Cooldown3>(), (int)(450 * factorTiempoH));
                p.AddBuff(ModContent.BuffType<CD>(), 60);

                IEntitySource source = Projectile.GetSource_FromThis();

                for (int i = 0; i < 7; i++)
                {
                    Projectile.NewProjectile(source, Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ChariotClon_Tier_3>(), Projectile.damage, 0f, p.whoAmI, i, Projectile.Center.X, Projectile.Center.Y);
                }

                Projectile.netUpdate = true;
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

            frame = state == State.Idle
                ? (frame + 1) % 4
                : (frame < 4 || frame > 7 ? 4 : (frame + 1 > 7 ? 4 : frame + 1));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player p = Main.player[Projectile.owner];

            if (habilidadHActiva)
            {
                return false;
            }

            bool goldenSkin = StandSlotSystem.HasGoldenSkinFor(p);
            string pathBase = goldenSkin ? PathGolden : PathNormal;

            var data = ParticulasStands.Stands.SilverCharitoParticulas3;
            bool spawning = runtime.spawning;

            Texture2D tex;
            Rectangle r;
            Vector2 o;
            float drawRotation;
            SpriteEffects drawEffects;

            if (spawning)
            {
                tex = ModContent.Request<Texture2D>(goldenSkin ? pathBase + "SILVERCHARIOT_Spawn_Golden" : data.SpawnTexture).Value;
                r = new Rectangle(runtime.frame * data.FrameWidth, 0, data.FrameWidth, data.SpawnHeight);
                o = new Vector2(data.FrameWidth / 2f, data.SpawnHeight / 2f);
                lightColor = Color.White;
                drawRotation = Projectile.rotation;

                drawEffects = p.direction > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                Main.EntitySpriteDraw(
                    tex,
                    Projectile.Center - Main.screenPosition,
                    r,
                    lightColor * ((255 - Projectile.alpha) / 255f),
                    drawRotation,
                    o,
                    Projectile.scale,
                    drawEffects,
                    0f
                );
                return false;
            }
            else if (habilidadGActiva)
            {
                tex = ModContent.Request<Texture2D>(goldenSkin ? pathBase + "ChariotDefensaSkill_Golden" : PathNormal + "ChariotDefensaSkill_Tier_3").Value;
                r = new Rectangle((frame % 8) * 88, 0, 88, 92);
                o = new Vector2(44, 46);

                bool proyectilALaIzquierda = !lastConfidentGoingRight;
                drawEffects = proyectilALaIzquierda ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                drawRotation = Projectile.rotation;

                Main.EntitySpriteDraw(
                    tex,
                    Projectile.Center - Main.screenPosition,
                    r,
                    lightColor * ((255 - Projectile.alpha) / 255f),
                    drawRotation,
                    o,
                    Projectile.scale,
                    drawEffects,
                    0f
                );
                return false;
            }
            else if (fShoot)
            {
                tex = ModContent.Request<Texture2D>(goldenSkin ? pathBase + "ShotChariot_Golden" : PathNormal + "ShotChariot_Tier_3").Value;
                r = new Rectangle(shootFrame * 88, 0, 88, 92);
                o = new Vector2(44, 46);

                if (aimDir.X >= 0)
                {
                    drawRotation = aimDir.ToRotation();
                    drawEffects = SpriteEffects.None;
                }
                else
                {
                    drawRotation = (float)Math.Atan2(-aimDir.Y, -aimDir.X);
                    drawEffects = SpriteEffects.FlipHorizontally;
                }

                Main.EntitySpriteDraw(
                    tex,
                    Projectile.Center - Main.screenPosition,
                    r,
                    lightColor * ((255 - Projectile.alpha) / 255f),
                    drawRotation,
                    o,
                    Projectile.scale,
                    drawEffects,
                    0f
                );
                return false;
            }
            else
            {
                tex = ModContent.Request<Texture2D>(goldenSkin ? pathBase + "SLIVERCHARIOTSTAND_Golden" : data.IdleTexture).Value;
                r = new Rectangle(frame * 88, 0, 88, 98);
                o = new Vector2(44, 49);
                drawRotation = Projectile.rotation;

                bool flipVisual = (state == State.Attack) ? !lastConfidentGoingRight : (syncOffX < 0);
                drawEffects = flipVisual ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                Main.EntitySpriteDraw(
                    tex,
                    Projectile.Center - Main.screenPosition,
                    r,
                    lightColor * ((255 - Projectile.alpha) / 255f),
                    drawRotation,
                    o,
                    Projectile.scale,
                    drawEffects,
                    0f
                );
                return false;
            }
        }
    }
}