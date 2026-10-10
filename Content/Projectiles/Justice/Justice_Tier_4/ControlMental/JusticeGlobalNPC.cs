using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

using Jojo.Content.Buffs.Justice_Buffs;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.Justice.Justice_Tier_4
{
    public class JusticeGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        // ---- Estado de control (sincronizado servidor -> clientes) ----
        public bool bajoControlMental = false;
        public int duenoIndex = -1;
        public string duenoNombre = "";
        private int duenoAusenteTimer = 0;

        // ---- Vida ----
        public bool vidaDuplicada = false;
        public int vidaMaxOriginal = 0;

        // ---- Niebla / marca ----
        public int insideNieblaTimer = 0;
        public int justiceMarkTimer = 0;
        public int markOwner = -1;
        public int markFrame = 0;
        public int markFrameCounter = 0;
        public int manualAttackCooldown = 0;

        public float idlePhase = 0f;

        // Spoofing
        private bool isSpoofing = false;
        private readonly Vector2[] posOriginal = new Vector2[Main.maxPlayers];
        private readonly Vector2[] velOriginal = new Vector2[Main.maxPlayers];

        // ====================================================================
        // BOSSES PRINCIPALES: nunca se pueden controlar.
        // ====================================================================
        public static bool EsBossPrincipal(NPC npc)
        {
            if (npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type])
                return true;

            if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs)
            {
                NPC cabeza = Main.npc[npc.realLife];
                if (cabeza.boss || NPCID.Sets.ShouldBeCountedAsBoss[cabeza.type])
                    return true;
            }

            if (npc.dontTakeDamage || npc.type == NPCID.TargetDummy)
                return true;

            return false;
        }

        // ====================================================================
        // CONTROLAR / LIBERAR (solo servidor o singleplayer)
        // ====================================================================
        public static void Controlar(NPC npc, int owner)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (owner < 0 || owner >= Main.maxPlayers) return;

            if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs)
                npc = Main.npc[npc.realLife];

            if (!npc.active || EsBossPrincipal(npc)) return;

            JusticeGlobalNPC g = npc.GetGlobalNPC<JusticeGlobalNPC>();

            bool controlValido = g.bajoControlMental
                && npc.FindBuffIndex(ModContent.BuffType<ControlMental1>()) != -1
                && g.duenoIndex >= 0 && g.duenoIndex < Main.maxPlayers
                && Main.player[g.duenoIndex].active
                && Main.player[g.duenoIndex].name == g.duenoNombre;
            if (controlValido) return;

            g.bajoControlMental = true;
            g.duenoIndex = owner;
            g.duenoNombre = Main.player[owner].name;
            g.duenoAusenteTimer = 0;
            g.justiceMarkTimer = 0;
            g.markOwner = -1;
            g.manualAttackCooldown = 0;

            // La vida se regenera (y se multiplica según el stand, por defecto x1) solo al transformarse
            if (!g.vidaDuplicada)
            {
                float multVida = JUSTICESTAND_Tier_4.Config(owner).MultVida;
                g.vidaMaxOriginal = npc.lifeMax;
                npc.lifeMax = Math.Max(1, (int)(npc.lifeMax * multVida));
                npc.life = npc.lifeMax;
                g.vidaDuplicada = true;
            }

            npc.friendly = true;
            npc.chaseable = false;
            npc.AddBuff(ModContent.BuffType<ControlMental1>(), 18000);

            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.NPCBuffs, -1, -1, null, npc.whoAmI);

            npc.netUpdate = true;
        }

        public static void Liberar(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs)
                npc = Main.npc[npc.realLife];

            JusticeGlobalNPC g = npc.GetGlobalNPC<JusticeGlobalNPC>();

            // Restaurar vida máxima SIN curar: se conserva el porcentaje de vida actual
            if (g.vidaDuplicada && g.vidaMaxOriginal > 0)
            {
                float ratio = npc.lifeMax > 0 ? (float)npc.life / npc.lifeMax : 1f;
                npc.lifeMax = g.vidaMaxOriginal;
                npc.life = Math.Clamp((int)Math.Ceiling(ratio * npc.lifeMax), 1, npc.lifeMax);
            }
            g.vidaDuplicada = false;
            g.vidaMaxOriginal = 0;

            int buffIndex = npc.FindBuffIndex(ModContent.BuffType<ControlMental1>());
            if (buffIndex != -1) npc.DelBuff(buffIndex);

            g.bajoControlMental = false;
            g.duenoIndex = -1;
            g.duenoNombre = "";
            g.duenoAusenteTimer = 0;
            g.justiceMarkTimer = 0;
            g.markOwner = -1;
            g.insideNieblaTimer = 0;
            g.manualAttackCooldown = 0;

            npc.friendly = false;
            npc.chaseable = ContentSamples.NpcsByNetId[npc.type].chaseable;
            npc.defense = npc.defDefense;
            npc.damage = npc.defDamage;

            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.NPCBuffs, -1, -1, null, npc.whoAmI);

            npc.netUpdate = true;
        }

        // ====================================================================
        // SINCRONIZACIÓN
        // ====================================================================
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(bajoControlMental);
            binaryWriter.Write((short)duenoIndex);
            binaryWriter.Write(duenoNombre ?? "");
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            bool eraControlado = bajoControlMental;
            bajoControlMental = bitReader.ReadBit();
            duenoIndex = binaryReader.ReadInt16();
            duenoNombre = binaryReader.ReadString();

            if (bajoControlMental)
            {
                npc.friendly = true;
                npc.chaseable = false;
            }
            else if (eraControlado)
            {
                npc.friendly = false;
                npc.chaseable = ContentSamples.NpcsByNetId[npc.type].chaseable;
            }
        }

        // ====================================================================
        // UTILIDADES
        // ====================================================================
        private Player ObtenerDueno(NPC npc)
        {
            if (!string.IsNullOrEmpty(duenoNombre))
            {
                bool indiceOk = duenoIndex >= 0 && duenoIndex < Main.maxPlayers
                    && Main.player[duenoIndex].active
                    && Main.player[duenoIndex].name == duenoNombre;

                if (!indiceOk)
                {
                    duenoIndex = -1;
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        if (Main.player[i].active && Main.player[i].name == duenoNombre)
                        {
                            duenoIndex = i;
                            break;
                        }
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient) npc.netUpdate = true;
                }
            }
            else if (duenoIndex < 0 || duenoIndex >= Main.maxPlayers)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) return null;
                duenoIndex = Player.FindClosest(npc.position, npc.width, npc.height);
                duenoNombre = Main.player[duenoIndex].name;
                npc.netUpdate = true;
            }

            if (duenoIndex < 0 || duenoIndex >= Main.maxPlayers) return null;
            Player p = Main.player[duenoIndex];
            return p.active ? p : null;
        }

        public static bool IsValidTarget(NPC target)
        {
            if (!target.active || target.friendly || target.dontTakeDamage) return false;
            if (target.lifeMax <= 5) return false;
            if (NPCID.Sets.CountsAsCritter[target.type] || !target.chaseable) return false;
            if (target.GetGlobalNPC<JusticeGlobalNPC>().bajoControlMental) return false;
            return true;
        }

        private NPC BuscarObjetivo(NPC npc, int dueno)
        {
            NPC cercano = null;
            float minDist = JUSTICESTAND_Tier_4.Config(dueno).RadioBusquedaObjetivo;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!n.active || n.whoAmI == npc.whoAmI || !IsValidTarget(n)) continue;

                JusticeGlobalNPC g = n.GetGlobalNPC<JusticeGlobalNPC>();
                if (g.justiceMarkTimer > 0 && g.markOwner == dueno)
                    return n;

                float dist = Vector2.Distance(npc.Center, n.Center);
                if (dist < minDist)
                {
                    minDist = dist;
                    cercano = n;
                }
            }
            return cercano;
        }

        private void IniciarSpoof(NPC npc, Vector2 centroDestino, Vector2 velocidadDestino, int duenoIdx)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player pl = Main.player[i];
                posOriginal[i] = pl.position;
                velOriginal[i] = pl.velocity;

                if (!pl.active || pl.dead) continue;

                pl.position = centroDestino - new Vector2(pl.width / 2f, pl.height / 2f);
                pl.velocity = velocidadDestino;
            }

            npc.target = duenoIdx;
            isSpoofing = true;
        }

        // ====================================================================
        // HOOKS
        // ====================================================================
        public override bool CheckActive(NPC npc)
        {
            if (bajoControlMental) return false;
            return base.CheckActive(npc);
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (!bajoControlMental) return;

            if (EsBossPrincipal(npc))
            {
                Liberar(npc);
                return;
            }

            Player owner = ObtenerDueno(npc);
            JUSTICESTAND_Tier_4 cfg = JUSTICESTAND_Tier_4.Config(owner != null ? owner.whoAmI : duenoIndex);

            // Sin bonos del jugador: solo el multiplicador del stand (por defecto x1 = daño y defensa base)
            npc.defense = (int)Math.Round(npc.defDefense * cfg.MultDefensa);
            npc.damage = (int)Math.Round(npc.defDamage * cfg.MultDano);
        }

        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (bajoControlMental) return false;
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }

        public override bool? CanBeHitByItem(NPC npc, Player player, Item item)
        {
            if (bajoControlMental && player.whoAmI == duenoIndex) return false;
            return base.CanBeHitByItem(npc, player, item);
        }

        public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
        {
            if (bajoControlMental && projectile.owner == duenoIndex) return false;
            return base.CanBeHitByProjectile(npc, projectile);
        }

        public override bool CanHitNPC(NPC npc, NPC target)
        {
            if (bajoControlMental && !IsValidTarget(target)) return false;
            return base.CanHitNPC(npc, target);
        }

        public override bool PreAI(NPC npc)
        {
            bool esCliente = Main.netMode == NetmodeID.MultiplayerClient;

            // 1. GESTIÓN DE SEGMENTOS (Worms / Wyverns)
            if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs)
            {
                NPC cabeza = Main.npc[npc.realLife];
                if (cabeza.active)
                {
                    JusticeGlobalNPC gc = cabeza.GetGlobalNPC<JusticeGlobalNPC>();

                    if (gc.bajoControlMental)
                    {
                        bajoControlMental = true;
                        duenoIndex = gc.duenoIndex;
                        duenoNombre = gc.duenoNombre;
                        npc.friendly = true;
                        npc.chaseable = false;
                        npc.timeLeft = Math.Max(npc.timeLeft, 3000);

                        if (npc.whoAmI != npc.realLife) return true;
                    }
                    else if (bajoControlMental && npc.whoAmI != npc.realLife)
                    {
                        bajoControlMental = false;
                        duenoIndex = -1;
                        duenoNombre = "";
                        npc.friendly = false;
                        npc.chaseable = ContentSamples.NpcsByNetId[npc.type].chaseable;
                        npc.defense = npc.defDefense;
                        npc.damage = npc.defDamage;
                    }
                }
            }

            // 2. ANIMACIÓN / TEMPORIZADOR DE LA MARCA
            if (justiceMarkTimer > 0)
            {
                justiceMarkTimer--;
                if (bajoControlMental) justiceMarkTimer = 0;
                if (justiceMarkTimer <= 0) markOwner = -1;

                markFrameCounter++;
                if (markFrameCounter >= 6)
                {
                    markFrameCounter = 0;
                    markFrame++;
                    if (markFrame >= 4) markFrame = 0;
                }
            }

            // 3. INFECCIÓN EN LA NIEBLA (solo servidor / singleplayer)
            //    El tiempo necesario se configura en el stand con TiempoInfeccion.
            if (!esCliente && !bajoControlMental && !npc.friendly && npc.lifeMax > 5 && !EsBossPrincipal(npc))
            {
                int duenoNiebla = -1;

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active && proj.ModProjectile is Justice_Niebla niebla)
                    {
                        if (Vector2.Distance(npc.Center, proj.Center) <= niebla.Radio)
                        {
                            duenoNiebla = proj.owner;
                            break;
                        }
                    }
                }

                if (duenoNiebla != -1 && npc.life < npc.lifeMax)
                {
                    insideNieblaTimer++;
                    if (insideNieblaTimer >= JUSTICESTAND_Tier_4.Config(duenoNiebla).TiempoInfeccion)
                    {
                        insideNieblaTimer = 0;
                        Controlar(npc, duenoNiebla);
                    }
                }
                else
                {
                    insideNieblaTimer = 0;
                }
            }

            // 4. LÓGICA DE CONTROL MENTAL ACTIVA
            if (bajoControlMental)
            {
                npc.friendly = true;
                npc.chaseable = false;
                npc.timeLeft = Math.Max(npc.timeLeft, 3000);

                Player owner = ObtenerDueno(npc);

                if (!esCliente)
                {
                    bool esCabezaOEntero = npc.realLife < 0 || npc.whoAmI == npc.realLife;

                    if (esCabezaOEntero && npc.FindBuffIndex(ModContent.BuffType<ControlMental1>()) == -1)
                    {
                        Liberar(npc);
                        return true;
                    }

                    if (owner == null || owner.dead)
                    {
                        if (++duenoAusenteTimer > 300)
                        {
                            Liberar(npc);
                            return true;
                        }
                    }
                    else duenoAusenteTimer = 0;
                }
                if (owner == null) return true;

                JUSTICESTAND_Tier_4 cfg = JUSTICESTAND_Tier_4.Config(owner.whoAmI);
                float distToPlayer = Vector2.Distance(npc.Center, owner.Center);

                // TELETRANSPORTE: si se queda atrás, aparece junto al dueño (lo decide el servidor)
                if (!esCliente && !owner.dead && distToPlayer > cfg.DistanciaTeletransporte)
                {
                    npc.Center = owner.Center + new Vector2(0f, -20f);
                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                    return false;
                }

                // El cooldown de golpe baja siempre, haya o no objetivo
                if (manualAttackCooldown > 0) manualAttackCooldown--;

                NPC targetEnemy = BuscarObjetivo(npc, owner.whoAmI);

                // MODO COMBATE
                if (targetEnemy != null)
                {
                    IniciarSpoof(npc, targetEnemy.Center, targetEnemy.velocity, owner.whoAmI);

                    if (!esCliente && manualAttackCooldown <= 0 && npc.Hitbox.Intersects(targetEnemy.Hitbox))
                    {
                        int hitDirection = Math.Sign(targetEnemy.Center.X - npc.Center.X);
                        if (hitDirection == 0) hitDirection = 1;

                        // Daño base del minion (sin bonos). CalculateHitInfo aplica la defensa del objetivo.
                        int dano = Math.Max(1, npc.damage);
                        NPC.HitInfo hit = targetEnemy.CalculateHitInfo(dano, hitDirection, false, 3f, DamageClass.Default, true, owner.whoAmI);
                        targetEnemy.StrikeNPC(hit);

                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendStrikeNPC(targetEnemy, hit);

                        manualAttackCooldown = Math.Max(1, cfg.CooldownGolpe);
                    }

                    return true;
                }

                // MODO PACÍFICO
                idlePhase += 0.035f;
                if (idlePhase > MathHelper.TwoPi) idlePhase -= MathHelper.TwoPi;

                Vector2 offsetPaseo;
                if (npc.noGravity)
                {
                    offsetPaseo = new Vector2(
                        (float)Math.Cos(idlePhase) * 150f,
                        (float)Math.Sin(idlePhase) * 60f - 70f
                    );
                }
                else
                {
                    offsetPaseo = new Vector2((float)Math.Sin(idlePhase) * 90f, 0f);
                }

                Vector2 destino = distToPlayer > 400f ? owner.Center : owner.Center + offsetPaseo;

                IniciarSpoof(npc, destino, Vector2.Zero, owner.whoAmI);
                return true;
            }

            return base.PreAI(npc);
        }

        public override void PostAI(NPC npc)
        {
            if (isSpoofing)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Main.player[i].position = posOriginal[i];
                    Main.player[i].velocity = velOriginal[i];
                }
                isSpoofing = false;
            }
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (bajoControlMental)
            {
                Texture2D texture = ModContent.Request<Texture2D>("Jojo/Content/Buffs/Justice_Buffs/ControlMental1").Value;
                Vector2 drawPos = new Vector2(npc.Center.X, npc.position.Y - 24) - screenPos;
                Vector2 origin = texture.Size() / 2f;
                spriteBatch.Draw(texture, drawPos, null, Color.White, 0f, origin, 1.0f, SpriteEffects.None, 0f);
            }

            if (justiceMarkTimer > 0 && !bajoControlMental)
            {
                Texture2D texture = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/Justice/Justice_Tier_4/Marca/Justice_Marca_Tier_4").Value;
                int frameHeight = texture.Height / 4;
                Rectangle sourceRect = new Rectangle(0, markFrame * frameHeight, texture.Width, frameHeight);
                Vector2 origin = new Vector2(texture.Width / 2f, frameHeight / 2f);
                Vector2 drawPos = npc.Center - screenPos;

                spriteBatch.Draw(texture, drawPos, sourceRect, Color.White, 0f, origin, 1.0f, SpriteEffects.None, 0f);
            }
        }
    }
}