using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using System;
using System.IO;
using System.Collections.Generic;
using Jojo.Content.Buffs;
using Jojo.Systems.StandFuncionComunes;
using Jojo.Content.Habilidades;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Items;
using Jojo.Content.Items.Potenciadores;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_2.Nubes_Tier_2;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_2.Tornado_Tier_2;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_2.LluviaLetal_Tier_2;

namespace Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_2
{
    public class WEATHERSTAND_Tier_2 : ModProjectile
    {
        enum State { Idle, Attack }
        State state;

        bool auto, init, dying, fDash;
        NPC target;

        int frame, animT, cd, dashT;
        int attackTimer, swingSoundTimer;

        float manR = 160f, autoR = 160f;
        float innerRadius = 90f;

        ParticulasStands.StandRuntime runtime = new();

        public int baseCritChance = 5;
        public int attackSpeed = 1;

        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");
        static readonly SoundStyle SwingSoundBase = new("Terraria/Sounds/Item_1");
        static readonly SoundStyle TornadoSkillSound = new("Jojo/Content/Sonidos/WeatherReport/Tornado");

        float syncOffX, syncOffY;
        int netTimer = 0;

        public int tornadoDamage = 0;

        List<NubeVisual> nubesDeFondo = new List<NubeVisual>();

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)state);
            writer.Write(auto);
            writer.Write(fDash);
            writer.Write(syncOffX);
            writer.Write(syncOffY);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            state = (State)reader.ReadByte();
            auto = reader.ReadBoolean();
            fDash = reader.ReadBoolean();
            syncOffX = reader.ReadSingle();
            syncOffY = reader.ReadSingle();
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
            Projectile.ArmorPenetration = 1000;
        }

        public void StartDying()
        {
            if (dying) return;
            dying = true;
            Projectile.friendly = false;
            Projectile.timeLeft = 25;
            SoundEngine.PlaySound(DespawnSound, Projectile.Center);

            // Desvincular del sistema de Stand activo del jugador
            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
            {
                Player p = Main.player[Projectile.owner];
                if (p.TryGetModPlayer(out StandStatsPlayer standStats) && standStats.activeStand == Projectile)
                {
                    standStats.activeStand = null;
                }
            }
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

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            UpdateAttackSpeed(p);

            GestorDeNubes();

            if (dying) { ParticulasStands.Despawn(Projectile, p); return; }
            if (!p.active || p.dead) { StartDying(); return; }

            if (!init)
            {
                init = true;
                Projectile.alpha = 0;
                Projectile.friendly = false;
                Projectile.damage = 0;
                SoundEngine.PlaySound(SpawnSound, Projectile.Center);
                ParticulasStands.Spawn(p, ParticulasStands.Stands.WeatherParticulas2);

                if (p.TryGetModPlayer(out StandStatsPlayer standStats))
                {
                    standStats.activeStand = Projectile;
                }
            }

            var data = ParticulasStands.Stands.WeatherParticulas2;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);

            bool spawning = runtime.spawning;
            bool isOwner = Projectile.owner == Main.myPlayer;

            if (!spawning && isOwner)
            {
                HandleSkills(p);
                HandleToggle(p);
            }

            auto = Projectile.ai[0] == 1f;

            if (!spawning)
            {
                UpdateSkills(p);
            }

            if (isOwner)
            {
                bool canAttack = ParticulasStands.CanAttack(runtime) && (auto ? (target = FindEnemy(p)) != null : Main.mouseLeft);

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

                syncOffX = off.X;
                syncOffY = off.Y;

                if (state == State.Attack)
                {
                    BarrageSystem.SpawnPunches(Projectile, p, off);

                    float s = p.GetModPlayer<StandStatsPlayer>().standSpeed;
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
                    }
                }
                else swingSoundTimer = 0;

                if (state == State.Attack)
                {
                    if (off.Length() <= innerRadius) Projectile.rotation = 0f;
                    else
                    {
                        float rot = off.ToRotation();
                        if (off.X < 0) rot += MathHelper.Pi;
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

                if (state == State.Attack)
                {
                    if (off.Length() <= innerRadius) Projectile.rotation = 0f;
                    else
                    {
                        float rot = off.ToRotation();
                        if (off.X < 0) rot += MathHelper.Pi;
                        Projectile.rotation = rot;
                    }
                }
                else Projectile.rotation = 0f;

                ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);
            }

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);

            float baseDamage = 10f;
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);

            if (!spawning)
            {
                Animate();
            }
        }

        void GestorDeNubes()
        {
            if (!dying && Main.rand.NextBool(14))
            {
                int texturaRandom = Main.rand.Next(1, 5);
                Vector2 offsetInicial = new Vector2(Main.rand.Next(-45, 15), Main.rand.Next(-35, 25));
                float velocidadX = Main.rand.NextFloat(0.25f, 0.5f);
                int tiempoDeVida = Main.rand.Next(45, 75);
                float escala = Main.rand.NextFloat(0.85f, 1.15f);

                bool dibujarEnFrente = Main.rand.NextFloat() < 0.40f;

                nubesDeFondo.Add(new NubeVisual(offsetInicial, velocidadX, tiempoDeVida, texturaRandom, escala, dibujarEnFrente));
            }

            for (int i = nubesDeFondo.Count - 1; i >= 0; i--)
            {
                nubesDeFondo[i].Update(dying);

                if (nubesDeFondo[i].alpha <= 0f && nubesDeFondo[i].isDying)
                {
                    nubesDeFondo.RemoveAt(i);
                }
            }
        }

        Vector2 GetOffset(Player p)
        {
            if (fDash) return (Main.MouseWorld - p.Center) * 0.35f;

            if (state == State.Attack)
                return auto && target != null
                    ? target.Center - p.Center
                    : Main.MouseWorld - p.Center;

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

            if (JojoKeybinds.SkillF.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown1>()))
            {
                float factorTiempoF = Math.Max(0f, 1f - stats.standCooldown1Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown1>(), (int)(900 * factorTiempoF));
                p.AddBuff(ModContent.BuffType<CD>(), 0);
                fDash = false;

                SoundEngine.PlaySound(TornadoSkillSound, p.Center);
                SpawnTornado(p);
                Projectile.netUpdate = true;
            }

            if (Main.mouseRight && Main.mouseRightRelease)
            {
                Vector2 targetPos = Main.MouseWorld;

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    p.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<Nube2_Tier_2>(),
                    20,
                    0f,
                    p.whoAmI,
                    targetPos.X,
                    targetPos.Y
                );

                Projectile.netUpdate = true;
            }
        }

        void SpawnTornado(Player p)
        {
            float multRango = 1f;
            if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
            {
                multRango = rangoPlayer.multiplicadorRango;
            }

            float rangoExclusivoTornado = 450f;
            float maxRange = rangoExclusivoTornado * multRango;

            Vector2 dir = Main.MouseWorld - p.Center;
            if (dir.Length() > maxRange)
                dir = dir.SafeNormalize(Vector2.UnitX) * maxRange;

            Vector2 spawnPos = p.Center + dir;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPos,
                Vector2.Zero,
                ModContent.ProjectileType<TornadoWeather_Tier_2>(),
                tornadoDamage,
                0f,
                p.whoAmI
            );
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
            foreach (var nube in nubesDeFondo)
            {
                if (!nube.enFrente)
                {
                    Texture2D nubeTex = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/WeatherReport/WeatherReport_Tier_2/Nubes_Tier_2/Nube" + nube.textureIndex).Value;
                    Vector2 pos = Projectile.Center + nube.offset - Main.screenPosition;
                    Vector2 origin = nubeTex.Size() / 2f;

                    Main.EntitySpriteDraw(
                        nubeTex, pos, null, lightColor * nube.alpha, 0f, origin, nube.scale, SpriteEffects.None, 0
                    );
                }
            }

            Player p = Main.player[Projectile.owner];
            var data = ParticulasStands.Stands.WeatherParticulas2;
            bool spawning = runtime.spawning;

            Texture2D tex;
            Rectangle r;
            Vector2 o;

            if (spawning)
            {
                tex = ModContent.Request<Texture2D>(data.SpawnTexture).Value;
                r = new Rectangle(runtime.frame * data.FrameWidth, 0, data.FrameWidth, data.SpawnHeight);
                o = new Vector2(data.FrameWidth / 2f, data.SpawnHeight / 2f);
                lightColor = Color.White;
            }
            else
            {
                tex = ModContent.Request<Texture2D>(data.IdleTexture).Value;
                r = new Rectangle(frame * 88, 0, 88, 88);
                o = new Vector2(44, 49);
            }

            bool flipVisual = syncOffX < 0;

            Main.EntitySpriteDraw(
                tex,
                Projectile.Center - Main.screenPosition,
                r,
                lightColor * ((255 - Projectile.alpha) / 255f),
                Projectile.rotation,
                o,
                Projectile.scale,
                flipVisual ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                0f
            );

            foreach (var nube in nubesDeFondo)
            {
                if (nube.enFrente)
                {
                    Texture2D nubeTex = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/WeatherReport/WeatherReport_Tier_2/Nubes_Tier_2/Nube" + nube.textureIndex).Value;
                    Vector2 pos = Projectile.Center + nube.offset - Main.screenPosition;
                    Vector2 origin = nubeTex.Size() / 2f;

                    Main.EntitySpriteDraw(
                        nubeTex, pos, null, lightColor * nube.alpha, 0f, origin, nube.scale, SpriteEffects.None, 0
                    );
                }
            }

            return false;
        }
    }
}