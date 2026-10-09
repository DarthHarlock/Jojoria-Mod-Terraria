using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using Terraria.DataStructures;
using System;
using System.IO;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.KillerQueen_Buffs;
using Jojo.Systems;
using Jojo.Systems.StandFuncionComunes;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Items;
using Jojo.Content.Items.Potenciadores;
using Jojo.Content.UI;

namespace Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_1
{
    public class KILLERQUEENSTAND_Tier_1 : ModProjectile
    {
        enum State { Idle, Attack }
        State state;
        bool auto, init, dying;
        NPC target;
        int frame, animT, cd, attackTimer, swingSoundTimer;
        int bombAnimFrame, bombAnimTimer;
        bool bombAnim, forcedIdle;
        Vector2 bombPosCache;
        float manR = 120f, autoR = 120f, bombR = 200f, innerRadius = 90f;
        ParticulasStands.StandRuntime runtime = new();
        public int baseCritChance = 5;
        public int attackSpeed = 1;
        Projectile bombaActual;

        // Temporizador de espera para el ciclo de la bomba (Reducido para mayor velocidad)
        int bombCooldown = 0;

        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");
        static readonly SoundStyle SwingSoundBase = new("Terraria/Sounds/Item_1");
        static readonly SoundStyle BombPlaceSound = new("Jojo/Content/Sonidos/KillerQueenBomba1");

        float syncOffX, syncOffY;
        int netTimer = 0;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)state);
            writer.Write(bombAnim);
            writer.Write(bombAnimFrame);
            writer.Write(forcedIdle);
            writer.Write(syncOffX);
            writer.Write(syncOffY);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            state = (State)reader.ReadByte();
            bombAnim = reader.ReadBoolean();
            bombAnimFrame = reader.ReadInt32();
            forcedIdle = reader.ReadBoolean();
            syncOffX = reader.ReadSingle();
            syncOffY = reader.ReadSingle();
        }

        void ClearBomb()
        {
            if (bombaActual != null && bombaActual.active)
                bombaActual.Kill();

            bombaActual = null;
        }

        void StartBombAnimation(Player p, Vector2 bombPos)
        {
            bombAnim = true;
            bombAnimFrame = 0;
            bombAnimTimer = 0;
            forcedIdle = true;
            bombPosCache = bombPos;
            Projectile.netUpdate = true;
        }

        void UpdateAttackSpeed(Player p)
        {
            float s = p.GetModPlayer<StandStatsPlayer>().standSpeed;
            if (s < 50f) { attackSpeed = 1; Projectile.localNPCHitCooldown = 7; }
            else if (s < 100f) { attackSpeed = 2; Projectile.localNPCHitCooldown = 6; }
            else if (s < 150f) { attackSpeed = 3; Projectile.localNPCHitCooldown = 5; }
            else { attackSpeed = 4; Projectile.localNPCHitCooldown = 4; }
        }

        SoundStyle GetSwingSound(float speed)
        {
            float pitch = 0f;

            if (speed >= 100f)
                pitch = 0.05f;
            else if (speed >= 50f)
                pitch = 0.08f;

            return SwingSoundBase with { Pitch = pitch };
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
            Projectile.usesLocalNPCImmunity = false;
            Projectile.localNPCHitCooldown = 0;
            Projectile.timeLeft = 25;
            SoundEngine.PlaySound(DespawnSound, Projectile.Center);
            ClearBomb();
        }

        public override void OnKill(int timeLeft)
        {
            ClearBomb();
            SoundEngine.PlaySound(DespawnSound, Projectile.Center);
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            UpdateAttackSpeed(p);

            if (!p.active || p.dead)
            {
                ClearBomb();
            }

            if (dying)
            {
                ParticulasStands.Despawn(Projectile, p);
                ClearBomb();
                return;
            }

            if (!init)
            {
                init = true;
                Projectile.alpha = 0;
                Projectile.friendly = false;
                Projectile.damage = 0;
                SoundEngine.PlaySound(SpawnSound, Projectile.Center);
                ParticulasStands.Spawn(p, ParticulasStands.Stands.KillerP1);
            }

            var data = ParticulasStands.Stands.KillerP1;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);

            bool spawning = runtime.spawning;
            bool isOwner = Projectile.owner == Main.myPlayer;

            auto = Projectile.ai[0] == 1f;

            if (!spawning && isOwner)
            {
                HandleToggle(p);
                HandleSkills(p);
                HandleBombAbility(p);
            }

            if (!ParticulasStands.CanAttack(runtime)) ClearBomb();

            if (isOwner)
            {
                bool canAttack =
                    ParticulasStands.CanAttack(runtime) &&
                    (auto ? (target = FindEnemy(p)) != null : Main.mouseLeft);

                state = canAttack ? State.Attack : State.Idle;
                Projectile.friendly = canAttack;

                Vector2 off = GetOffset(p);

                float multRango = 1f;
                if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
                {
                    multRango = rangoPlayer.multiplicadorRango;
                }

                float max = (auto ? autoR : manR) * multRango;
                if (off.Length() > max) off = Vector2.Normalize(off) * max;

                syncOffX = off.X;
                syncOffY = off.Y;

                if (state == State.Attack && !bombAnim)
                {
                    if (off.Length() <= innerRadius)
                        Projectile.rotation = 0f;
                    else
                    {
                        float rot = off.ToRotation();
                        if (off.X < 0) rot += MathHelper.Pi;
                        Projectile.rotation = rot;
                    }
                }
                else Projectile.rotation = 0f;

                if (state == State.Attack && !spawning && !bombAnim)
                {
                    BarrageSystem.SpawnPunches(Projectile, p, off);

                    float s = p.GetModPlayer<StandStatsPlayer>().standSpeed;
                    int swingDelay = s >= 100f ? 6 : s >= 50f ? 8 : 11;

                    swingSoundTimer++;
                    if (swingSoundTimer >= swingDelay)
                    {
                        SoundEngine.PlaySound(GetSwingSound(s), Projectile.Center);
                        swingSoundTimer = 0;
                    }

                    if (attackSpeed > 0)
                    {
                        attackTimer++;
                        int delay = attackSpeed <= 0 ? 60 : 60 / attackSpeed;
                        if (delay < 1) delay = 1;

                        if (attackTimer >= delay)
                        {
                            attackTimer = 0;
                            Projectile.friendly = true;

                            Rectangle hitbox = Projectile.Hitbox;

                            foreach (NPC npc in Main.npc)
                            {
                                if (!npc.active || npc.life <= 0 || npc.friendly || npc.dontTakeDamage) continue;
                                if (!hitbox.Intersects(npc.Hitbox)) continue;
                                if (npc.immune[Projectile.owner] > 0) continue;

                                NPC.HitInfo hit = new()
                                {
                                    Damage = Projectile.damage,
                                    Knockback = 0f,
                                    HitDirection = p.direction,
                                    Crit = Main.rand.Next(100) < Projectile.CritChance
                                };

                                npc.StrikeNPC(hit);
                                npc.immune[Projectile.owner] = Projectile.localNPCHitCooldown;
                            }
                        }
                    }
                }
                else swingSoundTimer = 0;

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

                if (state == State.Attack && !bombAnim)
                {
                    if (off.Length() <= innerRadius)
                        Projectile.rotation = 0f;
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

            float baseDamage = 7f;
            Projectile.damage =
                (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>())
                .ApplyTo(baseDamage);

            if (!spawning) Animate(p);
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
                if (!auto) ClearBomb();
                Projectile.netUpdate = true;
                Main.NewText(auto ? "AutoStand-ON" : "AutoStand-OFF", 255, 255, 0);
            }
        }

        void HandleSkills(Player p)
        {
            if (p.whoAmI != Main.myPlayer) return;
            if (p.HasBuff(ModContent.BuffType<CD>())) return;

            StandStatsPlayer stats = p.GetModPlayer<StandStatsPlayer>();

            if (JojoKeybinds.SkillH.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown3>()))
            {
                float factorTiempoH = Math.Max(0f, 1f - stats.standCooldown3Reduction);
                int cooldownFinalH = (int)(240 * factorTiempoH);

                p.AddBuff(ModContent.BuffType<Cooldown3>(), cooldownFinalH);
                p.AddBuff(ModContent.BuffType<CD>(), 120);
            }
        }

        void HandleBombAbility(Player p)
        {
            if (p.whoAmI != Main.myPlayer) return;

            if (bombCooldown > 0) bombCooldown--;

            if (!Main.mouseRight || bombCooldown > 0) return;

            if (bombaActual != null && bombaActual.active)
            {
                Vector2 bombPos = bombaActual.Center;

                if (bombaActual.ModProjectile is BOMBA1_Tier_1 bomba) bomba.Explode(p);

                bombaActual = null;
                StartBombAnimation(p, bombPos);

                // Cooldown casi instantáneo para simular la velocidad original
                bombCooldown = 22;
                return;
            }

            Vector2 mouse = Main.MouseWorld;
            float currentBombR = bombR;

            if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
            {
                currentBombR *= rangoPlayer.multiplicadorRango;
            }

            foreach (NPC npc in Main.npc)
            {
                if (!npc.active || npc.life <= 0) continue;

                if (npc.Hitbox.Contains(mouse.ToPoint()))
                {
                    if (Vector2.Distance(npc.Center, p.Center) > currentBombR) return;

                    int projNPC = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        npc.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<BOMBA1_Tier_1>(),
                        0,
                        0f,
                        p.whoAmI
                    );

                    bombaActual = Main.projectile[projNPC];
                    if (bombaActual.ModProjectile is BOMBA1_Tier_1 b) b.AttachToNPC(npc);

                    SoundEngine.PlaySound(BombPlaceSound, p.Center);

                    bombCooldown = 6;
                    return;
                }
            }

            int tileX = (int)(mouse.X / 16f);
            int tileY = (int)(mouse.Y / 16f);
            Tile tile = Framing.GetTileSafely(tileX, tileY);

            if (!tile.HasTile || !Main.tileSolid[tile.TileType]) return;

            Vector2 centerBlock = new(tileX * 16 + 8, tileY * 16 + 8);

            if (Vector2.Distance(centerBlock, p.Center) > currentBombR) return;

            int proj = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                centerBlock,
                Vector2.Zero,
                ModContent.ProjectileType<BOMBA1_Tier_1>(),
                0,
                0f,
                p.whoAmI
            );

            bombaActual = Main.projectile[proj];
            SoundEngine.PlaySound(BombPlaceSound, p.Center);

            bombCooldown = 6;
        }

        void Animate(Player p)
        {
            if (bombAnim)
            {
                bombAnimTimer++;

                if (bombAnimTimer >= 8)
                {
                    bombAnimTimer = 0;
                    bombAnimFrame++;

                    if (bombAnimFrame >= 5)
                    {
                        bombAnim = false;
                        bombAnimFrame = 0;
                        forcedIdle = false;
                        Projectile.netUpdate = true;
                    }
                }
                return;
            }

            if (++animT < (state == State.Attack ? 4 : 8)) return;

            animT = 0;

            if (state == State.Idle)
            {
                frame++;
                if (frame > 3) frame = 0;
            }
            else
            {
                frame++;
                if (frame < 4 || frame > 7) frame = 4;
            }
        }

        Vector2 GetOffset(Player p)
        {
            if (forcedIdle) return new Vector2(-40 * p.direction, -10);
            if (state == State.Attack) return auto && target != null ? target.Center - p.Center : Main.MouseWorld - p.Center;
            return new Vector2(-40 * p.direction, -10);
        }

        // Auto-apuntado: solo hostiles (ignora pacíficos, critters, town NPCs y dummies).
        // Aplica el multiplicador de rango internamente.
        NPC FindEnemy(Player p)
        {
            return StandTargeting.FindHostileEnemy(p, autoR);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player p = Main.player[Projectile.owner];
            var data = ParticulasStands.Stands.KillerP1;
            bool spawning = runtime.spawning;

            bool megumin = StandSlotSystem.HasMeguminSkinFor(p);
            bool isRed = StandSlotSystem.HasKillerQueenRedSkinFor(p);
            bool isBlue = StandSlotSystem.HasKillerQueenBlueSkinFor(p);
            bool isGreen = StandSlotSystem.HasKillerQueenGreenSkinFor(p);

            Texture2D tex;
            Rectangle r;
            Vector2 o;

            if (bombAnim)
            {
                string path;
                if (megumin) path = "Jojo/Content/Projectiles/Skins/KillerQueen/Megumin/KILLERQUEENSTAND_Megumin_Bomba1";
                else if (isRed) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Red/KILLERQUEENSTAND_Red_Bomba1";
                else if (isBlue) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Blue/KILLERQUEENSTAND_Blue_Bomba1";
                else if (isGreen) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Green/KILLERQUEENSTAND_Green_Bomba1";
                else path = "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_1/KILLERQUEENSTAND_Tier_1_Bomba1";

                tex = ModContent.Request<Texture2D>(path).Value;
                r = new Rectangle(bombAnimFrame * 88, 0, 88, 76);
                o = new Vector2(44, 36);
            }
            else if (spawning)
            {
                string path;
                if (megumin) path = "Jojo/Content/Projectiles/Skins/KillerQueen/Megumin/KQ_SPAWN_Megumin";
                else if (isRed) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Red/KQ_SPAWN_Red";
                else if (isBlue) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Blue/KQ_SPAWN_Blue";
                else if (isGreen) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Green/KQ_SPAWN_Green";
                else path = data.SpawnTexture;

                tex = ModContent.Request<Texture2D>(path).Value;
                r = new Rectangle(runtime.frame * data.FrameWidth, 0, data.FrameWidth, data.SpawnHeight);
                o = new Vector2(data.FrameWidth / 2f, data.SpawnHeight / 2f);
            }
            else
            {
                string path;
                if (megumin) path = "Jojo/Content/Projectiles/Skins/KillerQueen/Megumin/KILLERQUEENSTAND_Megumin";
                else if (isRed) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Red/KILLERQUEENSTAND_Red";
                else if (isBlue) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Blue/KILLERQUEENSTAND_Blue";
                else if (isGreen) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Green/KILLERQUEENSTAND_Green";
                else path = "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_1/KILLERQUEENSTAND_Tier_1";

                tex = ModContent.Request<Texture2D>(path).Value;
                r = new Rectangle(frame * 88, 0, 88, 76);
                o = new Vector2(44, 36);
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
}