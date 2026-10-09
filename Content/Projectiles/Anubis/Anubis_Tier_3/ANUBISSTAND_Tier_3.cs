using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using System;
using System.IO;
using Jojo.Content.Buffs;
using Jojo.Content.Habilidades;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.UI;

namespace Jojo.Content.Projectiles.Anubis.Anubis_Tier_3
{
    public class ANUBISSTAND_Tier_3 : ModProjectile
    {
        bool init, dying, fDash;
        int frame, animT, dashT;
        int attackTimer;

        public int baseCritChance = 1;
        public int attackSpeed = 1;

        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");
        static readonly SoundStyle SwingSoundBase = new("Terraria/Sounds/Item_1");

        ParticulasStands.StandRuntime runtime = new();

        float syncOffX, syncOffY;
        int netTimer = 0;

        public bool isAttacking = false;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(fDash);
            writer.Write(syncOffX);
            writer.Write(syncOffY);
            writer.Write(isAttacking);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            fDash = reader.ReadBoolean();
            syncOffX = reader.ReadSingle();
            syncOffY = reader.ReadSingle();
            isAttacking = reader.ReadBoolean();
        }

        public override void SetDefaults()
        {
            Projectile.width = 88;
            Projectile.height = 80;
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
            Projectile.timeLeft = 25;
            SoundEngine.PlaySound(DespawnSound, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
        }

        void UpdateAttackSpeed(Player p)
        {
            float s = p.GetModPlayer<StandStatsPlayer>().standSpeed;
            if (s < 50f) attackSpeed = 1;
            else if (s < 100f) attackSpeed = 2;
            else if (s < 150f) attackSpeed = 3;
            else attackSpeed = 4;
        }

        int GetAttackDelay()
        {
            switch (attackSpeed)
            {
                case 1: return 12;
                case 2: return 9;
                case 3: return 7;
                case 4: return 4;
                default: return 12;
            }
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            UpdateAttackSpeed(p);

            if (dying) { ParticulasStands.Despawn(Projectile, p); return; }
            if (!p.active || p.dead) { StartDying(); return; }

            if (p.TryGetModPlayer(out AnubisSoulPlayer_Tier_3 anubisPlayer))
            {
                anubisPlayer.AnubisEquipado = true;
            }

            if (!init)
            {
                init = true;
                Projectile.alpha = 0;
                SoundEngine.PlaySound(SpawnSound, Projectile.Center);
                ParticulasStands.Spawn(p, ParticulasStands.Stands.AnubisParticulas3);
            }

            var data = ParticulasStands.Stands.AnubisParticulas3;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);

            bool spawning = runtime.spawning;
            bool isOwner = Projectile.owner == Main.myPlayer;

            if (!spawning && isOwner)
            {
                HandleSkills(p);
            }

            if (!spawning)
            {
                UpdateSkills(p);
            }

            if (isOwner)
            {
                int direccionMiradaReal = p.direction;

                bool quiereAtacar = !spawning && Main.mouseLeft && !p.CCed && !p.mouseInterface;

                if (quiereAtacar)
                {
                    direccionMiradaReal = (Main.MouseWorld.X - p.MountedCenter.X >= 0) ? 1 : -1;
                    p.ChangeDir(direccionMiradaReal);
                }

                Vector2 off = new Vector2(-40 * direccionMiradaReal, -10);
                syncOffX = off.X;
                syncOffY = off.Y;

                Projectile.rotation = 0f;
                ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);

                isAttacking = quiereAtacar;

                if (quiereAtacar)
                {
                    if (attackTimer > 0) attackTimer--;

                    int tipoTajo = ModContent.ProjectileType<KatanaSlash_Tier_3>();
                    if (attackTimer <= 0 && p.ownedProjectileCounts[tipoTajo] < 1)
                    {
                        Vector2 direccionAtaque = Main.MouseWorld - p.MountedCenter;
                        direccionAtaque.Normalize();

                        float baseDamage = 20f;

                        if (anubisPlayer != null)
                        {
                            baseDamage *= anubisPlayer.AnubisDamageMultiplier;
                        }

                        int dañoFinal = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);

                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            p.MountedCenter,
                            direccionAtaque * 8f,
                            tipoTajo,
                            dañoFinal,
                            5f,
                            p.whoAmI
                        );

                        float pitch = attackSpeed >= 3 ? 0.05f : attackSpeed >= 2 ? 0.08f : 0f;
                        SoundEngine.PlaySound(SwingSoundBase with { Pitch = pitch }, p.Center);

                        attackTimer = GetAttackDelay();
                    }
                }
                else
                {
                    if (attackTimer > 0) attackTimer--;
                }

                if (++netTimer >= 5)
                { netTimer = 0; Projectile.netUpdate = true; }
            }
            else
            {
                Vector2 off = new Vector2(syncOffX, syncOffY);
                Projectile.rotation = 0f;
                ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);
            }

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);

            float damageCalculado = 30f;
            if (anubisPlayer != null)
            {
                damageCalculado *= anubisPlayer.AnubisDamageMultiplier;
            }
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(damageCalculado);

            if (!spawning)
            {
                Animate();
            }
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

        void HandleSkills(Player p)
        {
            if (p.whoAmI != Main.myPlayer) return;
            if (!ParticulasStands.CanUseSkills(runtime)) return;
            if (p.HasBuff(ModContent.BuffType<CD>())) return;

            if (JojoKeybinds.SkillF.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown1>()))
            {
                p.AddBuff(ModContent.BuffType<Cooldown1>(), 120);
                p.AddBuff(ModContent.BuffType<CD>(), 60);
                fDash = false;
                Projectile.netUpdate = true;
            }

            if (JojoKeybinds.SkillG.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown2>()))
            {
                p.AddBuff(ModContent.BuffType<Cooldown2>(), 120);
                p.AddBuff(ModContent.BuffType<CD>(), 60);
                Projectile.netUpdate = true;
            }

            if (JojoKeybinds.SkillH.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown3>()))
            {
                p.AddBuff(ModContent.BuffType<Cooldown3>(), 180);
                p.AddBuff(ModContent.BuffType<CD>(), 60);
                Projectile.netUpdate = true;
            }
        }

        void Animate()
        {
            if (++animT < 9) return;
            animT = 0;
            frame = (frame + 1) % 4;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player ownerPlayer = Main.player[Projectile.owner];
            var data = ParticulasStands.Stands.AnubisParticulas3;
            bool spawning = runtime.spawning;

            bool usandoSkinRed = StandSlotSystem.HasRedSkinFor(ownerPlayer);
            bool usandoSkinGreen = StandSlotSystem.HasGreenSkinFor(ownerPlayer);
            bool usandoSkinBlue = StandSlotSystem.HasBlueSkinFor(ownerPlayer);

            Texture2D tex;
            Rectangle r;
            Vector2 o;

            if (spawning)
            {
                string spawnTexPath = data.SpawnTexture;
                if (usandoSkinRed)
                {
                    spawnTexPath = "Jojo/Content/Projectiles/Skins/Anubis/Red/ANUBIS_Spawn";
                }
                else if (usandoSkinGreen)
                {
                    spawnTexPath = "Jojo/Content/Projectiles/Skins/Anubis/Green/ANUBIS_Spawn";
                }
                else if (usandoSkinBlue)
                {
                    spawnTexPath = "Jojo/Content/Projectiles/Skins/Anubis/Blue/ANUBIS_Spawn";
                }

                tex = ModContent.Request<Texture2D>(spawnTexPath).Value;

                int anchoNuevoFrame = 92;
                int altoNuevoFrame = 102;

                r = new Rectangle(runtime.frame * anchoNuevoFrame, 0, anchoNuevoFrame, altoNuevoFrame);
                o = new Vector2(anchoNuevoFrame / 2f, altoNuevoFrame / 2f);
                lightColor = Color.White;
            }
            else
            {
                string idleTexPath = data.IdleTexture;
                if (usandoSkinRed)
                {
                    idleTexPath = "Jojo/Content/Projectiles/Skins/Anubis/Red/ANUBISSTAND";
                }
                else if (usandoSkinGreen)
                {
                    idleTexPath = "Jojo/Content/Projectiles/Skins/Anubis/Green/ANUBISSTAND";
                }
                else if (usandoSkinBlue)
                {
                    idleTexPath = "Jojo/Content/Projectiles/Skins/Anubis/Blue/ANUBISSTAND";
                }

                tex = ModContent.Request<Texture2D>(idleTexPath).Value;
                r = new Rectangle(frame * 88, 0, 88, 98);
                o = new Vector2(44, 49);
            }

            bool flipVisual = syncOffX > 0;

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
}