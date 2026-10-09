using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using System;
using System.IO;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.WhiteSnake_Buffs;
using Jojo.Content.Habilidades;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Items;
using Jojo.Content.Items.Potenciadores;
using Jojo.Systems.StandFuncionComunes;

namespace Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_1
{
    public class WHITESNAKESTAND_Tier_1 : ModProjectile
    {
        enum State { Idle, Attack, Extracting }
        State state;

        bool auto, init, dying, grabAnimStarted;
        NPC target;
        int extractTargetIndex = -1;

        int frame, animT, cd;
        int attackTimer, swingSoundTimer;

        byte extractFrame;

        float manR = 230f, autoR = 230f;
        float innerRadius = 90f;

        Vector2 lastCenter = Vector2.Zero;  // Para sincronizar partículas en movimiento sin dejar estela

        ParticulasStands.StandRuntime runtime = new();

        public int baseCritChance = 5;
        public int attackSpeed = 1;

        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");
        static readonly SoundStyle SwingSoundBase = new("Terraria/Sounds/Item_1");

        float syncOffX, syncOffY;
        int netTimer = 0;

        const float VelocidadAcercamientoCliente = 20f;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)state);
            writer.Write(auto);
            writer.Write(syncOffX);
            writer.Write(syncOffY);

            writer.Write(extractFrame);

            writer.Write(grabAnimStarted);
            writer.Write((short)extractTargetIndex);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            state = (State)reader.ReadByte();
            auto = reader.ReadBoolean();
            syncOffX = reader.ReadSingle();
            syncOffY = reader.ReadSingle();

            extractFrame = reader.ReadByte();

            grabAnimStarted = reader.ReadBoolean();
            extractTargetIndex = reader.ReadInt16();
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

        int GetAttackDelay() => attackSpeed switch { 1 => 5, 2 => 4, 3 => 3, 4 => 2, _ => 5 };
        SoundStyle GetSwingSound(float speed) => SwingSoundBase with { Pitch = speed >= 100f ? 0.05f : speed >= 50f ? 0.08f : 0f };
        int GetSwingDelay(float speed) => speed >= 100f ? 6 : speed >= 50f ? 8 : 11;

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            UpdateAttackSpeed(p);

            if (dying) { ParticulasStands.Despawn(Projectile, p); return; }
            if (!p.active || p.dead) { StartDying(); return; }

            Projectile.timeLeft = 2;

            if (!init)
            {
                init = true;
                Projectile.alpha = 0;
                Projectile.friendly = false;
                Projectile.damage = 0;
                SoundEngine.PlaySound(SpawnSound, Projectile.Center);
                ParticulasStands.Spawn(p, ParticulasStands.Stands.WhiteSnakeParticulas1);
            }

            var data = ParticulasStands.Stands.WhiteSnakeParticulas1;
            runtime.Update(data);
            ParticulasStands.ApplySpawnLock(Projectile, runtime);

            bool spawning = runtime.spawning;
            bool isOwner = Projectile.owner == Main.myPlayer;

            // --- LUZ Y PARTÍCULAS ---
            if (!spawning)
            {
                // Emisión de luz azulada/oscura (#5a59a4) en lugar de blanca
                Lighting.AddLight(Projectile.Center, 90f / 255f, 89f / 255f, 164f / 255f);

                // Mover partículas activas junto al Stand para evitar estelas
                if (lastCenter != Vector2.Zero)
                {
                    Vector2 delta = Projectile.Center - lastCenter;
                    if (delta != Vector2.Zero)
                    {
                        for (int i = 0; i < Main.maxDust; i++)
                        {
                            Dust d = Main.dust[i];
                            if (d.active && d.type == 267 && d.customData is int ownerId && ownerId == Projectile.whoAmI)
                            {
                                d.position += delta;
                            }
                        }
                    }
                }

                // Frecuencia de partículas reducida (1 cada 8 frames)
                if (Main.rand.NextBool(8))
                {
                    // Paleta ajustada exclusivamente a tonos azulados oscuros
                    Color[] particleColors = new Color[]
                    {
                        new Color(57, 76, 131), // #394c83
                        new Color(90, 89, 164), // #5a59a4
                        new Color(45, 55, 115)  // Azul profundo complementario
                    };

                    Color chosenColor = Main.rand.Next(particleColors);

                    // Área de generación ligeramente más reducida para concentrarlas más cerca del Stand
                    Vector2 spawnCenter = Projectile.Center + new Vector2(Main.rand.NextFloat(-18f, 18f), Main.rand.NextFloat(-24f, 20f));

                    int dustId = Dust.NewDust(spawnCenter, 0, 0, 267, 0f, 0f, 100, chosenColor, Main.rand.NextFloat(0.7f, 1.0f));
                    Main.dust[dustId].noGravity = true;
                    Main.dust[dustId].velocity = new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-0.8f, -0.2f));
                    Main.dust[dustId].fadeIn = 0.2f;
                    Main.dust[dustId].customData = Projectile.whoAmI;
                }
            }

            lastCenter = Projectile.Center;

            if (!spawning && isOwner)
            {
                HandleToggle(p);
            }

            auto = Projectile.ai[0] == 1f;

            if (isOwner)
            {
                float multRango = 1f;
                if (p.TryGetModPlayer(out RangoStandPlayer rangoPlayer))
                {
                    multRango = rangoPlayer.multiplicadorRango;
                }

                float baseMax = (auto ? autoR : manR) * multRango;
                float extractMax = baseMax * 1.5f;

                if (state == State.Extracting)
                {
                    NPC extTarget = extractTargetIndex >= 0 ? Main.npc[extractTargetIndex] : null;

                    var estadoExtr = ExtraccionLogica.Actualizar(Projectile, p, ref extTarget, extractMax, grabAnimStarted);

                    if (estadoExtr == ExtraccionLogica.EstadoExtraccion.Cancelado)
                    {
                        state = State.Idle;
                        extractTargetIndex = -1;
                        grabAnimStarted = false;
                        extractFrame = 0;
                        Projectile.netUpdate = true;
                    }
                    else if (estadoExtr == ExtraccionLogica.EstadoExtraccion.Extrayendo)
                    {
                        extractTargetIndex = extTarget.whoAmI;

                        if (!grabAnimStarted)
                        {
                            grabAnimStarted = true;
                            extractFrame = 0;
                            animT = 0;
                            Projectile.netUpdate = true;
                        }

                        animT++;
                        if (animT >= 12)
                        {
                            animT = 0;
                            extractFrame++;
                            if (extractFrame > 5)
                            {
                                ExtraccionLogica.AplicarAmnesia(extTarget);
                                state = State.Idle;
                                extractTargetIndex = -1;
                                grabAnimStarted = false;
                                extractFrame = 0;
                                Projectile.netUpdate = true;
                            }
                        }
                    }
                    else if (estadoExtr == ExtraccionLogica.EstadoExtraccion.Acercandose)
                    {
                        if (extractTargetIndex != extTarget.whoAmI)
                            Projectile.netUpdate = true;

                        extractTargetIndex = extTarget.whoAmI;
                        grabAnimStarted = false;

                        if (extractFrame > 3) extractFrame = 0;

                        if (++animT >= 9)
                        {
                            animT = 0;
                            extractFrame = (byte)((extractFrame + 1) % 4);
                        }
                    }
                    else // BUSCANDO
                    {
                        extractTargetIndex = -1;
                        grabAnimStarted = false;

                        if (extractFrame > 3) extractFrame = 0;

                        if (++animT >= 9)
                        {
                            animT = 0;
                            extractFrame = (byte)((extractFrame + 1) % 4);
                        }
                    }

                    if (state == State.Extracting)
                    {
                        if (extractTargetIndex == -1)
                        {
                            Vector2 targetOff = Main.MouseWorld - p.Center;
                            if (targetOff.Length() > extractMax)
                                targetOff = Vector2.Normalize(targetOff) * extractMax;

                            syncOffX = targetOff.X;
                            syncOffY = targetOff.Y;

                            ParticulasStands.FollowPlayer(Projectile, p, new Vector2(syncOffX, syncOffY), 0.12f);
                        }
                        else if (extTarget != null)
                        {
                            syncOffX = extTarget.Center.X - p.Center.X;
                            syncOffY = extTarget.Center.Y - p.Center.Y;
                        }

                        Projectile.friendly = false;
                        Projectile.rotation = 0f;
                    }
                }

                if (state != State.Extracting)
                {
                    if (Main.mouseRight && !Main.mouseLeft && ParticulasStands.CanAttack(runtime))
                    {
                        state = State.Extracting;
                        extractFrame = 0;
                        animT = 0;
                        extractTargetIndex = -1;
                        Projectile.netUpdate = true;
                    }
                    else
                    {
                        bool canAttack = ParticulasStands.CanAttack(runtime) && (auto ? (target = FindEnemy(p)) != null : Main.mouseLeft);

                        state = canAttack ? State.Attack : State.Idle;
                        Projectile.friendly = canAttack;

                        Vector2 off = GetOffset(p);
                        if (off.Length() > baseMax) off = Vector2.Normalize(off) * baseMax;

                        syncOffX = off.X;
                        syncOffY = off.Y;

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
                        else
                        {
                            Projectile.rotation = 0f;
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
                            if (attackTimer >= GetAttackDelay())
                            {
                                attackTimer = 0;
                                Projectile.friendly = true;
                            }
                        }
                        else
                        {
                            swingSoundTimer = 0;
                        }

                        ParticulasStands.FollowPlayer(Projectile, p, off, 0.25f);
                    }
                }

                if (++netTimer >= 5)
                {
                    netTimer = 0;
                    Projectile.netUpdate = true;
                }
            }
            else // MULTIPLAYER CLIENTE
            {
                Vector2 off = new Vector2(syncOffX, syncOffY);

                if (state != State.Extracting)
                {
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
                else
                {
                    Projectile.rotation = 0f;

                    NPC clientTarget = (extractTargetIndex >= 0 && extractTargetIndex < Main.maxNPCs)
                        ? Main.npc[extractTargetIndex]
                        : null;

                    if (clientTarget != null && clientTarget.active)
                    {
                        if (grabAnimStarted)
                        {
                            Projectile.Center = clientTarget.Center;
                        }
                        else
                        {
                            Vector2 dir = clientTarget.Center - Projectile.Center;
                            float dist = dir.Length();
                            if (dist > 0.01f)
                            {
                                float avance = MathHelper.Min(VelocidadAcercamientoCliente, dist);
                                Projectile.Center += dir / dist * avance;
                            }
                        }
                    }
                    else
                    {
                        ParticulasStands.FollowPlayer(Projectile, p, off, 0.15f);
                    }

                    Projectile.rotation = 0f;
                }
            }

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);
            float baseDamage = 5f;
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);

            if (!spawning && state != State.Extracting)
            {
                Animate();
            }
        }

        Vector2 GetOffset(Player p)
        {
            if (state == State.Attack)
                return auto && target != null ? (target.Center - p.Center) + new Vector2(0, 25f) : Main.MouseWorld - p.Center;

            return new Vector2(-40 * p.direction, -10);
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
                    Main.NewText(auto ? "AutoStand-ON" : "AutoStand-OFF", 255, 255, 0);
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
            var data = ParticulasStands.Stands.WhiteSnakeParticulas1;
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
            else if (state == State.Extracting && grabAnimStarted)
            {
                tex = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_1/Extraccion_Tier_1").Value;
                r = new Rectangle(extractFrame * 88, 0, 88, 76);
                o = new Vector2(44, 49);
            }
            else if (state == State.Extracting && !grabAnimStarted)
            {
                tex = ModContent.Request<Texture2D>(data.IdleTexture).Value;
                r = new Rectangle(extractFrame * 88, 0, 88, 76);
                o = new Vector2(44, 49);
            }
            else
            {
                tex = ModContent.Request<Texture2D>(data.IdleTexture).Value;
                r = new Rectangle(frame * 88, 0, 88, 76);
                o = new Vector2(44, 49);
            }

            bool enMitadDerecha = syncOffX >= 0;
            bool flipVisual = state == State.Extracting
                ? enMitadDerecha
                : !enMitadDerecha;

            SpriteEffects sEffects = flipVisual ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // Sprite principal del Stand
            Main.EntitySpriteDraw(
                tex,
                Projectile.Center - Main.screenPosition,
                r,
                lightColor * ((255 - Projectile.alpha) / 255f),
                Projectile.rotation,
                o,
                Projectile.scale,
                sEffects,
                0f
            );

            return false;
        }
    }

    public class AmnesiaVisualsNPC_Tier_1 : GlobalNPC
    {
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (npc.HasBuff(ModContent.BuffType<AmnesiaDebuff>()))
            {
                Texture2D tex = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_1/NoDiscoDebuff").Value;

                Vector2 drawPos = npc.Center - screenPos;
                Vector2 origin = tex.Size() / 2f;

                spriteBatch.Draw(
                    tex,
                    drawPos,
                    null,
                    Color.White,
                    0f,
                    origin,
                    1f,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }
}