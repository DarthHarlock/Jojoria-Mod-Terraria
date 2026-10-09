using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using System;
using System.IO;
using Jojo.Systems.StandFuncionComunes;
using Jojo.Content.Buffs;
using Jojo.Content.Habilidades;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Projectiles.Tusk.Tusk_Tier_1.Disparo;

namespace Jojo.Content.Projectiles.Tusk.Tusk_Tier_1
{
    public class TUSKSTAND_Tier_1 : ModProjectile
    {
        bool init, dying;
        int frame, animT;
        int cd; // enfriamiento de la tecla de cambio de modo

        public int baseCritChance = 5;

        // Toda la lógica de apuntar / cargar / disparar / aro vive aquí
        readonly TuskAimController aim = new();

        // Habilidad: ráfaga de disparos con click derecho
        readonly Abocajarro abocajarro = new();

        ParticulasStands.StandRuntime runtime = new();

        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");

        // Modo libre: ai[0] == 1 -> el Tusk NO ataca, solo te sigue y recuperas inventario/armas.
        // (NoUsarInventarioConStand ya lee stand.ai[0] == 1f para devolverte el control)
        bool FreeMode => Projectile.ai[0] == 1f;

        public override void SendExtraAI(BinaryWriter writer)
        {
            aim.Write(writer);
            abocajarro.Write(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            aim.Read(reader);
            abocajarro.Read(reader);
        }

        public override void SetDefaults()
        {
            Projectile.width = 88;
            Projectile.height = 76;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.friendly = false; // el stand no hace daño, solo la bala
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
        }

        public void StartDying()
        {
            if (dying) return;
            dying = true;
            aim.Cancel(Projectile);
            abocajarro.Cancel(Projectile); // cancela la ráfaga, conservas las balas
            Projectile.friendly = false;
            Projectile.timeLeft = 25;
            SoundEngine.PlaySound(DespawnSound, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            aim.ReleasePlayer(Projectile.owner);
            abocajarro.ReleasePlayer(Projectile.owner);
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];

            if (dying) { ParticulasStands.Despawn(Projectile, p); return; }
            if (!p.active || p.dead) { StartDying(); return; }

            if (!init)
            {
                init = true;
                Projectile.alpha = 0;
                Projectile.friendly = false;
                Projectile.damage = 0;
                SoundEngine.PlaySound(SpawnSound, Projectile.Center);
                ParticulasStands.Spawn(p, ParticulasStands.Stands.Tusk1);
            }

            var data = ParticulasStands.Stands.Tusk1;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);

            bool spawning = runtime.spawning;
            bool isOwner = Projectile.owner == Main.myPlayer;

            if (!spawning && isOwner)
            {
                HandleSkills(p);
                HandleToggle(p);
            }

            // Solo puede atacar si NO está en modo libre
            bool canUse = !spawning && !FreeMode && ParticulasStands.CanAttack(runtime);

            // Click izquierdo: disparo normal (se bloquea mientras dura la ráfaga)
            aim.Update(Projectile, p, canUse && !abocajarro.Active, baseCritChance);

            // Click derecho: Abocajarro (se actualiza después para que su brazo/bloqueo tengan prioridad)
            abocajarro.Update(Projectile, p, canUse, baseCritChance, aim.Aiming);

            if (!spawning && !FreeMode)
            {
                aim.ApplyArm(p);
                abocajarro.ApplyArm(p);
            }

            // El stand SOLO flota y sigue al jugador (igual que RIKASTAND en Idle)
            Vector2 off = new Vector2(-40 * p.direction, -10);
            ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);
            Projectile.rotation = 0f;

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);
            Projectile.damage = 0;

            if (!spawning)
                Animate();
        }

        // Cambia entre modo disparo y modo libre (misma tecla que usa Rika)
        void HandleToggle(Player p)
        {
            if (cd > 0) { cd--; return; }
            if (p.whoAmI != Main.myPlayer) return;

            if (JojoKeybinds.ToggleAuto.JustPressed)
            {
                bool nowFree = !FreeMode;
                Projectile.ai[0] = nowFree ? 1f : 0f;
                cd = 30;

                // Al pasar a modo libre, corta cualquier apuntado o ráfaga en curso
                if (nowFree)
                {
                    aim.Cancel(Projectile);
                    abocajarro.Cancel(Projectile);
                }

                if (Main.netMode != Terraria.ID.NetmodeID.Server)
                {
                    Main.NewText(nowFree ? "AutoStand-ON" : "AutoStand-OFF", 255, 255, 0);
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

            // Skill F (Cooldown1)
            if (JojoKeybinds.SkillF.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown1>()))
            {
                float factorTiempoF = Math.Max(0f, 1f - stats.standCooldown1Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown1>(), (int)(700 * factorTiempoF));
                p.AddBuff(ModContent.BuffType<CD>(), 60);
                Projectile.netUpdate = true;
            }
        }

        // Solo animación Idle
        void Animate()
        {
            if (++animT < 9) return;
            animT = 0;
            frame = (frame + 1) % 4;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player p = Main.player[Projectile.owner];
            var data = ParticulasStands.Stands.Tusk1;
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

            // Mismo espejo que RIKASTAND en Idle
            float offX = -40 * p.direction;
            bool flipVisual = offX < 0;

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

            if (!spawning && !FreeMode)
            {
                aim.DrawRing(p);
                abocajarro.DrawRing(p);
            }

            return false;
        }
    }
}