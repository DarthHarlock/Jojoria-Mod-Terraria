using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Jojo.Systems.StandFuncionComunes;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Audio;
using System;
using System.IO;
using System.Collections.Generic;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.KingCrimson_Buffs;
using Jojo.Content.Habilidades;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Items;
using Jojo.Content.Items.Potenciadores;

namespace Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_4
{
    public class KingCrimsonBackgroundDrawer_Tier_4 : ModProjectile
    {
        public override string Texture => "Jojo/Content/Projectiles/KingCrimson/KingCrimson_Tier_4/KingCrimsonBackground";

        private float fadeProgress = 0f;
        private const float FadeSpeed = 0.05f;
        private int duracionHabilidad = 300;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.hide = true;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead) { Projectile.Kill(); return; }
            Projectile.Center = player.Center;
            if (duracionHabilidad > 0) { duracionHabilidad--; fadeProgress = Math.Min(fadeProgress + FadeSpeed, 1f); }
            else { fadeProgress -= FadeSpeed; if (fadeProgress <= 0f) Projectile.Kill(); }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
            => behindNPCsAndTiles.Add(index);

        public override bool PreDraw(ref Color lightColor)
        {
            if (fadeProgress <= 0f) return false;
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int w = texture.Width, h = texture.Height;
            int startX = (int)(-(Main.screenPosition.X * 0.3f) % w);
            int startY = (int)(-(Main.screenPosition.Y * 0.3f) % h);
            if (startX > 0) startX -= w;
            if (startY > 0) startY -= h;
            for (int x = startX; x < Main.screenWidth + w; x += w)
                for (int y = startY; y < Main.screenHeight + h; y += h)
                    Main.spriteBatch.Draw(texture, Main.screenPosition + new Vector2(x, y), null,
                        Color.White * fadeProgress, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class KINGCRIMSONSTAND_Tier_4 : ModProjectile
    {
        enum State { Idle, Attack }
        State state;
        bool auto, init, dying, fDash;
        NPC target;
        int frame, animT, cd, dashT;
        float manR = 150f, autoR = 150f, innerRadius = 90f;
        ParticulasStands.StandRuntime runtime = new();
        public int baseCritChance = 5;
        public int attackSpeed = 1;
        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");
        static readonly SoundStyle SwingSoundBase = new("Terraria/Sounds/Item_1");
        float syncOffX, syncOffY;
        int netTimer = 0;
        bool isTimeErasing = false;
        int aTimer = 0, sTimer = 0, heavyAttackCooldown = 0;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((int)state);
            writer.Write(auto);
            writer.Write(fDash);
            writer.Write(syncOffX);
            writer.Write(syncOffY);
            writer.Write(isTimeErasing);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            state = (State)reader.ReadInt32();
            auto = reader.ReadBoolean();
            fDash = reader.ReadBoolean();
            syncOffX = reader.ReadSingle();
            syncOffY = reader.ReadSingle();
            isTimeErasing = reader.ReadBoolean();
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
            Player p = Main.player[Projectile.owner];
            if (p.HasBuff(ModContent.BuffType<Donut1>())) p.ClearBuff(ModContent.BuffType<Donut1>());
            if (p.HasBuff(ModContent.BuffType<Donut2>())) p.ClearBuff(ModContent.BuffType<Donut2>());
            SoundEngine.PlaySound(DespawnSound, Projectile.Center);
        }

        // ─────────────────────────────────────────────────────────────
        // FIX: Este hook se ejecuta SIEMPRE que el proyectil del stand
        // muere (Kill()), sin importar si fue por despawn manual, por
        // el fade-out de StartDying(), o por cualquier otro motivo.
        // A diferencia del chequeo que había en AI() (que dependía de
        // que el proyectil siguiera vivo para detectar que el buff
        // TimeErased ya no estaba), esto garantiza que la habilidad 2
        // (Time Erase) se cierre de forma natural -exactamente igual
        // que si presionaras la tecla de la habilidad dos veces- aunque
        // el stand se haya despawneado a mitad de la habilidad.
        // Esto evita que TimeEraseNetHandler.timeEraseActive se quede
        // "pegado" en true y que las sombras se sigan generando para
        // siempre en los NPCs.
        // ─────────────────────────────────────────────────────────────
        public override void OnKill(int timeLeft)
        {
            if (isTimeErasing && Projectile.owner == Main.myPlayer)
            {
                Player p = Main.player[Projectile.owner];
                isTimeErasing = false;

                if (p.HasBuff(ModContent.BuffType<TimeErased>()))
                    p.ClearBuff(ModContent.BuffType<TimeErased>());

                TimeEraseNetHandler.SendTimeEraseEnd(p.whoAmI);
            }
        }

        void UpdateAttackSpeed(Player p)
        {
            float s = p.GetModPlayer<StandStatsPlayer>().standSpeed;
            if (s < 50f) { attackSpeed = 1; Projectile.localNPCHitCooldown = 8; } // 6
            else if (s < 100f) { attackSpeed = 2; Projectile.localNPCHitCooldown = 7; }
            else if (s < 150f) { attackSpeed = 3; Projectile.localNPCHitCooldown = 6; }
            else { attackSpeed = 4; Projectile.localNPCHitCooldown = 5; }
        }

        int GetAttackDelay() => attackSpeed switch { 1 => 5, 2 => 4, 3 => 3, 4 => 2, _ => 5 };
        SoundStyle GetSwingSound(float speed) => SwingSoundBase with { Pitch = speed >= 100f ? 0.05f : speed >= 50f ? 0.08f : 0f };
        int GetSwingDelay(float speed) => speed >= 100f ? 6 : speed >= 50f ? 8 : 11;

        public override void AI()
        {
            Projectile.timeLeft = 2;
            Player p = Main.player[Projectile.owner];
            UpdateAttackSpeed(p);

            if (isTimeErasing && Projectile.owner == Main.myPlayer)
            {
                if (!p.HasBuff(ModContent.BuffType<TimeErased>()))
                {
                    isTimeErasing = false;
                    TimeEraseNetHandler.SendTimeEraseEnd(p.whoAmI);
                    Projectile.netUpdate = true;
                }
            }

            if (dying) { ParticulasStands.Despawn(Projectile, p); return; }
            if (!p.active || p.dead) { StartDying(); return; }

            if (!init)
            {
                init = true;
                Projectile.alpha = 0;
                Projectile.friendly = false;
                Projectile.damage = 0;
                SoundEngine.PlaySound(SpawnSound, Projectile.Center);
                ParticulasStands.Spawn(p, ParticulasStands.Stands.KingCrimsonParticulas3);
            }

            var data = ParticulasStands.Stands.KingCrimsonParticulas3;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);
            bool spawning = runtime.spawning;
            bool isOwner = Projectile.owner == Main.myPlayer;

            if (!spawning && isOwner) { HandleSkills(p); HandleToggle(p); }

            auto = Projectile.ai[0] == 1f;
            if (!spawning) UpdateSkills(p);

            bool heavyAttackActive = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.type == ModContent.ProjectileType<KingCrimsonHeavyAttack_Tier_4>() && proj.owner == Projectile.owner)
                { heavyAttackActive = true; break; }
            }

            if (heavyAttackCooldown > 0) heavyAttackCooldown--;

            if (isOwner)
            {
                if (heavyAttackActive)
                {
                    state = State.Idle;
                    Projectile.friendly = false;
                    Vector2 off = new Vector2(-40 * p.direction, -10);
                    syncOffX = off.X; syncOffY = off.Y;
                    Projectile.rotation = 0f;
                    ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);
                    if (++netTimer >= 5) { netTimer = 0; Projectile.netUpdate = true; }
                }
                else
                {
                    if (Main.mouseRight && !spawning && heavyAttackCooldown == 0)
                    {
                        Vector2 mouseOff = Main.MouseWorld - p.Center;
                        float multRango = 1f;
                        if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer)) multRango = rangoPlayer.multiplicadorRango;
                        float max = manR * multRango;
                        if (mouseOff.Length() > max) mouseOff = Vector2.Normalize(mouseOff) * max;
                        Vector2 spawnPos = p.Center + mouseOff;

                        float baseDmg = 200f; //DAÑO GOLPE HEAVY

                        if (p.HasBuff(ModContent.BuffType<Donut2>())) baseDmg *= 30f;
                        else if (p.HasBuff(ModContent.BuffType<Donut1>())) baseDmg *= 10f; //multiplicador?

                        int dañoHeavy = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDmg);
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                            ModContent.ProjectileType<KingCrimsonHeavyAttack_Tier_4>(),
                            dañoHeavy, Projectile.knockBack * 2f, p.whoAmI, spawnPos.X, spawnPos.Y);
                        heavyAttackCooldown = 70;
                        Projectile.netUpdate = true;
                    }
                    else
                    {
                        bool canAttack = ParticulasStands.CanAttack(runtime) && (auto ? (target = FindEnemy(p)) != null : Main.mouseLeft);
                        state = canAttack ? State.Attack : State.Idle;
                        Projectile.friendly = canAttack;
                        Vector2 off = GetOffset(p);
                        float multRango = 1f;
                        if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer)) multRango = rangoPlayer.multiplicadorRango;
                        float maxR = (auto ? autoR : manR) * multRango;
                        if (off.Length() > maxR) off = Vector2.Normalize(off) * maxR;
                        syncOffX = off.X; syncOffY = off.Y;
                        if (state == State.Attack)
                        {
                            BarrageSystem.SpawnPunches(Projectile, p, off);
                            float s = p.GetModPlayer<StandStatsPlayer>().standSpeed;
                            if (++sTimer >= GetSwingDelay(s)) { SoundEngine.PlaySound(GetSwingSound(s), Projectile.Center); sTimer = 0; }
                            if (++aTimer >= GetAttackDelay()) { aTimer = 0; Projectile.friendly = true; }
                        }
                        else sTimer = 0;
                        Projectile.rotation = (state == State.Attack && off.Length() > innerRadius)
                            ? (off.X < 0 ? off.ToRotation() + MathHelper.Pi : off.ToRotation()) : 0f;
                        ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);
                        if (++netTimer >= 5) { netTimer = 0; Projectile.netUpdate = true; }
                    }
                }
            }
            else
            {
                Vector2 off = new Vector2(syncOffX, syncOffY);
                Projectile.rotation = (state == State.Attack && off.Length() > innerRadius)
                    ? (off.X < 0 ? off.ToRotation() + MathHelper.Pi : off.ToRotation()) : 0f;
                ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);
            }

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);
            Projectile.damage = heavyAttackActive ? 0 : (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(75f); //DAÑO de los golpes
            if (!spawning) Animate();
        }

        Vector2 GetOffset(Player p)
        {
            if (fDash) return (Main.MouseWorld - p.Center) * 0.35f;
            if (state == State.Attack)
                return auto && target != null ? target.Center - p.Center : Main.MouseWorld - p.Center;
            return new Vector2(-40 * p.direction, -10);
        }

        void UpdateSkills(Player p)
        {
            if (fDash && ++dashT > 6) { fDash = false; dashT = 0; if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true; }
        }

        void HandleToggle(Player p)
        {
            if (cd-- > 0 || p.whoAmI != Main.myPlayer) return;
            if (JojoKeybinds.ToggleAuto.JustPressed)
            {
                auto = !auto;
                Projectile.ai[0] = auto ? 1f : 0f;
                cd = 30; target = null;
                if (Main.netMode != Terraria.ID.NetmodeID.Server)
                    Main.NewText(auto ? "AutoStand-ON" : "AutoStand-OFF", 255, 255, 0);
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
                float factorF = Math.Max(0f, 1f - stats.standCooldown1Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown1>(), (int)(1200 * factorF));
                p.AddBuff(ModContent.BuffType<PrediccionKingCrimson>(), 300);
                p.AddBuff(ModContent.BuffType<CD>(), 0);
                p.AddBuff(ModContent.BuffType<EpitaphVision>(), 300);
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item8, p.Center);
                Projectile.netUpdate = true;
            }

            if (JojoKeybinds.SkillG.JustPressed)
            {
                if (p.HasBuff(ModContent.BuffType<TimeErased>()))
                {
                    p.ClearBuff(ModContent.BuffType<TimeErased>());
                    Projectile.netUpdate = true;
                    return;
                }
                if (TimeEraseNetHandler.timeEraseLocked)
                {
                    string alertMsg = Language.ActiveCulture.Name switch
                    {
                        "es-ES" => "El tiempo ya está siendo borrado...",
                        "pt-BR" => "O tempo já está sendo apagado...",
                        "ru-RU" => "Время уже стирается...",
                        "zh-Hans" => "时间已被抹去...",
                        "ja-JP" => "時間は既に消し飛ばされている...",
                        _ => "Time is already being erased..."
                    };
                    Main.NewText(alertMsg, 200, 50, 50);
                    return;
                }
                if (!p.HasBuff(ModContent.BuffType<Cooldown2>()))
                {
                    float factorG = Math.Max(0f, 1f - stats.standCooldown2Reduction);
                    p.AddBuff(ModContent.BuffType<Cooldown2>(), (int)(3000 * factorG));
                    p.AddBuff(ModContent.BuffType<CD>(), 60);
                    p.AddBuff(ModContent.BuffType<TimeErased>(), 660);
                    isTimeErasing = true;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), p.Center, Vector2.Zero,
                        ModContent.ProjectileType<TimeErasedEffect_Tier_4>(), 0, 0, p.whoAmI);
                    TimeEraseNetHandler.SendTimeEraseStart(p.whoAmI, 3);
                    Projectile.netUpdate = true;
                }
            }

            if (JojoKeybinds.SkillH.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown3>()))
            {
                Vector2 destino = Main.MouseWorld;
                Point tilePos = destino.ToTileCoordinates();
                Tile tile = Framing.GetTileSafely(tilePos.X, tilePos.Y);
                Tile tileAbove = Framing.GetTileSafely(tilePos.X, tilePos.Y - 1);
                bool bloqueado = (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType])
                                 || (tileAbove.HasUnactuatedTile && Main.tileSolid[tileAbove.TileType]);
                if (!bloqueado)
                {
                    float factorH = Math.Max(0f, 1f - stats.standCooldown1Reduction);
                    p.AddBuff(ModContent.BuffType<Cooldown3>(), (int)(900 * factorH));
                    p.AddBuff(ModContent.BuffType<CD>(), 0);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), p.Center, Vector2.Zero,
                        ModContent.ProjectileType<KingCrimsonTeleport_Tier_4>(), 0, 0f, p.whoAmI, destino.X, destino.Y);
                    Projectile.netUpdate = true;
                }
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
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.type == ModContent.ProjectileType<KingCrimsonHeavyAttack_Tier_4>() && proj.owner == Projectile.owner)
                    return false;
            }

            Player p = Main.player[Projectile.owner];
            var data = ParticulasStands.Stands.KingCrimsonParticulas3;
            bool spawning = runtime.spawning;

            bool blackSkin = UI.StandSlotSystem.HasBlackCrimsonSkinFor(p);
            string pathBlack = "Jojo/Content/Projectiles/Skins/KingCrimson/BlackCrimson/";

            Texture2D tex; Rectangle r; Vector2 o;

            if (spawning)
            {
                string spawnTex = blackSkin ? pathBlack + "KingCrimson_Spawn_BLACK" : data.SpawnTexture;
                tex = ModContent.Request<Texture2D>(spawnTex).Value;
                r = new Rectangle(runtime.frame * data.FrameWidth, 0, data.FrameWidth, data.SpawnHeight);
                o = new Vector2(data.FrameWidth / 2f, data.SpawnHeight / 2f);
                lightColor = Color.White;
            }
            else
            {
                string idleTex = blackSkin ? pathBlack + "KINGCRIMSONSTAND_BLACK" : data.IdleTexture;
                tex = ModContent.Request<Texture2D>(idleTex).Value;
                r = new Rectangle(frame * 88, 0, 88, 76);
                o = new Vector2(44, 49);
            }

            bool flipVisual = syncOffX < 0;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, r,
                lightColor * ((255 - Projectile.alpha) / 255f), Projectile.rotation, o,
                Projectile.scale, flipVisual ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
            return false;
        }
    }

    public class KingCrimsonHeavyAttack_Tier_4 : ModProjectile
    {
        public override string Texture => "Jojo/Content/Projectiles/KingCrimson/KingCrimson_Tier_4/GolpeFuerte_Tier_4";

        static readonly SoundStyle DonutSound = new("Jojo/Content/Sonidos/Donut");
        private float frozenRotation = 0f;
        private bool rotationFrozen = false;
        private bool soundPlayed = false;
        private int trailTimer = 0;
        private const int TrailInterval = 3;

        // Variables dedicadas de Lerp que hemos implementado para solucionar el fallo de retorno
        private Vector2 returnStartPos;
        private Vector2 returnTargetPos;
        private float returnStartRotation;
        private int returnTimer = 0;
        private const int ReturnDurationFrames = 10;

        public override void SetDefaults()
        {
            Projectile.width = 88;
            Projectile.height = 74;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.timeLeft = 300;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        void SpawnTrailShadow()
        {
            Player p = Main.player[Projectile.owner];
            bool flip = (Projectile.Center.X - p.Center.X) < 0;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<KingCrimsonHeavyTrail_Tier_4>(),
                0, 0f, Projectile.owner,
                Projectile.frame, flip ? 1f : 0f, Projectile.rotation);
        }

        // Método que inicia el retorno, fijando las posiciones base para que Terraria no recalcule mal
        void BeginReturn(Player p)
        {
            returnStartPos = Projectile.Center;
            returnTargetPos = p.Center + new Vector2(-40 * p.direction, -10);
            returnStartRotation = Projectile.rotation;
            returnTimer = 0;
            rotationFrozen = false;
            Projectile.frame = 0;
            Projectile.friendly = false;
            Projectile.localAI[0] = 2f;
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            if (!p.active || p.dead) { Projectile.Kill(); return; }

            if (Projectile.localAI[0] == 0f)
            {
                if (!soundPlayed) { SoundEngine.PlaySound(DonutSound, Projectile.Center); soundPlayed = true; }
                Projectile.frame = 0;
                Vector2 targetPos = new Vector2(Projectile.ai[0], Projectile.ai[1]);
                Vector2 dif = targetPos - Projectile.Center;
                Vector2 dirToTarget = targetPos - p.Center;
                if (dirToTarget.Length() > 1f)
                {
                    float rot = dirToTarget.ToRotation();
                    if (dirToTarget.X < 0) rot += MathHelper.Pi;
                    Projectile.rotation = rot;
                }
                Projectile.Center = Vector2.Lerp(Projectile.Center, targetPos, 0.35f);
                if (++trailTimer >= TrailInterval) { trailTimer = 0; SpawnTrailShadow(); }
                if (dif.Length() <= 15f) { Projectile.Center = targetPos; Projectile.velocity = Vector2.Zero; Projectile.localAI[0] = 1f; }
            }
            else if (Projectile.localAI[0] == 1f)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.localAI[1]++;
                int t = (int)Projectile.localAI[1];
                if (t < 4) Projectile.frame = 0;
                else if (t < 8) Projectile.frame = 1;
                else if (t < 12) Projectile.frame = 2;
                else Projectile.frame = 3;
                if (Projectile.frame == 3 && !rotationFrozen) { frozenRotation = Projectile.rotation; rotationFrozen = true; }
                if (rotationFrozen) Projectile.rotation = frozenRotation;
                Projectile.friendly = (Projectile.frame == 2);
                if (Projectile.frame == 2 && t == 8) SoundEngine.PlaySound(Terraria.ID.SoundID.Item71, Projectile.Center);

                // Aquí activamos nuestro método
                if (t > 18)
                {
                    BeginReturn(p);
                }
            }
            else if (Projectile.localAI[0] == 2f)
            {
                // El bloque de retorno estable
                float progress = ReturnDurationFrames <= 0 ? 1f : Math.Clamp((float)returnTimer / ReturnDurationFrames, 0f, 1f);
                Projectile.Center = Vector2.Lerp(returnStartPos, returnTargetPos, progress);
                Projectile.rotation = returnStartRotation;
                Projectile.frame = 0;
                Projectile.friendly = false;
                returnTimer++;

                if (progress >= 1f)
                {
                    Projectile.Kill();
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player p = Main.player[Projectile.owner];

            bool blackSkin = UI.StandSlotSystem.HasBlackCrimsonSkinFor(p);
            string texturePath = blackSkin
                ? "Jojo/Content/Projectiles/Skins/KingCrimson/BlackCrimson/GolpeFuerte_BLACK"
                : "Jojo/Content/Projectiles/KingCrimson/KingCrimson_Tier_4/GolpeFuerte_Tier_4";

            Texture2D tex = ModContent.Request<Texture2D>(texturePath).Value;
            Rectangle r = new Rectangle(Projectile.frame * 88, 0, 88, 74);
            Vector2 o = new Vector2(44, 37);
            bool flipVisual = (Projectile.Center.X - p.Center.X) < 0;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, r,
                lightColor * ((255 - Projectile.alpha) / 255f), Projectile.rotation, o,
                Projectile.scale, flipVisual ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
            return false;
        }
    }
}