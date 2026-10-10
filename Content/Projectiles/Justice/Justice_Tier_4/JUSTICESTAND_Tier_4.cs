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
using Jojo.Content.Buffs.Justice_Buffs;
using Jojo.Content.Habilidades;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Items;
using Jojo.Content.Projectiles.Justice.Justice_Tier_4.Marca;
using Jojo.Content.Projectiles.Justice.Justice_Tier_4.ControlMental;
using Jojo.Content.Items.Potenciadores;

namespace Jojo.Content.Projectiles.Justice.Justice_Tier_4
{
    public class JUSTICESTAND_Tier_4 : ModProjectile
    {
        // ====================================================================
        // CONFIGURACIÓN (los tiers 1, 2 y 3 heredan de esta clase y sobrescriben
        // solo los valores que quieran cambiar)
        // ====================================================================
        public virtual float MultVida => 1f;                    // vida del enemigo al ser minion (1 = sin boost)
        public virtual float MultDano => 1f;                    // daño del minion (1 = daño base del enemigo, sin boost)
        public virtual float MultDefensa => 1f;                 // defensa del minion (1 = defensa base)
        public virtual int CooldownGolpe => 45;                 // ticks entre golpes del minion (60 = 1 segundo)
        public virtual float RadioNiebla => 650f;               // tamaño de la niebla
        public virtual int TiempoInfeccion => 180;              // ticks dentro de la niebla para convertir a un enemigo en aliado (60 = 1 segundo)
        public virtual int DuracionMarca => 300;                // ticks que dura la marca (clic derecho)
        public virtual float DistanciaTeletransporte => 800f;   // si el minion está más lejos, vuelve a ti al instante (16 px = 1 bloque)
        public virtual float RadioBusquedaObjetivo => 1600f;    // distancia a la que los minions buscan enemigos

        // Interruptores de habilidades (false = el tier no tiene esa habilidad)
        public virtual bool TieneHabilidadF => true;            // Habilidad F (Aura Curativa)
        public virtual bool TieneHabilidadG => true;            // Habilidad G (reorganizar minions)

        // Cooldowns de las habilidades en ticks (60 = 1 segundo).
        // A estos valores se les aplica después la reducción de cooldown de tus stats.
        public virtual int CooldownHabilidad1 => 700;           // Habilidad F (Aura Curativa)
        public virtual int CooldownHabilidad2 => 700;           // Habilidad G (reorganizar minions)

        // Aura Curativa (habilidad F). Son multiplicadores: 1 = valores originales de la aura.
        public virtual float CuraEficacia => 1f;                // multiplicador de la cantidad de curación
        public virtual float RangoCuracion => 1f;               // multiplicador del tamaño/rango de la aura

        // Devuelve el stand de Justice (de cualquier tier) del jugador, o null
        public static JUSTICESTAND_Tier_4 ObtenerStand(int owner)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == owner && p.ModProjectile is JUSTICESTAND_Tier_4 s)
                    return s;
            }
            return null;
        }

        // Configuración del dueño. Si no tiene stand invocado, valores base del Tier 4.
        public static JUSTICESTAND_Tier_4 Config(int owner)
        {
            return ObtenerStand(owner) ?? ModContent.GetInstance<JUSTICESTAND_Tier_4>();
        }

        // ====================================================================

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

        float syncOffX, syncOffY;
        int netTimer = 0;

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

            if (s < 50f) { attackSpeed = 1; Projectile.localNPCHitCooldown = 6; }
            else if (s < 100f) { attackSpeed = 2; Projectile.localNPCHitCooldown = 5; }
            else if (s < 150f) { attackSpeed = 3; Projectile.localNPCHitCooldown = 4; }
            else { attackSpeed = 4; Projectile.localNPCHitCooldown = 3; }
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

            if (dying) { ParticulasStands.Despawn(Projectile, p); return; }
            if (!p.active || p.dead) { StartDying(); return; }

            if (!init)
            {
                init = true;
                Projectile.alpha = 0;
                Projectile.friendly = false;
                Projectile.damage = 0;
                SoundEngine.PlaySound(SpawnSound, Projectile.Center);
                ParticulasStands.Spawn(p, ParticulasStands.Stands.JusticeParticulas4);

                if (Projectile.owner == Main.myPlayer)
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<Justice_Niebla>(),
                        0,
                        0f,
                        Projectile.owner,
                        Projectile.whoAmI
                    );
                }
            }

            var data = ParticulasStands.Stands.JusticeParticulas4;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);

            bool spawning = runtime.spawning;
            bool isOwner = Projectile.owner == Main.myPlayer;

            if (!spawning && isOwner)
            {
                HandleSkills(p);
            }

            auto = false;

            if (!spawning)
            {
                UpdateSkills(p);
            }

            if (isOwner)
            {
                state = State.Idle;
                Projectile.friendly = false;

                Vector2 off = GetOffset(p);

                syncOffX = off.X;
                syncOffY = off.Y;

                Projectile.rotation = 0f;

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
                Projectile.rotation = 0f;
                ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);
            }

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);

            float baseDamage = 10f;
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);

            if (!spawning) Animate();
        }

        Vector2 GetOffset(Player p)
        {
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

            // HABILIDAD F (Aura Curativa) - solo si el tier la tiene (TieneHabilidadF)
            // cooldown: CooldownHabilidad1. Se le pasa eficacia (ai[0]) y rango (ai[1]).
            if (TieneHabilidadF && JojoKeybinds.SkillF.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown1>()))
            {
                float factorTiempoF = Math.Max(0f, 1f - stats.standCooldown1Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown1>(), (int)(CooldownHabilidad1 * factorTiempoF));
                p.AddBuff(ModContent.BuffType<CD>(), 60);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    p.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<Justice_HealingAura>(),
                    0,
                    0f,
                    p.whoAmI,
                    CuraEficacia,
                    RangoCuracion
                );

                Projectile.netUpdate = true;
            }

            // HABILIDAD G (Reorganizar) - solo si el tier la tiene (TieneHabilidadG)
            // cooldown: CooldownHabilidad2
            if (TieneHabilidadG && JojoKeybinds.SkillG.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown2>()))
            {
                float factorTiempoG = Math.Max(0f, 1f - stats.standCooldown2Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown2>(), (int)(CooldownHabilidad2 * factorTiempoG));
                p.AddBuff(ModContent.BuffType<CD>(), 60);

                // La señal se ejecuta en el servidor (o en singleplayer) para que funcione en multijugador
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    p.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<JusticeSenalReorganizar>(),
                    0,
                    0f,
                    p.whoAmI
                );

                Projectile.netUpdate = true;
            }

            // La habilidad H (3) fue eliminada.
        }

        NPC FindEnemy(Player p)
        {
            NPC best = null;
            float max = autoR;
            if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer)) max *= rangoPlayer.multiplicadorRango;

            foreach (NPC n in Main.npc)
            {
                if (!n.active || n.friendly || n.life <= 0) continue;

                if (n.GetGlobalNPC<JusticeGlobalNPC>().justiceMarkTimer > 0)
                {
                    return n;
                }
            }

            foreach (NPC n in Main.npc)
            {
                if (!n.active || n.friendly || n.life <= 0) continue;

                float d = Vector2.Distance(p.Center, n.Center);
                if (d < max) { best = n; max = d; }
            }

            return best;
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
            var data = ParticulasStands.Stands.JusticeParticulas4;
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
                r = new Rectangle(frame * 88, 0, 88, 76);
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

            return false;
        }
    }

    // ========================================================================
    // Señal de la habilidad G: teletransporta todos los minions del jugador a él.
    // Se ejecuta en el servidor / singleplayer (igual que JusticeSenalLiberar).
    // ========================================================================
    public class JusticeSenalReorganizar : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        private bool aplicado = false;

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 5;
            Projectile.hide = true;
        }

        public override bool? CanCutTiles() => false;

        public override void OnSpawn(IEntitySource source)
        {
            Aplicar();
        }

        public override void AI()
        {
            Aplicar();
        }

        private void Aplicar()
        {
            if (aplicado) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            aplicado = true;

            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) return;
            Player dueno = Main.player[Projectile.owner];
            if (!dueno.active || dueno.dead || string.IsNullOrEmpty(dueno.name)) return;

            int k = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active) continue;
                if (npc.realLife >= 0 && npc.realLife != npc.whoAmI) continue; // solo cabeza de los worms

                JusticeGlobalNPC g = npc.GetGlobalNPC<JusticeGlobalNPC>();
                if (!g.bajoControlMental || g.duenoNombre != dueno.name) continue;

                // Los reparte un poco para que no queden todos apilados
                float offX = ((k % 5) - 2) * 36f;
                float offY = -20f - (k / 5) * 30f;
                k++;

                npc.Center = dueno.Center + new Vector2(offX, offY);
                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}