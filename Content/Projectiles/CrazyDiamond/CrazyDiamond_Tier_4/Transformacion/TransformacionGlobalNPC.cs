using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.IO;
using System.Collections.Generic;
using Terraria.GameContent;
using Terraria.ID;

namespace Jojo.Content.Projectiles.CrazyDiamond.Transformacion
{
    public class TransformacionGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public float charge = 0f;
        public float smoothCharge = 0f;
        public const float MAX_CHARGE = 100f;

        // --- COOLDOWN (BARRA AZUL) ---
        public float rockCooldown = 0f;
        public float smoothCooldown = 0f;
        public const float MAX_COOLDOWN = 100f;

        public bool isTrappedInRock = false;
        public int trappedRockWhoAmI = -1;

        public bool isAboutToBeTrapped = false;
        public int trapTimer = 0;
        public int timeSinceLastHit = 0;

        public Vector2 triggerPosition = Vector2.Zero;

        // --- GUARDADO DE VELOCIDAD (FIX PARA DASH DE JEFES) ---
        public Vector2 savedVelocity = Vector2.Zero;

        // --- VARIABLES DE CONFIGURACIÓN ---
        public float currentDecayRate = 0.8f;
        public int currentDecayDelay = 90;
        public float currentDefenseMult = 0.5f;
        public int currentCinDuration = 60;
        public int currentRockSpawnRate = 3;
        public float currentBossHpThreshold = 500f;
        public float currentBossHpScaling = 0.2f;
        public float currentCooldownDecay = 0.1f;

        // Daño base de la transformación (viene del stand, escala con daño de clase Stand).
        // Solo lo usa el servidor / singleplayer, no hace falta sincronizarlo.
        public float currentTransformDamage = 200f;

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(charge);
            binaryWriter.Write(rockCooldown);
            bitWriter.WriteBit(isTrappedInRock);
            bitWriter.WriteBit(isAboutToBeTrapped);
            binaryWriter.Write((short)trapTimer);
            binaryWriter.Write((short)trappedRockWhoAmI);

            binaryWriter.Write(currentDecayRate);
            binaryWriter.Write((short)currentDecayDelay);
            binaryWriter.Write(currentDefenseMult);
            binaryWriter.Write((short)currentCinDuration);
            binaryWriter.Write((short)currentRockSpawnRate);
            binaryWriter.Write(currentBossHpThreshold);
            binaryWriter.Write(currentBossHpScaling);
            binaryWriter.Write(currentCooldownDecay);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            bool wasTrapped = isTrappedInRock;

            charge = binaryReader.ReadSingle();
            rockCooldown = binaryReader.ReadSingle();
            isTrappedInRock = bitReader.ReadBit();
            isAboutToBeTrapped = bitReader.ReadBit();
            trapTimer = binaryReader.ReadInt16();
            trappedRockWhoAmI = binaryReader.ReadInt16();

            currentDecayRate = binaryReader.ReadSingle();
            currentDecayDelay = binaryReader.ReadInt16();
            currentDefenseMult = binaryReader.ReadSingle();
            currentCinDuration = binaryReader.ReadInt16();
            currentRockSpawnRate = binaryReader.ReadInt16();
            currentBossHpThreshold = binaryReader.ReadSingle();
            currentBossHpScaling = binaryReader.ReadSingle();
            currentCooldownDecay = binaryReader.ReadSingle();

            // Transición: atrapado -> liberado (visto desde un cliente)
            if (wasTrapped && !isTrappedInRock)
            {
                npc.hide = false;
                npc.dontTakeDamage = false;
                smoothCharge = 0f;
                smoothCooldown = rockCooldown;

                if (!Main.dedServ && GetMasterNPC(npc).whoAmI == npc.whoAmI)
                {
                    Roca.PlayBreakEffects(npc.Center, 32, 32, npc.GetSource_Death());
                }
            }
        }

        // --- SISTEMA DE AGRUPACIÓN UNIVERSAL ---
        public NPC GetMasterNPC(NPC npc)
        {
            // 1. Regla universal: Gusanos y multisegmentos que usan el sistema estándar 'realLife'
            if (npc.realLife >= 0 && Main.npc[npc.realLife].active)
                return Main.npc[npc.realLife];

            // 2. Excepción explícita Moon Lord
            if (npc.type == NPCID.MoonLordHead || npc.type == NPCID.MoonLordHand || npc.type == NPCID.MoonLordCore)
            {
                int coreIdx = (npc.type == NPCID.MoonLordCore) ? npc.whoAmI : (int)npc.ai[3];
                if (coreIdx >= 0 && coreIdx < Main.maxNPCs && Main.npc[coreIdx].active && Main.npc[coreIdx].type == NPCID.MoonLordCore)
                    return Main.npc[coreIdx];
            }

            // 3. Regla por defecto: cada parte actúa independientemente
            return npc;
        }

        public List<NPC> GetAllGroupedParts(NPC npc)
        {
            List<NPC> segments = new List<NPC>();
            NPC master = GetMasterNPC(npc);
            int masterIdx = master.whoAmI;

            foreach (NPC n in Main.npc)
            {
                if (n.active)
                {
                    if (n.TryGetGlobalNPC(out TransformacionGlobalNPC gNPC))
                    {
                        if (gNPC.GetMasterNPC(n).whoAmI == masterIdx)
                        {
                            segments.Add(n);
                        }
                    }
                }
            }
            return segments;
        }

        // Solo servidor / singleplayer. Varios jugadores pueden llamar esto a la vez: la carga se suma.
        public void AddCharge(NPC masterNpc, Vector2 hitPosition, float amount, float decayRate, int decayDelay, float defenseMult, int cinDuration, int rockSpwnRate, float bossHpThreshold, float bossHpScaling, float cooldownDecay, float transformDamage)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (masterNpc.type == ModContent.NPCType<Roca>()) return;
            if (isTrappedInRock || isAboutToBeTrapped || masterNpc.friendly || masterNpc.townNPC) return;

            // --- BLOQUEO POR COOLDOWN: mientras haya barra azul NADIE puede cargarlo ---
            if (rockCooldown > 0f) return;

            // --- ESCALADO POR VIDA: +bossHpScaling (0.2 = +20%) de tiempo por cada 500 HP sobre el umbral ---
            float actualAmount = amount;
            if (masterNpc.lifeMax > bossHpThreshold)
            {
                float extraHp = masterNpc.lifeMax - bossHpThreshold;
                float extra500s = extraHp / 500f;
                float penaltyMultiplier = 1f + (extra500s * bossHpScaling);
                actualAmount /= penaltyMultiplier;
            }

            charge += actualAmount;
            timeSinceLastHit = 0;
            triggerPosition = hitPosition;

            currentDecayRate = decayRate;
            currentDecayDelay = decayDelay;
            currentDefenseMult = defenseMult;
            currentCinDuration = cinDuration;
            currentRockSpawnRate = rockSpwnRate;
            currentBossHpThreshold = bossHpThreshold;
            currentBossHpScaling = bossHpScaling;
            currentCooldownDecay = cooldownDecay;
            currentTransformDamage = transformDamage;

            if (charge >= MAX_CHARGE)
            {
                charge = MAX_CHARGE;
                isAboutToBeTrapped = true;
                trapTimer = 0;
            }

            masterNpc.netUpdate = true;
        }

        public override bool PreAI(NPC npc)
        {
            if (isTrappedInRock)
            {
                if (npc.type != NPCID.TargetDummy)
                {
                    npc.velocity = Vector2.Zero;
                }
                return false; // Congela la IA mientras es piedra.
            }
            return base.PreAI(npc);
        }

        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (isTrappedInRock) return false;
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }

        public override bool CanHitNPC(NPC npc, NPC target)
        {
            if (isTrappedInRock) return false;
            return base.CanHitNPC(npc, target);
        }

        public override bool? CanBeHitByItem(NPC npc, Player player, Item item)
        {
            if (isTrappedInRock) return false;
            return null;
        }

        public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
        {
            if (isTrappedInRock) return false;
            return null;
        }

        public override void PostAI(NPC npc)
        {
            NPC master = GetMasterNPC(npc);
            bool isMaster = npc.whoAmI == master.whoAmI;
            bool authority = Main.netMode != NetmodeID.MultiplayerClient; // servidor o singleplayer

            // Interpolación suave visual SOLO en la entidad Master
            if (isMaster)
            {
                smoothCharge = MathHelper.Lerp(smoothCharge, charge, 0.15f);
                smoothCooldown = MathHelper.Lerp(smoothCooldown, rockCooldown, 0.15f);
                if (rockCooldown <= 0f && smoothCooldown < 0.5f) smoothCooldown = 0f;
            }

            // --- DESCENSO LENTO DE LA BARRA AZUL (determinista, todos lo simulan; el servidor corrige) ---
            if (rockCooldown > 0f && !isTrappedInRock && !isAboutToBeTrapped)
            {
                rockCooldown -= currentCooldownDecay;
                if (rockCooldown < 0f) rockCooldown = 0f;
            }

            // --- DECAIMIENTO DE LA CARGA: solo servidor / singleplayer ---
            if (authority && charge > 0 && !isTrappedInRock && !isAboutToBeTrapped)
            {
                timeSinceLastHit++;
                if (timeSinceLastHit > currentDecayDelay)
                {
                    charge -= currentDecayRate;
                    if (charge < 0) charge = 0;
                }
            }

            // El servidor refresca a los clientes mientras haya barras activas
            if (Main.netMode == NetmodeID.Server && isMaster && (charge > 0f || rockCooldown > 0f) && Main.GameUpdateCount % 10 == 0)
            {
                npc.netUpdate = true;
            }

            if (isAboutToBeTrapped)
            {
                // La roca debe aparecer DONDE ESTÁ el enemigo ahora, no donde se llenó la barra.
                if (authority && isMaster) triggerPosition = npc.Center;

                trapTimer++;
                int spawnRate = currentRockSpawnRate > 0 ? currentRockSpawnRate : 1;

                if (trapTimer % spawnRate == 0 && trapTimer < currentCinDuration - 5)
                {
                    int numRocks = Main.rand.Next(1, 3);
                    for (int r = 0; r < numRocks; r++) SpawnHomingRock(npc);
                }

                // Solo el servidor / singleplayer transforma. Los clientes esperan la sincronización.
                if (authority && trapTimer >= currentCinDuration)
                {
                    isAboutToBeTrapped = false;
                    trapTimer = 0;
                    TransformIntoRock(npc);
                    npc.netUpdate = true;
                }
            }

            if (isTrappedInRock)
            {
                npc.hide = true;
                npc.dontTakeDamage = true;

                bool rockValid = trappedRockWhoAmI >= 0
                    && trappedRockWhoAmI < Main.maxNPCs
                    && Main.npc[trappedRockWhoAmI].active
                    && Main.npc[trappedRockWhoAmI].type == ModContent.NPCType<Roca>();

                if (!rockValid)
                {
                    // Solo el servidor / singleplayer libera; los clientes reciben el cambio por sync.
                    if (authority) ReleaseFromRock(npc);
                }
                else
                {
                    if (npc.type != NPCID.TargetDummy)
                    {
                        npc.Center = Main.npc[trappedRockWhoAmI].Center;
                    }
                }
            }
        }

        private void SpawnHomingRock(NPC npc)
        {
            Vector2 spawnPos = triggerPosition != Vector2.Zero ? triggerPosition : npc.Center;
            bool foundFloor = false;

            for (int i = 0; i < 40; i++)
            {
                int tX = (int)(spawnPos.X / 16f) + Main.rand.Next(-10, 11);
                int tY = (int)(spawnPos.Y / 16f) + i;

                if (WorldGen.InWorld(tX, tY))
                {
                    Tile tile = Main.tile[tX, tY];
                    if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                    {
                        spawnPos = new Vector2(tX * 16 + 8, tY * 16 - 8);
                        foundFloor = true;
                        break;
                    }
                }
            }

            if (!foundFloor)
            {
                spawnPos = npc.Center + new Vector2(Main.rand.Next(-200, 200), 600);
            }

            // Las rocas homing las crea solo el servidor / singleplayer (se sincronizan a los clientes)
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 randomVel = new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-12f, -6f));
                Projectile.NewProjectile(
                    npc.GetSource_FromAI(),
                    spawnPos,
                    randomVel,
                    ModContent.ProjectileType<RocaProyectil>(),
                    0,
                    0,
                    Main.myPlayer,
                    npc.whoAmI
                );
            }

            if (!Main.dedServ)
            {
                for (int i = 0; i < 6; i++)
                {
                    Dust.NewDust(spawnPos, 16, 16, DustID.Stone, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-3f, 0f));
                    Dust.NewDust(spawnPos, 16, 16, DustID.Smoke, Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, 0f));
                }
            }
        }

        private void TransformIntoRock(NPC masterNpc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            // Siempre la posición ACTUAL del enemigo
            Vector2 rockSpawnPos = masterNpc.Center;

            if (!Main.dedServ)
            {
                for (int i = 0; i < 45; i++)
                {
                    Dust d = Dust.NewDustPerfect(rockSpawnPos, DustID.YellowTorch, Main.rand.NextVector2Circular(9f, 9f), 100, default, 2.5f);
                    d.noGravity = true;
                    d.velocity *= 1.2f;
                }
            }

            List<NPC> allSegments = GetAllGroupedParts(masterNpc);

            // Daño escalado con el daño Stand y con variación natural (±15%)
            int dmg = (int)(currentTransformDamage * Main.rand.NextFloat(0.85f, 1.15f));
            if (dmg < 1) dmg = 1;

            NPC.HitInfo hit = new NPC.HitInfo
            {
                Damage = dmg,
                Knockback = 0f,
                HitDirection = 0,
                Crit = false,
                HideCombatText = false
            };
            masterNpc.StrikeNPC(hit);
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendStrikeNPC(masterNpc, hit);
            }

            if (masterNpc.active && masterNpc.life > 0)
            {
                int rockIndex = NPC.NewNPC(
                    masterNpc.GetSource_FromAI(),
                    (int)rockSpawnPos.X,
                    (int)rockSpawnPos.Y,
                    ModContent.NPCType<Roca>(),
                    ai0: masterNpc.whoAmI,
                    ai1: currentDefenseMult
                );

                // Sin hueco para la roca: cancelamos sin bloquear al enemigo
                if (rockIndex < 0 || rockIndex >= Main.maxNPCs)
                {
                    charge = 0f;
                    masterNpc.netUpdate = true;
                    return;
                }

                foreach (NPC segment in allSegments)
                {
                    if (segment.TryGetGlobalNPC(out TransformacionGlobalNPC gNPC))
                    {
                        gNPC.isTrappedInRock = true;
                        gNPC.isAboutToBeTrapped = false;
                        gNPC.trappedRockWhoAmI = rockIndex;
                        gNPC.savedVelocity = segment.velocity;
                        gNPC.currentCooldownDecay = currentCooldownDecay; // todas las partes conocen la tasa del cooldown
                        gNPC.charge = 0f;
                        segment.netUpdate = true;
                    }
                    segment.hide = true;
                    segment.dontTakeDamage = true;

                    if (segment.type != NPCID.TargetDummy)
                    {
                        segment.Center = rockSpawnPos;
                        segment.velocity = Vector2.Zero;
                    }
                }
            }
        }

        // Solo servidor / singleplayer. Los clientes reciben el resultado por ReceiveExtraAI.
        public void ReleaseFromRock(NPC masterNpc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            bool wasTrapped = isTrappedInRock;
            List<NPC> allSegments = GetAllGroupedParts(masterNpc);

            Vector2 releasePos = masterNpc.Center;
            if (trappedRockWhoAmI >= 0 && trappedRockWhoAmI < Main.maxNPCs && Main.npc[trappedRockWhoAmI].active)
            {
                releasePos = Main.npc[trappedRockWhoAmI].Center;
            }

            float cooldownDecay = currentCooldownDecay;

            foreach (NPC segment in allSegments)
            {
                if (segment.TryGetGlobalNPC(out TransformacionGlobalNPC gNPC))
                {
                    gNPC.isTrappedInRock = false;
                    gNPC.trappedRockWhoAmI = -1;
                    gNPC.charge = 0f;
                    gNPC.smoothCharge = 0f;
                    gNPC.timeSinceLastHit = 0;

                    // --- INICIA EL COOLDOWN AZUL (compartido: vive en el enemigo) ---
                    gNPC.currentCooldownDecay = cooldownDecay;
                    gNPC.rockCooldown = MAX_COOLDOWN;
                    gNPC.smoothCooldown = MAX_COOLDOWN;

                    segment.netUpdate = true;
                }

                segment.hide = false;
                segment.dontTakeDamage = false;

                if (segment.type != NPCID.TargetDummy)
                {
                    segment.Center = releasePos;
                    if (segment.TryGetGlobalNPC(out TransformacionGlobalNPC gNPC2))
                    {
                        segment.velocity = gNPC2.savedVelocity;
                    }
                }
            }

            // Efectos de romper la roca (singleplayer; el servidor dedicado los omite)
            if (wasTrapped && !Main.dedServ)
            {
                Roca.PlayBreakEffects(releasePos, 32, 32, masterNpc.GetSource_Death());
            }
        }

        // --- OCULTA POR COMPLETO EL SPRITE (Moon Lord y cualquier multiparte) ---
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (isTrappedInRock) return false;

            NPC master = GetMasterNPC(npc);
            if (master.whoAmI != npc.whoAmI && master.TryGetGlobalNPC(out TransformacionGlobalNPC masterGNPC))
            {
                if (masterGNPC.isTrappedInRock) return false;
            }

            return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            NPC master = GetMasterNPC(npc);
            if (!master.TryGetGlobalNPC(out TransformacionGlobalNPC masterGNPC)) return;

            if (masterGNPC.isTrappedInRock || isTrappedInRock) return;

            bool showCharge = masterGNPC.charge > 0;
            bool showCooldown = masterGNPC.rockCooldown > 0 || masterGNPC.smoothCooldown > 0.5f;

            if (!showCharge && !showCooldown) return;

            Vector2 barPos = new Vector2(npc.Center.X, npc.position.Y - 18f) - screenPos;

            if (showCharge && masterGNPC.isAboutToBeTrapped)
            {
                barPos.X += Main.rand.NextFloat(-3f, 3f);
                barPos.Y += Main.rand.NextFloat(-3f, 3f);
            }

            int barWidth = 40;
            int barHeight = 6;

            Rectangle bgRect = new Rectangle((int)(barPos.X - barWidth / 2f), (int)barPos.Y, barWidth, barHeight);
            Rectangle borderRect = new Rectangle(bgRect.X - 1, bgRect.Y - 1, barWidth + 2, barHeight + 2);

            float value = showCharge ? masterGNPC.smoothCharge : masterGNPC.smoothCooldown;
            float maxValue = showCharge ? MAX_CHARGE : MAX_COOLDOWN;
            float fillPercent = MathHelper.Clamp(value / maxValue, 0f, 1f);
            Rectangle fillRect = new Rectangle(bgRect.X, bgRect.Y, (int)(barWidth * fillPercent), barHeight);

            Texture2D pixel = TextureAssets.MagicPixel.Value;

            spriteBatch.Draw(pixel, borderRect, Color.Black * 0.8f);
            spriteBatch.Draw(pixel, bgRect, Color.DarkGray * 0.5f);

            Color barColor;
            if (showCharge)
                barColor = masterGNPC.isAboutToBeTrapped ? Color.White : Color.DeepPink;
            else
                barColor = Color.DeepSkyBlue; // barra azul de cooldown

            spriteBatch.Draw(pixel, fillRect, barColor);
        }
    }
}