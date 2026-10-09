using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Buffs.WeatherReport_Buffs;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_3.LluviaDeRanas_Tier_3;

namespace Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_4.LluviaDeRanas_Tier_4
{
    public class RanaVenenosa_Tier_4 : ModNPC
    {
        // ---------------- Frames ----------------
        private const int FRAME_IDLE_START = 0;
        private const int FRAME_IDLE_END = 5;
        private const int FRAME_JUMP_START = 6;
        private const int FRAME_JUMP_END = 9;
        private const int TOTAL_FRAMES = 10;

        private const int IDLE_FRAME_SPEED = 8;
        private const int JUMP_FRAME_SPEED = 4;

        // ---------------- Salto / plataformas ----------------
        private const float VELOCIDAD_SALTO_NORMAL = -6f;
        private const float VELOCIDAD_SALTO_ALTO = -11f;
        private const int PROBABILIDAD_SALTO_ALTO = 3;

        private const int BAJADA_DURACION = 20;
        private const int PROBABILIDAD_BAJAR = 2;

        // ---------------- Estado ----------------
        private bool EstaSaltando => NPC.ai[0] == 1f;
        private bool enPlataforma = false;
        private bool bajandoPlataforma = false;
        private int bajadaTimer = 0;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = TOTAL_FRAMES;
            NPCID.Sets.CountsAsCritter[NPC.type] = true;
            NPCID.Sets.NPCBestiaryDrawOffset[NPC.type] = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Velocity = 0f
            };
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 20;

            NPC.friendly = false;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.lifeMax = 5;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 0f; // ESTO EVITA QUE SUELTE MONEDAS
            NPC.knockBackResist = 1f;
            NPC.dontTakeDamage = false;
            NPC.catchItem = (short)ItemID.BugNet;

            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.aiStyle = -1;
            NPC.lavaImmune = false;

            // Inmunidad TOTAL y ABSOLUTA a cualquier buff/debuff (presente o futuro)
            for (int i = 0; i < NPC.buffImmune.Length; i++)
            {
                NPC.buffImmune[i] = true;
            }
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Underground,
                new FlavorTextBestiaryInfoElement("Una pequeña rana venenosa. Inofensiva a menos que la molestes.")
            });
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            return spawnInfo.Player.ZoneRockLayerHeight ? 0.1f : 0f;
        }

        public override void AI()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC target = Main.npc[i];

                    if (target.active && !target.friendly && target.damage > 0 && target.type != NPC.type)
                    {
                        if (NPC.Hitbox.Intersects(target.Hitbox))
                        {
                            ExplotarKamikaze();
                            return;
                        }
                    }
                }
            }

            if (bajandoPlataforma)
            {
                bajadaTimer++;
                if (bajadaTimer >= BAJADA_DURACION)
                {
                    bajandoPlataforma = false;
                    NPC.noTileCollide = false;
                    NPC.netUpdate = true;
                }
                return;
            }

            bool enSuelo = NPC.velocity.Y == 0f && NPC.collideY;

            if (enSuelo)
            {
                enPlataforma = EstaSobrePlataforma();
            }

            if (!EstaSaltando)
            {
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, 0f, 0.2f);

                if (enSuelo)
                {
                    NPC.ai[1]++;
                    int tiempoParaSaltar = 150 + NPC.whoAmI % 90;

                    if (NPC.ai[1] >= tiempoParaSaltar)
                    {
                        if (enPlataforma && Main.rand.NextBool(PROBABILIDAD_BAJAR))
                        {
                            IniciarBajada();
                        }
                        else
                        {
                            IniciarSalto();
                        }
                    }
                }
            }
            else
            {
                NPC.velocity.X = NPC.ai[2] * 2.2f;

                if (enSuelo && NPC.velocity.Y == 0f)
                {
                    TerminarSalto();
                }
            }

            if (NPC.velocity.X != 0f)
            {
                NPC.spriteDirection = NPC.velocity.X > 0 ? 1 : -1;
            }
        }

        private bool EstaSobrePlataforma()
        {
            int tileX = (int)(NPC.Center.X / 16f);
            int tileY = (int)((NPC.position.Y + NPC.height + 2f) / 16f);

            if (!WorldGen.InWorld(tileX, tileY)) return false;

            Tile tile = Main.tile[tileX, tileY];
            return tile != null && tile.HasTile && TileID.Sets.Platforms[tile.TileType];
        }

        private void IniciarBajada()
        {
            bajandoPlataforma = true;
            bajadaTimer = 0;
            NPC.noTileCollide = true;
            NPC.velocity.Y = 3f;
            NPC.netUpdate = true;
        }

        private void ExplotarKamikaze()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.life = 0;
                NPC.checkDead(); // Esto activa OnKill en el Server y HitEffect en todos los Clientes (Multijugador)
            }
        }

        // --- LÓGICA DE BUFF (Solo corre en el Servidor) ---
        public override void OnKill()
        {
            int buffType = ModContent.BuffType<Veneno_Tier_4>();
            float radioDeExplosion = 120f;

            int tipoRana3 = ModContent.NPCType<RanaVenenosa_Tier_3>();
            int tipoRana4 = ModContent.NPCType<RanaVenenosa_Tier_4>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC target = Main.npc[i];

                // Nunca afectar a NINGUNA rana (ni Tier_3 ni Tier_4)
                if (target.active && !target.friendly &&
                    target.type != tipoRana3 && target.type != tipoRana4)
                {
                    if (Vector2.Distance(NPC.Center, target.Center) <= radioDeExplosion)
                    {
                        target.buffImmune[buffType] = false;
                        target.AddBuff(buffType, 600);
                    }
                }
            }
        }

        // --- EFECTOS VISUALES Y PARTÍCULAS (Corre en todos los Clientes) ---
        // NOTA: Si usas tModLoader 1.4.3, cambia "NPC.HitInfo hit" por "int hitDirection, double damage"
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0) // Si la rana muere
            {
                for (int i = 0; i < 40; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
                    Dust.NewDust(NPC.Center, 0, 0, DustID.PurpleTorch, vel.X, vel.Y, 100, default, 2f);
                }
            }
        }

        private void IniciarSalto()
        {
            NPC.ai[0] = 1f;
            NPC.ai[1] = 0f;
            NPC.frameCounter = 0;
            float direccion = Main.rand.NextBool() ? 1 : -1;
            NPC.ai[2] = direccion;

            bool saltoAlto = Main.rand.NextBool(PROBABILIDAD_SALTO_ALTO);
            NPC.velocity.Y = saltoAlto ? VELOCIDAD_SALTO_ALTO : VELOCIDAD_SALTO_NORMAL;
            NPC.velocity.X = direccion * 2.2f;
            NPC.netUpdate = true;
        }

        private void TerminarSalto()
        {
            NPC.ai[0] = 0f;
            NPC.ai[1] = 0f;
            NPC.velocity.X = 0f;
            NPC.frameCounter = 0;
            NPC.netUpdate = true;
        }

        public override void FindFrame(int frameHeight)
        {
            if (!EstaSaltando)
            {
                AnimarTramo(FRAME_IDLE_START, FRAME_IDLE_END, IDLE_FRAME_SPEED, frameHeight, loop: true);
            }
            else
            {
                AnimarTramo(FRAME_JUMP_START, FRAME_JUMP_END, JUMP_FRAME_SPEED, frameHeight, loop: false);
            }
        }

        private void AnimarTramo(int inicio, int fin, int velocidad, int frameHeight, bool loop)
        {
            NPC.frameCounter++;
            if (NPC.frameCounter >= velocidad)
            {
                NPC.frameCounter = 0;
                int frameActual = NPC.frame.Y / frameHeight;

                if (frameActual < inicio || frameActual > fin)
                {
                    frameActual = inicio;
                }
                else
                {
                    frameActual++;
                    if (frameActual > fin)
                    {
                        frameActual = loop ? inicio : fin;
                    }
                }
                NPC.frame.Y = frameActual * frameHeight;
            }
        }
    }
}