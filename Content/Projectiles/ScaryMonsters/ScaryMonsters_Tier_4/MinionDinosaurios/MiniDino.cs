using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Systems;
using Jojo.Content.Clases;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4.Marca;

namespace Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4.MinionDinosaurios
{
    /// <summary>
    /// Dinosaurio pequeño del Stand Scary Monsters (compartido por todos los tiers).
    ///  - Vive exactamente 20 segundos (1200 ticks) desde que es invocado/creado.
    ///  - ai[0] = 0 -> permanente (invocado con G)
    ///  - ai[0] = 1 -> temporal (convertido por DinoVirus)
    /// </summary>
    public class MiniDino : ModProjectile
    {
        // ===================== CONFIGURACIÓN DE FRAMES =====================
        const int TotalFrames = 13;
        const int FrameIdle = 0;
        const int FrameCayendo = 1;
        const int FrameDespegue = 3;
        const int FrameCorrerInicio = 4;
        const int FrameCorrerFin = 8;
        const int FrameVueloInicio = 9;
        const int FrameVueloFin = 12;
        static readonly bool UsarSpritesDeVuelo = false;
        const float OffsetDibujoY = 2f;
        // ===================================================================

        bool init;
        bool volando;
        int jumpCooldown;
        int stuckTimer;
        int progressTimer;
        float lastDist;
        bool bajarPlataforma;

        // Perfil del tier activo del dueño (se refresca cada tick)
        ScaryProfile perfil;

        bool Temporal => Projectile.ai[0] == 1f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = TotalFrames;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;

            // Tiempo de vida inicial: 20 segundos (20 * 60 fps = 1200 ticks)
            Projectile.timeLeft = 20 * 60;

            Projectile.minion = false;
            Projectile.minionSlots = 0f;
            Projectile.netImportant = true;
            Projectile.knockBack = 3f;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.spriteDirection = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(volando);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            volando = reader.ReadBoolean();
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = bajarPlataforma;
            return true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => false;

        // =========================================================================================
        //  IA PRINCIPAL
        // =========================================================================================
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            // Perfil según el stand de Scary Monsters que tenga el dueño
            int tierActivo = ScaryTiers.GetActiveTier(owner);
            perfil = ScaryTiers.GetProfile(tierActivo);

            if (!init)
            {
                init = true;
                // Forzamos 20 segundos exactos (1200 ticks) al nacer
                Projectile.timeLeft = 20 * 60;
                EfectoAparicion();
            }

            // Si el jugador se quita el stand de Scary Monsters, mueren inmediatamente
            if (tierActivo == 0)
            {
                Projectile.Kill();
                return;
            }

            // El dueño calcula el daño con sus estadísticas de Stand
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.damage = (int)owner.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(perfil.MiniDinoDano);
                Projectile.CritChance = StandCritSystem.GetFinalCritChance(owner, 1);
            }

            // ---------------- Posición de seguimiento ----------------
            int indice = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile o = Main.projectile[i];
                if (o.active && o.type == Projectile.type && o.owner == Projectile.owner && o.whoAmI < Projectile.whoAmI)
                    indice++;
            }
            int lado = (indice % 2 == 0) ? -1 : 1;
            float offsetX = lado * (20f + (indice % 5) * 8f);
            float metaSeguirX = owner.Center.X + offsetX;

            bool enSuelo = !volando && Projectile.velocity.Y == 0f;
            float distDueno = Vector2.Distance(Projectile.Center, owner.Center);

            // ---------------- Objetivo: la MARCA tiene prioridad ----------------
            NPC marca = ScaryMarca.GetObjetivo(Projectile.owner);
            NPC objetivo = marca ?? (volando ? null : BuscarObjetivo(owner));

            // ---------------- Detección de "atorado" ----------------
            if (!volando)
            {
                Vector2 meta = objetivo != null ? objetivo.Center : new Vector2(metaSeguirX, owner.Center.Y);
                if (++progressTimer >= 30)
                {
                    progressTimer = 0;
                    float d = Vector2.Distance(Projectile.Center, meta);
                    if (d > lastDist - 16f && d > 140f && distDueno > 260f)
                        stuckTimer += 30;
                    else
                        stuckTimer = 0;
                    lastDist = d;
                }
            }

            // ---------------- Transiciones suelo <-> vuelo ----------------
            float distDestino = Vector2.Distance(Projectile.Center, marca != null ? marca.Center : owner.Center);

            if (!volando)
            {
                if ((marca == null && distDueno > perfil.MiniDinoDistanciaVuelo) || stuckTimer >= 120)
                {
                    volando = true;
                    stuckTimer = 0;
                    Projectile.netUpdate = true;
                }
            }
            else
            {
                if (distDestino < 140f && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                {
                    volando = false;
                    stuckTimer = 0;
                    progressTimer = 0;
                    Projectile.netUpdate = true;
                }
            }

            Projectile.tileCollide = !volando;

            if (volando)
                Volar(owner, offsetX, marca);
            else
                Caminar(owner, objetivo, metaSeguirX, enSuelo);

            Animar(enSuelo);
        }

        // =========================================================================================
        //  MOVIMIENTO EN SUELO
        // =========================================================================================
        void Caminar(Player owner, NPC objetivo, float metaSeguirX, bool enSuelo)
        {
            bool atacando = objetivo != null;
            float vMax = perfil.MiniDinoVelocidad;

            float destinoX = atacando ? objetivo.Center.X : metaSeguirX;
            float dx = destinoX - Projectile.Center.X;
            float deseada = 0f;

            if (atacando)
            {
                if (Math.Abs(dx) > 8f)
                    deseada = Math.Sign(dx) * vMax * 1.15f;
            }
            else if (Math.Abs(dx) > 24f)
            {
                float factor = Math.Abs(dx) > 320f ? 1.5f : 1f;
                deseada = Math.Sign(dx) * MathHelper.Clamp(Math.Abs(dx) * 0.07f, 1.8f, vMax * factor);
            }

            deseada += Separacion();

            Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, deseada, enSuelo ? 0.18f : 0.08f);
            if (Math.Abs(deseada) < 0.01f && Math.Abs(Projectile.velocity.X) < 0.1f)
                Projectile.velocity.X = 0f;

            if (atacando && Math.Abs(dx) > 4f)
                Projectile.spriteDirection = dx > 0f ? 1 : -1;
            else if (Math.Abs(Projectile.velocity.X) > 0.3f)
                Projectile.spriteDirection = Projectile.velocity.X > 0f ? 1 : -1;

            if (jumpCooldown > 0) jumpCooldown--;

            Vector2 metaPos = atacando ? objetivo.Center : owner.Center;

            if (enSuelo && jumpCooldown <= 0)
            {
                bool bloqueado = Math.Abs(deseada) > 0.5f && ObstaculoAdelante(Math.Sign(deseada));
                bool metaArriba = metaPos.Y < Projectile.Center.Y - 56f && Math.Abs(dx) < 160f;

                if (bloqueado || metaArriba)
                {
                    float fuerza = perfil.MiniDinoSalto * ((metaArriba && !bloqueado) ? 1.15f : 1f);
                    Projectile.velocity.Y = -fuerza;
                    jumpCooldown = 25;
                }
            }

            bajarPlataforma = metaPos.Y > Projectile.Bottom.Y + 40f && Math.Abs(dx) < 80f;

            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.4f, 12f);
        }

        bool ObstaculoAdelante(int dir)
        {
            Vector2 punto = Projectile.Center + new Vector2(dir * (Projectile.width / 2f + 8f), Projectile.height / 2f - 8f);
            Point tp = punto.ToTileCoordinates();
            Tile t = Framing.GetTileSafely(tp.X, tp.Y);
            return t.HasUnactuatedTile && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType];
        }

        float Separacion() => 0f;

        // =========================================================================================
        //  VUELO
        // =========================================================================================
        void Volar(Player owner, float offsetX, NPC marca)
        {
            bajarPlataforma = true;

            Vector2 destino = marca != null
                ? marca.Center
                : owner.Center + new Vector2(offsetX, -24f);
            Vector2 dir = destino - Projectile.Center;
            float dist = dir.Length();

            if (marca == null && dist > 2400f)
            {
                Projectile.Center = destino;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
                return;
            }

            if (dist > 1f) dir /= dist;
            float vel = MathHelper.Clamp(dist * 0.08f, 7f, 22f);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, dir * vel, 0.1f);

            if (Math.Abs(Projectile.velocity.X) > 0.4f)
                Projectile.spriteDirection = Projectile.velocity.X > 0f ? 1 : -1;
        }

        // =========================================================================================
        //  OBJETIVOS
        // =========================================================================================
        NPC BuscarObjetivo(Player owner)
        {
            float rango = perfil.MiniDinoRangoDeteccion;

            if (owner.HasMinionAttackTargetNPC)
            {
                NPC t = Main.npc[owner.MinionAttackTargetNPC];
                if (t.CanBeChasedBy(Projectile) && Vector2.Distance(t.Center, owner.Center) < rango * 1.5f)
                    return t;
            }

            NPC mejor = null;
            float mejorDist = rango;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!n.CanBeChasedBy(Projectile)) continue;

                if (Vector2.Distance(n.Center, owner.Center) > rango * 1.3f) continue;

                float d = Vector2.Distance(n.Center, Projectile.Center);
                if (d < mejorDist)
                {
                    mejorDist = d;
                    mejor = n;
                }
            }
            return mejor;
        }

        // =========================================================================================
        //  ANIMACIÓN
        // =========================================================================================
        void Animar(bool enSuelo)
        {
            if (volando)
            {
                if (UsarSpritesDeVuelo)
                {
                    if (++Projectile.frameCounter >= 5)
                    {
                        Projectile.frameCounter = 0;
                        Projectile.frame++;
                    }
                    if (Projectile.frame < FrameVueloInicio || Projectile.frame > FrameVueloFin)
                        Projectile.frame = FrameVueloInicio;
                }
                else
                {
                    Projectile.frame = FrameCayendo;
                }
                return;
            }

            if (!enSuelo)
            {
                Projectile.frame = Projectile.velocity.Y < 0f ? FrameDespegue : FrameCayendo;
                Projectile.frameCounter = 0;
                return;
            }

            if (Math.Abs(Projectile.velocity.X) < 0.4f)
            {
                Projectile.frame = FrameIdle;
                Projectile.frameCounter = 0;
                return;
            }

            Projectile.frameCounter += (int)Math.Ceiling(Math.Abs(Projectile.velocity.X));
            if (Projectile.frameCounter >= 16)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
            }
            if (Projectile.frame < FrameCorrerInicio || Projectile.frame > FrameCorrerFin)
                Projectile.frame = FrameCorrerInicio;
        }

        // =========================================================================================
        //  COMBATE
        // =========================================================================================
        public override bool? CanHitNPC(NPC target)
        {
            if (ScaryMarca.EsMarcado(Projectile.owner, target))
                return true;
            return null;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Projectile.Center.X ? 1 : -1;
        }

        // =========================================================================================
        //  EFECTOS
        // =========================================================================================
        void EfectoAparicion()
        {
            for (int i = 0; i < 14; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                    Main.rand.NextBool() ? DustID.GreenTorch : DustID.Smoke, 0f, 0f, 100, default, 1.3f);
                d.noGravity = true;
                d.velocity = Main.rand.NextVector2Circular(3f, 3f);
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 12; i++)
            {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                    Main.rand.NextBool() ? DustID.GreenTorch : DustID.Smoke, 0f, 0f, 100, default, 1.2f);
                d.noGravity = true;
                d.velocity = Main.rand.NextVector2Circular(2.5f, 2.5f);
            }
        }

        // =========================================================================================
        //  DIBUJO
        // =========================================================================================
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            int frameH = tex.Height / TotalFrames;
            Rectangle src = new Rectangle(0, Projectile.frame * frameH, tex.Width, frameH);
            Vector2 origen = new Vector2(tex.Width / 2f, frameH);
            Vector2 pos = Projectile.Bottom - Main.screenPosition + new Vector2(0f, OffsetDibujoY);

            SpriteEffects fx = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float rot = volando ? Projectile.velocity.X * 0.04f : 0f;

            Color color = lightColor;

            // Parpadea durante los últimos 2 segundos (120 ticks) antes de desaparecer
            if (Projectile.timeLeft < 120 && (Projectile.timeLeft / 6) % 2 == 0)
                color *= 0.4f;

            Main.EntitySpriteDraw(tex, pos, src, color, rot, origen, 1f, fx, 0);
            return false;
        }
    }
}