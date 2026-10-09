using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Audio;
using System.IO;
using Jojo.Content.Items; // Necesario para detectar NpcKillerPlayer
using Jojo.Content.NPCs.TownNPCs;
using Jojo.Content.NPCs.StandsNpc.StarPlatinum;
using Jojo.Content.NPCs.StandsNpc.TheWorld;
using Jojo.Content.NPCs.StandsNpc.Cinderella;
using Jojo.Content.NPCs.StandsNpc.SilverChariot;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_3.LluviaDeRanas_Tier_3;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_4.LluviaDeRanas_Tier_4;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_2.Rana_Tier_2;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_3.Rana_Tier_3;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_4.Rana_Tier_4;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_3.Mariposas_Tier_3;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_4.Mariposas_Tier_4;

namespace Jojo.Content.NPCs.StandsNpc
{
    public class GlobalStandSpawner : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool initializedStand;
        public bool hasStand;
        public int standType = -1;

        public Entity currentAggressor;
        public int standRespawnCooldown;
        public bool forceSpawn;

        public int currentStandWhoAmI = -1;

        private const float KillerDetectionRadius = 380f;
        private static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");

        // Cinderella NO está en esta lista: es exclusiva de la Estilista y no sale al azar.
        public static readonly int[] AllStandTypes = new int[]
        {
            ModContent.ProjectileType<StarPlatinumNpcProj>(),
            ModContent.ProjectileType<TheWorldNpcProj>(),
            ModContent.ProjectileType<SilverChariotNpcProj>()
        };

        public static int GetRandomStandType()
        {
            return AllStandTypes[Main.rand.Next(AllStandTypes.Length)];
        }

        // ---------- Identificación de NPCs con stand fijo ----------

        public static bool IsJotaro(NPC npc)
        {
            return npc.type == ModContent.NPCType<Jotaro>();
        }

        // Solo la Estilista vanilla (ModNPC == null) y nunca Jotaro.
        public static bool IsStylist(NPC npc)
        {
            if (IsJotaro(npc)) return false;
            return npc.ModNPC == null && npc.type == NPCID.Stylist;
        }

        public static bool IsCinderellaType(int type)
        {
            return type == ModContent.ProjectileType<CinderellaNpcProj>();
        }

        public static bool IsSameEntityGroup(NPC a, NPC b)
        {
            if (a.whoAmI == b.whoAmI) return true;

            if (a.realLife >= 0 && b.realLife >= 0 && a.realLife == b.realLife) return true;
            if (a.realLife >= 0 && a.realLife == b.whoAmI) return true;
            if (b.realLife >= 0 && b.realLife == a.whoAmI) return true;

            int groupA = GetVanillaBossGroup(a.type);
            int groupB = GetVanillaBossGroup(b.type);

            if (groupA != -1 && groupA == groupB) return true;

            return false;
        }

        private static int GetVanillaBossGroup(int type)
        {
            if (type == NPCID.EaterofWorldsHead || type == NPCID.EaterofWorldsBody || type == NPCID.EaterofWorldsTail) return 1;
            if (type == NPCID.SkeletronHead || type == NPCID.SkeletronHand) return 2;
            if (type == NPCID.SkeletronPrime || type == NPCID.PrimeCannon || type == NPCID.PrimeLaser || type == NPCID.PrimeSaw || type == NPCID.PrimeVice) return 3;
            if (type == NPCID.Plantera || type == NPCID.PlanterasHook || type == NPCID.PlanterasTentacle) return 4;
            if (type == NPCID.Golem || type == NPCID.GolemHead || type == NPCID.GolemFistLeft || type == NPCID.GolemFistRight || type == NPCID.GolemHeadFree) return 5;
            if (type == NPCID.Retinazer || type == NPCID.Spazmatism) return 6;
            if (type == NPCID.MoonLordCore || type == NPCID.MoonLordHand || type == NPCID.MoonLordHead || type == NPCID.MoonLordFreeEye || type == NPCID.MoonLordLeechBlob) return 7;
            if (type == NPCID.MartianSaucer || type == NPCID.MartianSaucerCannon || type == NPCID.MartianSaucerCore || type == NPCID.MartianSaucerTurret) return 8;
            if (type == NPCID.BrainofCthulhu || type == NPCID.Creeper) return 9;
            if (type == NPCID.WallofFlesh || type == NPCID.WallofFleshEye || type == NPCID.TheHungry || type == NPCID.TheHungryII || type == NPCID.LeechHead || type == NPCID.LeechBody || type == NPCID.LeechTail) return 10;

            return -1;
        }

        public static NPC FindLivingGroupMemberWithStand(NPC npc)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                if (other.active && other.whoAmI != npc.whoAmI && IsSameEntityGroup(npc, other))
                {
                    var gNPC = other.GetGlobalNPC<GlobalStandSpawner>();
                    if (gNPC.hasStand)
                    {
                        return other;
                    }
                }
            }
            return null;
        }

        private static void TransferStandToAnotherMember(NPC dyingNpc, int previousStandType)
        {
            if (previousStandType == -1) return;
            // Cinderella jamás se transfiere a nadie.
            if (IsCinderellaType(previousStandType)) return;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                if (other.active && other.whoAmI != dyingNpc.whoAmI && IsSameEntityGroup(dyingNpc, other))
                {
                    var gNPC = other.GetGlobalNPC<GlobalStandSpawner>();
                    if (!gNPC.hasStand)
                    {
                        gNPC.hasStand = true;
                        gNPC.standType = previousStandType;
                        gNPC.standRespawnCooldown = 20;
                        other.netUpdate = true;
                        break;
                    }
                }
            }
        }

        public static bool CanHaveStand(NPC npc)
        {
            // Vanilla y Dummies
            if (npc.type == NPCID.TargetDummy) return false;
            if (npc.type == NPCID.Snail || npc.type == NPCID.GlowingSnail || npc.type == NPCID.MagmaSnail) return false;

            // Ranas y Mariposas
            if (npc.type == ModContent.NPCType<RanaVenenosa_Tier_3>() || npc.type == ModContent.NPCType<RanaVenenosa_Tier_4>()) return false;
            if (npc.type == ModContent.NPCType<Rana_Refleja_Tier_2>() || npc.type == ModContent.NPCType<Rana_Refleja_Tier_3>() || npc.type == ModContent.NPCType<Rana_Refleja_Tier_4>()) return false;
            if (npc.type == ModContent.NPCType<Mariposa_Refleja_Tier_3>() || npc.type == ModContent.NPCType<Mariposa_Refleja_Tier_4>()) return false;

            // ========================================================
            // EXCLUSIÓN DINÁMICA DE DINOSAURIOS Y MINIONS (SCARY MONSTERS)
            // ========================================================
            if (npc.ModNPC != null)
            {
                string npcNamespace = npc.ModNPC.GetType().Namespace ?? "";
                string npcName = npc.ModNPC.GetType().Name;

                if (npcNamespace.Contains("ScaryMonsters") || npcName.Contains("Dino") || npcName.Contains("Dinosaurio"))
                {
                    return false;
                }
            }

            return true;
        }

        public override void OnSpawn(NPC npc, Terraria.DataStructures.IEntitySource source) { }

        private void InitializeStand(NPC npc)
        {
            // Jotaro SIEMPRE Star Platinum
            if (IsJotaro(npc))
            {
                hasStand = true;
                standType = ModContent.ProjectileType<StarPlatinumNpcProj>();
                return;
            }

            // La Estilista SIEMPRE Cinderella
            if (IsStylist(npc))
            {
                hasStand = true;
                standType = ModContent.ProjectileType<CinderellaNpcProj>();
                return;
            }

            if (!CanHaveStand(npc)) return;

            NPC existingHost = FindLivingGroupMemberWithStand(npc);
            if (existingHost != null)
            {
                hasStand = false;
                standType = -1;
                return;
            }

            if (Main.rand.NextFloat() <= 0.01f) // 1% de probabilidad
            {
                hasStand = true;
                standType = GetRandomStandType();
            }
        }

        /// <summary>
        /// Fuerza los stands fijos y elimina Cinderella de cualquiera que no sea la Estilista.
        /// Se ejecuta cada tick en el servidor / singleplayer, así corrige mundos ya guardados.
        /// </summary>
        private void EnforceFixedStands(NPC npc)
        {
            int starPlatinumType = ModContent.ProjectileType<StarPlatinumNpcProj>();
            int cinderellaType = ModContent.ProjectileType<CinderellaNpcProj>();

            if (IsJotaro(npc))
            {
                if (!hasStand || standType != starPlatinumType)
                {
                    KillStandProjectilesOf(npc, cinderellaType);
                    hasStand = true;
                    standType = starPlatinumType;
                    initializedStand = true;
                    currentStandWhoAmI = -1;
                    npc.netUpdate = true;
                }
                return;
            }

            if (IsStylist(npc))
            {
                if (!hasStand || standType != cinderellaType)
                {
                    hasStand = true;
                    standType = cinderellaType;
                    initializedStand = true;
                    currentStandWhoAmI = -1;
                    npc.netUpdate = true;
                }
                return;
            }

            // Cualquier otro NPC: prohibido tener Cinderella
            if (standType == cinderellaType)
            {
                KillStandProjectilesOf(npc, cinderellaType);
                hasStand = false;
                standType = -1;
                currentStandWhoAmI = -1;
                npc.netUpdate = true;
            }
        }

        private static void KillStandProjectilesOf(NPC npc, int projType)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.type == projType && (int)proj.ai[0] == npc.whoAmI)
                {
                    proj.Kill();
                }
            }
        }

        public override void OnKill(NPC npc)
        {
            if (hasStand)
            {
                TransferStandToAnotherMember(npc, standType);
                hasStand = false;
                standType = -1;
            }
        }

        public override void SaveData(NPC npc, TagCompound tag)
        {
            tag["jojo_hasStand"] = hasStand;
            tag["jojo_standType"] = standType;
            tag["jojo_initialized"] = initializedStand;
        }

        public override void LoadData(NPC npc, TagCompound tag)
        {
            hasStand = tag.GetBool("jojo_hasStand");
            if (tag.ContainsKey("jojo_standType")) standType = tag.GetInt("jojo_standType");

            initializedStand = tag.GetBool("jojo_initialized");
            if (hasStand) initializedStand = true;
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
        {
            writer.Write(hasStand);
            writer.Write(standType);
            writer.Write(standRespawnCooldown);
            writer.Write(forceSpawn);

            if (currentAggressor is Player p && p.active)
            {
                writer.Write((byte)1);
                writer.Write((short)p.whoAmI);
            }
            else if (currentAggressor is NPC n && n.active)
            {
                writer.Write((byte)2);
                writer.Write((short)n.whoAmI);
            }
            else
            {
                writer.Write((byte)0);
            }
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
        {
            hasStand = reader.ReadBoolean();
            standType = reader.ReadInt32();
            standRespawnCooldown = reader.ReadInt32();
            forceSpawn = reader.ReadBoolean();

            if (hasStand)
            {
                initializedStand = true;
            }

            byte targetType = reader.ReadByte();
            if (targetType == 1)
            {
                short id = reader.ReadInt16();
                currentAggressor = (id >= 0 && id < Main.maxPlayers) ? Main.player[id] : null;
            }
            else if (targetType == 2)
            {
                short id = reader.ReadInt16();
                currentAggressor = (id >= 0 && id < Main.maxNPCs) ? Main.npc[id] : null;
            }
            else
            {
                currentAggressor = null;
            }
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            currentAggressor = player;
            npc.netUpdate = true;
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            Entity prevAggressor = currentAggressor;

            if (projectile.npcProj && projectile.owner >= 0 && projectile.owner < Main.maxNPCs)
                currentAggressor = Main.npc[projectile.owner];
            else if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
                currentAggressor = Main.player[projectile.owner];

            if (prevAggressor != currentAggressor)
                npc.netUpdate = true;
        }

        public override void PostAI(NPC npc)
        {
            if (!initializedStand)
            {
                initializedStand = true;
                InitializeStand(npc);
            }

            // Jotaro = Star Platinum, Estilista = Cinderella, nadie más = Cinderella
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                EnforceFixedStands(npc);
            }

            if (!hasStand) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            int tipoDeStandInvocado = standType != -1 ? standType : ModContent.ProjectileType<StarPlatinumNpcProj>();
            float killerDetectionRadiusSq = KillerDetectionRadius * KillerDetectionRadius;

            // 1) DETECCION INSTANTANEA DE JUGADORES CON EL ITEM EQUIPADO
            if (npc.townNPC)
            {
                foreach (Player p in Main.player)
                {
                    if (p.active && !p.dead && p.GetModPlayer<NpcKillerPlayer>().canKillTownNPCs)
                    {
                        if (npc.DistanceSQ(p.Center) < killerDetectionRadiusSq)
                        {
                            if (currentAggressor != p)
                            {
                                currentAggressor = p;
                                npc.netUpdate = true;
                            }
                            break;
                        }
                    }
                }
            }

            // 2) LIMPIEZA DEL AGRESOR SI DESEQUIPA EL ITEM O SE ALEJA
            if (currentAggressor != null)
            {
                bool agresorInvalido = !currentAggressor.active;

                if (currentAggressor is Player p)
                {
                    bool tieneItem = p.GetModPlayer<NpcKillerPlayer>().canKillTownNPCs;
                    float distSq = npc.DistanceSQ(p.Center);

                    if (p.dead || distSq > 800f * 800f)
                    {
                        agresorInvalido = true;
                    }
                    else if (npc.townNPC && !tieneItem)
                    {
                        agresorInvalido = true;
                    }
                }
                else if (currentAggressor is NPC nMuerto && (!nMuerto.active || nMuerto.life <= 0))
                {
                    agresorInvalido = true;
                }

                if (agresorInvalido)
                {
                    currentAggressor = null;
                    npc.netUpdate = true;
                }
            }

            if (standRespawnCooldown > 0)
            {
                standRespawnCooldown--;
                return;
            }

            bool standActivo = false;
            if (currentStandWhoAmI != -1 && Main.projectile[currentStandWhoAmI].active)
            {
                Projectile p = Main.projectile[currentStandWhoAmI];
                if (p.type == tipoDeStandInvocado && (int)p.ai[0] == npc.whoAmI)
                {
                    standActivo = true;
                }
                else
                {
                    currentStandWhoAmI = -1;
                }
            }

            if (!standActivo)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active && proj.type == tipoDeStandInvocado && (int)proj.ai[0] == npc.whoAmI)
                    {
                        standActivo = true;
                        currentStandWhoAmI = i;
                        break;
                    }
                }
            }

            bool deberiaInvocar = forceSpawn || HayObjetivosCerca(npc, tipoDeStandInvocado, 380f);

            if (!standActivo && deberiaInvocar)
            {
                int nuevoStandIndex = Projectile.NewProjectile(
                    npc.GetSource_FromAI(),
                    npc.Center,
                    Vector2.Zero,
                    tipoDeStandInvocado,
                    0,          // El daño real lo calcula cada stand en su AI con GetAdjustedDamage()
                    5f,
                    Main.myPlayer,
                    ai0: npc.whoAmI
                );

                if (nuevoStandIndex >= 0 && nuevoStandIndex < Main.maxProjectiles)
                {
                    currentStandWhoAmI = nuevoStandIndex;
                    Projectile proj = Main.projectile[nuevoStandIndex];
                    proj.alpha = 255;

                    if (Main.netMode != NetmodeID.Server)
                    {
                        SoundEngine.PlaySound(SpawnSound, npc.Center);
                    }
                }
            }

            if (forceSpawn)
            {
                forceSpawn = false;
                npc.netUpdate = true;
            }
        }

        private bool HayObjetivosCerca(NPC owner, int tipoDeStandInvocado, float radius)
        {
            float radiusSq = radius * radius;
            bool isFriendly = owner.friendly || owner.townNPC;

            if (isFriendly)
            {
                // 1. Enemigos hostiles
                foreach (NPC n in Main.npc)
                {
                    if (n.active && !n.friendly && !n.townNPC && n.damage > 0 && n.lifeMax > 5 && owner.DistanceSQ(n.Center) < radiusSq)
                        return true;
                }

                // 2. Jugadores con el ítem NpcKiller equipado
                foreach (Player p in Main.player)
                {
                    if (p.active && !p.dead && p.GetModPlayer<NpcKillerPlayer>().canKillTownNPCs && owner.DistanceSQ(p.Center) < radiusSq)
                        return true;
                }

                // 3. Agresor activo
                if (currentAggressor != null && currentAggressor.active)
                {
                    if (currentAggressor is Player pAggro && !pAggro.dead && owner.DistanceSQ(pAggro.Center) < radiusSq)
                        return true;
                    if (currentAggressor is NPC nAggro && nAggro.active && owner.DistanceSQ(nAggro.Center) < radiusSq)
                        return true;
                }

                return false;
            }
            else
            {
                foreach (Player p in Main.player)
                {
                    if (p.active && !p.dead && owner.DistanceSQ(p.Center) < radiusSq) return true;
                }
                foreach (NPC n in Main.npc)
                {
                    if (n.active && (n.friendly || n.townNPC) && owner.DistanceSQ(n.Center) < radiusSq) return true;
                }
                return false;
            }
        }
    }
}