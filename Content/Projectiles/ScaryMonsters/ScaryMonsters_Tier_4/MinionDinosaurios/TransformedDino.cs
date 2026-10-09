using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System;
using System.IO;
using Jojo.Systems;
using Jojo.Content.Buffs.ScaryMonsters_Buffs;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4.Marca;

namespace Jojo.Content.NPCs.ScaryMonsters.MinionDinosaurios
{
    public class TransformedDinoNPC : ModNPC
    {
        public override string Texture => "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_4/MinionDinosaurios/MiniDino";

        // ===================== CONFIGURACIÓN DE FRAMES =====================
        const int TotalFrames = 13;
        const int FrameIdle = 0;
        const int FrameCayendo = 1;
        const int FrameDespegue = 3;
        const int FrameCorrerInicio = 4;
        const int FrameCorrerFin = 8;
        const int FrameVueloInicio = 9;
        const int FrameVueloFin = 12;
        static readonly bool UsarSpritesDeVuelo = true;
        const float OffsetDibujoY = 2f;
        // ===================================================================

        bool volando;
        int jumpCooldown;
        int stuckTimer;
        int progressTimer;
        float lastDist;
        int netTimer;
        bool bajarPlataforma;

        int currentFrame;
        int frameCounter;

        public string ownerName = "";

        // Tier del stand que lo creó (guardado en ai[1], que Terraria sincroniza)
        public int Tier => (int)NPC.ai[1];

        // Estadísticas según el tier que lo infectó
        ScaryProfile Perfil => ScaryTiers.GetProfile(Tier);

        public Player Dueño
        {
            get
            {
                if (!string.IsNullOrEmpty(ownerName))
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];
                        if (p.active && p.name == ownerName)
                        {
                            NPC.ai[0] = i;
                            return p;
                        }
                    }
                }

                int index = (int)NPC.ai[0];
                if (index >= 0 && index < Main.maxPlayers)
                {
                    Player p = Main.player[index];
                    if (p.active)
                    {
                        if (string.IsNullOrEmpty(ownerName))
                            ownerName = p.name;
                        return p;
                    }
                }

                return null;
            }
        }

        public override bool NeedSaving() => true;

        public override void SaveData(TagCompound tag)
        {
            tag["ownerName"] = ownerName;
            tag["tier"] = Tier;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("ownerName"))
                ownerName = tag.GetString("ownerName");

            if (tag.ContainsKey("tier"))
                NPC.ai[1] = tag.GetInt("tier");
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = TotalFrames;
        }

        public override void SetDefaults()
        {
            NPC.width = 26;
            NPC.height = 22;
            NPC.friendly = true;
            NPC.damage = 20;
            NPC.lifeMax = 100;
            NPC.defense = 10;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;
            NPC.aiStyle = -1;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
        }

        // ===================== BLOQUEAR REGENERACIÓN DE VIDA =====================
        public override void UpdateLifeRegen(ref int damage)
        {
            // Cancela la regeneración de vida positiva para que mueran por combate acumulado
            if (NPC.lifeRegen > 0)
            {
                NPC.lifeRegen = 0;
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(volando);
            writer.Write(ownerName ?? "");
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            volando = reader.ReadBoolean();
            ownerName = reader.ReadString();
        }

        // ===================== IA PRINCIPAL =====================
        public override void AI()
        {
            Player owner = Dueño;

            // Timer de gracia para evitar despawns erróneos al cargar el mundo
            NPC.localAI[1]++;

            if (NPC.localAI[1] > 5)
            {
                // CONDICIÓN DE MUERTE: dueño inexistente/muerto o SIN NINGÚN stand de Scary Monsters
                if (owner == null || !owner.active || owner.dead || !ScaryTiers.StandActivo(owner))
                {
                    NPC.life = 0;
                    NPC.HitEffect();
                    NPC.checkDead();
                    NPC.active = false;
                    return;
                }
            }

            if (owner == null)
                return;

            ScaryProfile perfil = Perfil;

            // El daño depende del tier (también en clientes, donde damage no se sincroniza)
            NPC.damage = perfil.TransformedDinoDano;

            // ---------------- objetivo: la MARCA tiene prioridad sobre todo ----------------
            NPC marca = ScaryMarca.GetObjetivo(owner.whoAmI);
            NPC objetivo = marca ?? (volando ? null : FindClosestNPC(600f));

            AtacarEnemigosCercanos();

            int indice = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC o = Main.npc[i];
                if (o.active && o.type == NPC.type && o.ai[0] == NPC.ai[0] && o.whoAmI < NPC.whoAmI)
                    indice++;
            }
            int lado = (indice % 2 == 0) ? -1 : 1;
            float offsetX = lado * (45f + 38f * (indice / 2));
            float metaSeguirX = owner.Center.X + offsetX;

            bool enSuelo = !volando && NPC.velocity.Y == 0f;
            float distDueno = Vector2.Distance(NPC.Center, owner.Center);

            if (!volando)
            {
                Vector2 meta = objetivo != null ? objetivo.Center : new Vector2(metaSeguirX, owner.Center.Y);

                if (++progressTimer >= 30)
                {
                    progressTimer = 0;
                    float d = Vector2.Distance(NPC.Center, meta);
                    if (d > lastDist - 16f && d > 140f && distDueno > 260f)
                        stuckTimer += 30;
                    else
                        stuckTimer = 0;
                    lastDist = d;
                }
            }

            float distDestino = Vector2.Distance(NPC.Center, marca != null ? marca.Center : owner.Center);

            if (!volando)
            {
                if ((marca == null && distDueno > perfil.MiniDinoDistanciaVuelo) || stuckTimer >= 120)
                {
                    volando = true;
                    stuckTimer = 0;
                    NPC.netUpdate = true;
                }
            }
            else
            {
                if (distDestino < 140f && !Collision.SolidCollision(NPC.position, NPC.width, NPC.height))
                {
                    volando = false;
                    stuckTimer = 0;
                    progressTimer = 0;
                    NPC.netUpdate = true;
                }
            }

            NPC.noTileCollide = volando || bajarPlataforma;
            NPC.noGravity = volando;

            if (volando)
                Volar(owner, offsetX, marca);
            else
                Caminar(owner, objetivo, metaSeguirX, enSuelo, perfil);

            if (Main.netMode != NetmodeID.MultiplayerClient && ++netTimer >= 15)
            {
                netTimer = 0;
                NPC.netUpdate = true;
            }
        }

        // ===================== MOVIMIENTO Y COMBATE =====================
        void Caminar(Player owner, NPC objetivo, float metaSeguirX, bool enSuelo, ScaryProfile perfil)
        {
            float vMax = perfil.TransformedDinoVelocidad;
            float destinoX = objetivo != null ? objetivo.Center.X : metaSeguirX;
            float dx = destinoX - NPC.Center.X;
            float deseada = 0f;

            if (objetivo != null)
            {
                if (Math.Abs(dx) > 8f)
                    deseada = Math.Sign(dx) * vMax * 1.15f;
            }
            else if (Math.Abs(dx) > 24f)
            {
                float factor = Math.Abs(dx) > 320f ? 1.5f : 1f;
                deseada = Math.Sign(dx) * MathHelper.Clamp(Math.Abs(dx) * 0.07f, 1.8f, vMax * factor);
            }

            // Ya no se añade la repulsión de Separacion() para permitir el stackeo fluido
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, deseada, enSuelo ? 0.18f : 0.08f);
            if (Math.Abs(deseada) < 0.01f && Math.Abs(NPC.velocity.X) < 0.1f)
                NPC.velocity.X = 0f;

            if (objetivo != null && Math.Abs(dx) > 4f)
                NPC.direction = NPC.spriteDirection = dx > 0f ? 1 : -1;
            else if (Math.Abs(NPC.velocity.X) > 0.3f)
                NPC.direction = NPC.spriteDirection = NPC.velocity.X > 0f ? 1 : -1;

            if (jumpCooldown > 0) jumpCooldown--;

            Vector2 metaPos = objetivo != null ? objetivo.Center : owner.Center;

            if (enSuelo && jumpCooldown <= 0)
            {
                bool bloqueado = Math.Abs(deseada) > 0.5f && ObstaculoAdelante(Math.Sign(deseada));
                bool metaArriba = metaPos.Y < NPC.Center.Y - 56f && Math.Abs(dx) < 160f;

                if (bloqueado || metaArriba)
                {
                    float fuerza = perfil.TransformedDinoSalto * ((metaArriba && !bloqueado) ? 1.15f : 1f);
                    NPC.velocity.Y = -fuerza;
                    jumpCooldown = 25;
                }
            }

            bajarPlataforma = metaPos.Y > NPC.Bottom.Y + 40f && Math.Abs(dx) < 80f;
        }

        void AtacarEnemigosCercanos()
        {
            if (NPC.localAI[0] > 0) NPC.localAI[0]--;

            if (NPC.localAI[0] == 0)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC enemigo = Main.npc[i];
                    if (enemigo.whoAmI == NPC.whoAmI) continue;

                    // El NPC marcado se puede golpear aunque sea amigable / pacífico
                    bool esMarca = enemigo.active && ScaryMarca.EsMarcado((int)NPC.ai[0], enemigo);

                    if (enemigo.active && (esMarca || !enemigo.friendly) && !enemigo.dontTakeDamage && enemigo.life > 0)
                    {
                        if (NPC.Hitbox.Intersects(enemigo.Hitbox))
                        {
                            NPC.HitInfo hit = new NPC.HitInfo
                            {
                                Damage = NPC.damage,
                                Knockback = 3f,
                                HitDirection = Math.Sign(enemigo.Center.X - NPC.Center.X)
                            };
                            enemigo.StrikeNPC(hit);

                            if (!enemigo.boss)
                            {
                                // Los dinos transformados infectan con SU tier, no con el stand activo
                                DinoVirusGlobalNPC.Infectar(enemigo, (int)NPC.ai[0], Tier);
                            }

                            NPC.localAI[0] = 20;
                            break;
                        }
                    }
                }
            }
        }

        bool ObstaculoAdelante(int dir)
        {
            Vector2 punto = NPC.Center + new Vector2(dir * (NPC.width / 2f + 8f), NPC.height / 2f - 8f);
            Point tp = punto.ToTileCoordinates();
            Tile t = Framing.GetTileSafely(tp.X, tp.Y);
            return t.HasUnactuatedTile && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType];
        }

        // Devuelve 0 para desactivar la fuerza de empuje entre minions
        float Separacion() => 0f;

        void Volar(Player owner, float offsetX, NPC marca)
        {
            bajarPlataforma = true;

            Vector2 destino = marca != null
                ? marca.Center
                : owner.Center + new Vector2(offsetX, -24f);
            Vector2 dir = destino - NPC.Center;
            float dist = dir.Length();

            if (marca == null && dist > 2400f)
            {
                NPC.Center = destino;
                NPC.velocity = Vector2.Zero;
                NPC.netUpdate = true;
                return;
            }

            if (dist > 1f) dir /= dist;
            float vel = MathHelper.Clamp(dist * 0.08f, 7f, 22f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, dir * vel, 0.1f);

            if (Math.Abs(NPC.velocity.X) > 0.4f)
                NPC.direction = NPC.spriteDirection = NPC.velocity.X > 0f ? 1 : -1;
        }

        public NPC FindClosestNPC(float maxDistance)
        {
            NPC closest = null;
            float closestDist = maxDistance;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.lifeMax > 5 && !npc.dontTakeDamage && npc.whoAmI != NPC.whoAmI)
                {
                    float dist = Vector2.Distance(NPC.Center, npc.Center);
                    if (dist < closestDist && dist < maxDistance)
                    {
                        closestDist = dist;
                        closest = npc;
                    }
                }
            }
            return closest;
        }

        // ===================== DIBUJO Y ANIMACIÓN =====================
        public override void FindFrame(int frameHeight)
        {
            bool enSuelo = !volando && NPC.velocity.Y == 0f;

            if (volando)
            {
                if (UsarSpritesDeVuelo)
                {
                    if (++frameCounter >= 5)
                    {
                        frameCounter = 0;
                        currentFrame++;
                    }
                    if (currentFrame < FrameVueloInicio || currentFrame > FrameVueloFin)
                        currentFrame = FrameVueloInicio;
                }
                else
                {
                    currentFrame = FrameCayendo;
                }
            }
            else if (!enSuelo)
            {
                currentFrame = NPC.velocity.Y < 0f ? FrameDespegue : FrameCayendo;
                frameCounter = 0;
            }
            else if (Math.Abs(NPC.velocity.X) < 0.4f)
            {
                currentFrame = FrameIdle;
                frameCounter = 0;
            }
            else
            {
                frameCounter += (int)Math.Ceiling(Math.Abs(NPC.velocity.X));
                if (frameCounter >= 16)
                {
                    frameCounter = 0;
                    currentFrame++;
                }
                if (currentFrame < FrameCorrerInicio || currentFrame > FrameCorrerFin)
                    currentFrame = FrameCorrerInicio;
            }

            NPC.frame.Y = currentFrame * frameHeight;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 20; i++)
                {
                    Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, 0f, 0f, 100, default, 1.3f);
                    d.velocity *= 1.5f;
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            int frameH = texture.Height / Main.npcFrameCount[NPC.type];

            Vector2 origin = new Vector2(texture.Width / 2f, frameH);
            Vector2 drawPos = NPC.Bottom - screenPos + new Vector2(0f, OffsetDibujoY + NPC.gfxOffY);
            SpriteEffects effects = NPC.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float rot = volando ? NPC.velocity.X * 0.04f : 0f;

            spriteBatch.Draw(
                texture,
                drawPos,
                NPC.frame,
                drawColor,
                rot,
                origin,
                NPC.scale,
                effects,
                0f
            );

            return false;
        }
    }
}