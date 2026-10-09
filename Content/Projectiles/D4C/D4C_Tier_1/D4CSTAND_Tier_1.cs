using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using Jojo.Systems.StandFuncionComunes;
using System;
using System.IO;
using System.Collections.Generic;
using Jojo.Content.Buffs;
using Jojo.Content.Habilidades;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Items;
using Jojo.Content.Items.Potenciadores;

namespace Jojo.Content.Projectiles.D4C.D4C_Tier_1
{
    public class D4CSTAND_Tier_1 : ModProjectile
    {
        enum State { Idle, Attack, FlagAnim }
        State state;

        bool auto, init, dying, fDash;
        NPC target;

        int frame, animT, cd, dashT;
        int attackTimer, swingSoundTimer;

        int flagAnimFrame = 0;
        int flagAnimTimer = 0;
        bool spawningFlag = false;

        int clonesRestantesPorSpawnear = 0;

        float manR = 130f, autoR = 130f;
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
            writer.Write(spawningFlag);
            writer.Write(flagAnimFrame);
            writer.Write(flagAnimTimer);
            writer.Write(clonesRestantesPorSpawnear);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            state = (State)reader.ReadByte();
            auto = reader.ReadBoolean();
            fDash = reader.ReadBoolean();
            syncOffX = reader.ReadSingle();
            syncOffY = reader.ReadSingle();
            spawningFlag = reader.ReadBoolean();
            flagAnimFrame = reader.ReadInt32();
            flagAnimTimer = reader.ReadInt32();
            clonesRestantesPorSpawnear = reader.ReadInt32();
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

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }

        public void StartDying()
        {
            if (dying) return;
            dying = true;
            Projectile.friendly = false;
            Projectile.timeLeft = 25;
            SoundEngine.PlaySound(DespawnSound, Projectile.Center);
        }

        // ── LIMPIEZA TOTAL AL DESAPARECER EL STAND ──
        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                LimpiarMarcasDelJugador(Main.player[Projectile.owner]);
                EliminarTodosLosAliados(Main.player[Projectile.owner]);
            }
        }

        static void LimpiarMarcasDelJugador(Player owner)
        {
            if (D4CGlobalNPC_Tier_1.ObjetivoMarcadoPorJugador.ContainsKey(owner.whoAmI))
            {
                D4CGlobalNPC_Tier_1.ObjetivoMarcadoPorJugador[owner.whoAmI] = -1;
            }

            foreach (NPC npc in Main.npc)
            {
                if (!npc.active) continue;
                if (npc.TryGetGlobalNPC<D4CGlobalNPC_Tier_1>(out var gnpc))
                {
                    if (gnpc.OwnerIndex == owner.whoAmI && gnpc.d4cMarkTimer > 0)
                    {
                        gnpc.d4cMarkTimer = 0;
                        gnpc.OwnerIndex = -1;
                        npc.netUpdate = true;
                    }
                }
            }
        }

        static void EliminarTodosLosAliados(Player owner)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(
                    owner.GetSource_FromThis("D4C_LimpiarTodoAlDespawnear"),
                    owner.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<D4CRedBridgeProjectile_Tier_1>(),
                    0, 0f, owner.whoAmI, 3f
                );
            }
            else
            {
                AliadoMeleeNPC_Tier_1.EliminarAliadosDelJugadorServidor(owner);
                AliadoRangedNPC_Tier_1.EliminarAliadosDelJugadorServidor(owner);
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

            if (dying) { ParticulasStands.Despawn(Projectile, p); return; }
            if (!p.active || p.dead) { StartDying(); return; }

            if (!init)
            {
                init = true;
                Projectile.alpha = 0;
                Projectile.friendly = false;
                Projectile.damage = 0;
                SoundEngine.PlaySound(SpawnSound, Projectile.Center);
                ParticulasStands.Spawn(p, ParticulasStands.Stands.PARTICULASD4C1);
            }

            var data = ParticulasStands.Stands.PARTICULASD4C1;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);

            bool spawning = runtime.spawning;
            bool isOwner = Projectile.owner == Main.myPlayer;

            if (!spawning && isOwner)
            {
                HandleSkills(p);
                HandleToggle(p);
                HandleRightClick(p);
            }

            auto = Projectile.ai[0] == 1f;

            if (!spawning)
            {
                UpdateSkills(p);
            }

            if (isOwner)
            {
                Vector2 off = GetOffset(p);

                float multRango = 1f;
                if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
                {
                    multRango = rangoPlayer.multiplicadorRango;
                }

                float max = (auto ? autoR : manR) * multRango;

                if (off.Length() > max && !spawningFlag)
                    off = Vector2.Normalize(off) * max;

                syncOffX = off.X;
                syncOffY = off.Y;

                if (spawningFlag)
                {
                    state = State.FlagAnim;
                    Projectile.friendly = false;
                }
                else
                {
                    bool canAttack = ParticulasStands.CanAttack(runtime) && (auto ? (target = FindEnemy(p)) != null : Main.mouseLeft);
                    state = canAttack ? State.Attack : State.Idle;
                    Projectile.friendly = canAttack;
                }

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

                if (state == State.Attack && !spawningFlag)
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

                if (state == State.Attack && !spawningFlag)
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

            float baseDamage = 15f;
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);

            if (!spawning)
            {
                Animate();
            }
        }

        Vector2 GetOffset(Player p)
        {
            if (spawningFlag)
            {
                return new Vector2(40f * p.direction, -10f);
            }

            if (fDash) return (Main.MouseWorld - p.Center) * 0.35f;

            if (state == State.Attack)
                return auto && target != null
                    ? target.Center - p.Center
                    : Main.MouseWorld - p.Center;

            return new Vector2(-40f * p.direction, -10f);
        }

        void UpdateSkills(Player p)
        {
            if (fDash && ++dashT > 6)
            {
                fDash = false;
                dashT = 0;
                if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
            }

            if (spawningFlag)
            {
                flagAnimTimer++;
                if (flagAnimTimer >= 9)
                {
                    flagAnimTimer = 0;
                    flagAnimFrame++;

                    if (flagAnimFrame == 4 && clonesRestantesPorSpawnear > 0)
                    {
                        if (Projectile.owner == Main.myPlayer)
                        {
                            D4CAliadosBridgeProjectile_Tier_1.InvocarGrupoCompleto(p, Projectile.Center, 1, 1);
                        }
                        clonesRestantesPorSpawnear--;
                    }

                    if (flagAnimFrame >= 6)
                    {
                        flagAnimFrame = 0;
                        spawningFlag = false;
                        if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
                    }
                }
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

        void HandleRightClick(Player p)
        {
            if (p.whoAmI != Main.myPlayer) return;

            if (Main.mouseRight)
            {
                Vector2 mousePos = Main.MouseWorld;

                foreach (NPC n in Main.npc)
                {
                    if (!n.active || n.friendly || n.life <= 0 || n.dontTakeDamage) continue;

                    if (n.Hitbox.Contains(mousePos.ToPoint()))
                    {
                        D4CGlobalNPC_Tier_1.ObjetivoMarcadoPorJugador.TryGetValue(p.whoAmI, out int marcadoActual);

                        if (marcadoActual != n.whoAmI)
                        {
                            // 🔊 Reproducir Sonido Vanilla al Marcar Nuevo Objetivo (SoundID.MaxMana / "Ding" o SoundID.Item8 para magia)
                            SoundEngine.PlaySound(SoundID.MaxMana, n.Center);

                            // Limpiamos las marcas de cualquier otro NPC antes de poner la nueva
                            foreach (NPC otro in Main.npc)
                            {
                                if (otro.active)
                                    otro.GetGlobalNPC<D4CGlobalNPC_Tier_1>().d4cMarkTimer = 0;
                            }

                            D4CGlobalNPC_Tier_1 gnpc = n.GetGlobalNPC<D4CGlobalNPC_Tier_1>();
                            // Le asignamos 1 porque ahora el timer no baja. Si es > 0, es permanente.
                            gnpc.d4cMarkTimer = 1;
                            gnpc.OwnerIndex = p.whoAmI;

                            D4CGlobalNPC_Tier_1.ObjetivoMarcadoPorJugador[p.whoAmI] = n.whoAmI;

                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                Vector2.Zero,
                                ModContent.ProjectileType<D4CMarcaBridgeProjectile_Tier_1>(),
                                0, 0f, p.whoAmI, n.whoAmI
                            );
                        }
                        else
                        {
                            // Si ya está marcado y seguimos manteniendo el clic encima, solo aseguramos que el valor no sea 0
                            n.GetGlobalNPC<D4CGlobalNPC_Tier_1>().d4cMarkTimer = 1;
                        }

                        break; // Solo marcamos un enemigo a la vez por tick
                    }
                }
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
                float factorTiempoG = Math.Max(0f, 1f - stats.standCooldown1Reduction);
                p.AddBuff(ModContent.BuffType<Cooldown1>(), (int)(1200 * factorTiempoG));
                p.AddBuff(ModContent.BuffType<CD>(), 180);

                fDash = false;
                spawningFlag = true;
                flagAnimFrame = 0;
                flagAnimTimer = 0;
                clonesRestantesPorSpawnear = 1;

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
            if (spawningFlag) return;

            if (++animT < (state == State.Attack ? 5 : 9)) return;
            animT = 0;

            frame = state == State.Idle
                ? (frame + 1) % 4
                : (frame < 4 || frame > 7 ? 4 : (frame + 1 > 7 ? 4 : frame + 1));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player p = Main.player[Projectile.owner];
            var data = ParticulasStands.Stands.PARTICULASD4C1;
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
            else if (spawningFlag)
            {
                tex = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/D4C/D4C_Tier_1/Bandera1_Tier_1").Value;
                r = new Rectangle(flagAnimFrame * 88, 0, 88, 88);
                o = new Vector2(44, 49);
            }
            else
            {
                tex = ModContent.Request<Texture2D>(data.IdleTexture).Value;
                r = new Rectangle(frame * 88, 0, 88, 88);
                o = new Vector2(44, 49);
            }

            bool flipVisual = syncOffX < 0;

            if (spawningFlag)
            {
                flipVisual = p.direction == -1;
            }

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