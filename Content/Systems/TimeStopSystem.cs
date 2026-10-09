using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using System.Collections.Generic;
using Jojo.Content.Buffs;
using Jojo.Content.Projectiles.TheWorld.TheWorld_Tier_1;
using Jojo.Content.Projectiles.TheWorld.TheWorld_Tier_2;
using Jojo.Content.Projectiles.TheWorld.TheWorld_Tier_3;
using Jojo.Content.Projectiles.TheWorld.TheWorld_Tier_4;
using Jojo.Content.Projectiles.StarPlatinum.StarPlatinum_Tier_3;
using Jojo.Content.Projectiles.StarPlatinum.StarPlatinum_Tier_4;
using Jojo.Content.Players;
using Jojo.Content.NPCs.StandsNpc;

namespace Jojo.Content.Systems
{
    public class TimeStopSystem : ModSystem
    {
        public static bool timeStopped = false;
        public static int owner = -1;
        public static int ownerNPC = -1;
        public static int ownerProj = -1;
        public static string endSoundPath = "";
        private static bool lastTimeStopped = false;

        private static bool clockActive = false;
        private static int clockTimer = 0;
        private static int startDelay = 0;

        private static float storedMusicVolume = 1f;

        private const int START_SOUND_DURATION = 180;
        private const int CLOCK_DURATION = 120;

        private static readonly Dictionary<int, NPCState> npcStates = new();
        private static readonly Dictionary<int, PlayerVisualState> playerStates = new();

        private static readonly Dictionary<int, int> accumulatedDamage = new();

        private struct NPCState
        {
            public Vector2 position;
            public Vector2 velocity;
            public float rotation;
            public short frameX;
            public short frameY;
            public bool initialized;
            public bool originalDontTakeDamage;
        }

        private struct PlayerVisualState
        {
            public Rectangle bodyFrame;
            public Rectangle legFrame;
            public Rectangle headFrame;
            public int wingFrame;
            public int wings;
            public float wingTime;
            public int wingFrameCounter;
            public int direction;
            public int itemAnimation;
            public int itemTime;
            public float itemRotation;
            public Vector2 itemLocation;
        }

        public static void ResetTimeStop()
        {
            timeStopped = false;

            clockActive = false;
            clockTimer = 0;
            startDelay = 0;
            Main.musicVolume = storedMusicVolume;

            ApplyAccumulatedDamage();

            foreach (var pair in npcStates)
            {
                // Si es el NPC dueño, jamás restaurar su velocidad con un estado viejo
                if (ownerNPC >= 0 && pair.Key == ownerNPC) continue;

                NPC npc = Main.npc[pair.Key];
                if (npc != null && npc.active)
                {
                    npc.velocity = pair.Value.velocity;
                    npc.dontTakeDamage = pair.Value.originalDontTakeDamage;
                }
            }

            owner = -1;
            ownerNPC = -1;
            ownerProj = -1;

            npcStates.Clear();
            playerStates.Clear();
        }

        private static void ApplyAccumulatedDamage()
        {
            if (accumulatedDamage.Count == 0) return;

            Player attacker = (owner >= 0 && owner < Main.maxPlayers) ? Main.player[owner] : null;

            foreach (var kv in accumulatedDamage)
            {
                if (kv.Value <= 0) continue;

                NPC npc = Main.npc[kv.Key];
                if (npc == null || !npc.active) continue;

                int hitDirection = 1;
                if (attacker != null && attacker.active)
                    hitDirection = npc.Center.X >= attacker.Center.X ? 1 : -1;

                npc.SimpleStrikeNPC(kv.Value, hitDirection, false, 20f, null, false, float.NaN, false);
            }
            accumulatedDamage.Clear();
        }

        public override void PostUpdateEverything()
        {
            if (timeStopped && !lastTimeStopped)
            {
                storedMusicVolume = Main.musicVolume;
                startDelay = START_SOUND_DURATION;
                clockActive = false;
                clockTimer = 0;
            }

            if (timeStopped && startDelay > 0)
            {
                startDelay--;
                if (startDelay == 0)
                {
                    clockActive = true;
                    clockTimer = 0;
                }
            }

            if (timeStopped && clockActive)
            {
                if (clockTimer <= 0)
                {
                    SoundEngine.PlaySound(new SoundStyle("Jojo/Content/Sonidos/Clock"));
                    clockTimer = CLOCK_DURATION;
                }
                else clockTimer--;
            }

            if (!timeStopped && lastTimeStopped)
            {
                if (!string.IsNullOrEmpty(endSoundPath))
                    SoundEngine.PlaySound(new SoundStyle(endSoundPath));

                ResetTimeStop();
            }

            lastTimeStopped = timeStopped;

            if (timeStopped) Main.musicVolume = 0f;
            if (!timeStopped) return;

            // Validación del Jugador
            if (owner >= 0 && owner < Main.maxPlayers)
            {
                Player p = Main.player[owner];
                if (!p.active || p.dead || !p.HasBuff(ModContent.BuffType<TimeStoped>()) || TimeEraseNetHandler.IsTimeManipulationActive())
                {
                    if (Main.netMode == NetmodeID.MultiplayerClient && owner == Main.myPlayer)
                    {
                        ModPacket packet = ModContent.GetInstance<Jojo>().GetPacket();
                        packet.Write(Jojo.PacketType_TimeStopSync);
                        packet.Write(false);
                        packet.Write(-1);
                        packet.Write(-1);
                        packet.Write(-1);
                        packet.Write(endSoundPath);
                        packet.Send();
                    }

                    ResetTimeStop();
                }
            }
            // Validación del NPC
            else if (ownerNPC >= 0 && ownerNPC < Main.maxNPCs)
            {
                NPC npc = Main.npc[ownerNPC];
                if (!npc.active)
                {
                    ResetTimeStop();
                }
            }
        }

        public class GlobalNPCFreeze : GlobalNPC
        {
            public override bool PreAI(NPC npc)
            {
                if (!timeStopped || !npc.active) return base.PreAI(npc);

                // El NPC dueño del Time Stop NO SE CONGELA
                if (ownerNPC >= 0 && npc.whoAmI == ownerNPC)
                {
                    if (npcStates.ContainsKey(npc.whoAmI))
                        npcStates.Remove(npc.whoAmI);
                    return base.PreAI(npc);
                }

                if (!npcStates.ContainsKey(npc.whoAmI))
                {
                    npcStates[npc.whoAmI] = new NPCState
                    {
                        position = npc.position,
                        velocity = npc.velocity,
                        rotation = npc.rotation,
                        frameX = (short)npc.frame.X,
                        frameY = (short)npc.frame.Y,
                        initialized = true,
                        originalDontTakeDamage = npc.dontTakeDamage
                    };
                }

                npc.dontTakeDamage = true;

                if (owner >= 0 && owner < Main.maxPlayers && npc.immune[owner] <= 0)
                {
                    foreach (Projectile proj in Main.projectile)
                    {
                        if (!proj.active || proj.owner != owner || !proj.friendly) continue;
                        if (proj.ModProjectile is IStandNpcProjectile) continue;
                        if (proj.damage <= 0) continue;
                        if (!proj.Hitbox.Intersects(npc.Hitbox)) continue;

                        int dmg = System.Math.Max(proj.damage - npc.defense, proj.damage / 10);
                        if (dmg > 0)
                        {
                            accumulatedDamage.TryGetValue(npc.whoAmI, out int current);
                            accumulatedDamage[npc.whoAmI] = current + dmg;
                        }

                        Player attacker = Main.player[owner];
                        float standSpeed = attacker.GetModPlayer<StandStatsPlayer>().standSpeed;
                        float pitch = standSpeed >= 100f ? 0.05f : standSpeed >= 50f ? 0.08f : 0f;

                        SoundStyle hitSound = npc.HitSound ?? SoundID.NPCHit1;
                        SoundEngine.PlaySound(hitSound with { Pitch = pitch }, npc.Center);

                        npc.immune[owner] = proj.localNPCHitCooldown > 0 ? proj.localNPCHitCooldown : 10;
                        break;
                    }
                }

                npc.velocity = Vector2.Zero;
                npc.oldVelocity = Vector2.Zero;
                npc.frameCounter = 0;
                return false;
            }

            public override void PostAI(NPC npc)
            {
                if (!timeStopped || !npc.active) return;
                if (ownerNPC >= 0 && npc.whoAmI == ownerNPC) return;

                if (npcStates.TryGetValue(npc.whoAmI, out NPCState s))
                {
                    npc.position = s.position;
                    npc.velocity = Vector2.Zero;
                    npc.rotation = s.rotation;
                    npc.frame.X = s.frameX;
                    npc.frame.Y = s.frameY;
                }
            }

            public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
            {
                if (timeStopped) return true;
                return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);
            }
        }

        public class GlobalProjectileFreeze : GlobalProjectile
        {
            public override bool InstancePerEntity => true;

            bool initialized = false;
            Vector2 storedVelocity, currentVelocity;
            float storedRotation;
            int initDelay = 0, storedTimeLeft;

            public override bool PreAI(Projectile p)
            {
                if (!timeStopped || !p.active) return base.PreAI(p);

                // 1. Si es el Stand del NPC Invocador, NO SE CONGELA
                if (ownerProj >= 0 && p.whoAmI == ownerProj)
                    return base.PreAI(p);

                if (p.ModProjectile is IStandNpcProjectile standNpc)
                {
                    if (ownerNPC >= 0 && standNpc.OwnerNpc != null && standNpc.OwnerNpc.whoAmI == ownerNPC)
                        return base.PreAI(p);

                    p.velocity = Vector2.Zero;
                    return false;
                }

                // 2. Excepciones para Stands/Ataques del jugador invocador
                if (owner >= 0 && p.owner == owner)
                {
                    if (p.ModProjectile is THEWORLDSTAND_Tier_1 || p.ModProjectile is THEWORLDSTAND_Tier_2 || p.ModProjectile is THEWORLDSTAND_Tier_3 || p.ModProjectile is THEWORLDSTAND_Tier_4) return base.PreAI(p);
                    if (p.ModProjectile is TW_BarragePunch_Tier_1 || p.ModProjectile is TW_BarragePunch_Tier_2 || p.ModProjectile is TW_BarragePunch_Tier_3 || p.ModProjectile is TW_BarragePunch_Tier_4) return base.PreAI(p);
                    if (p.ModProjectile is STARPLATINUMSTAND_Tier_3 || p.ModProjectile is STARPLATINUMSTAND_Tier_4) return base.PreAI(p);
                    if (p.ModProjectile is SP_BarragePunch_Tier_3 || p.ModProjectile is SP_BarragePunch_Tier_4) return base.PreAI(p);
                    if (ProjectileID.Sets.IsAWhip[p.type]) return base.PreAI(p);
                    if (p.minion || p.sentry) return base.PreAI(p);
                }

                if ((owner >= 0 && p.owner != owner) || (ownerNPC >= 0 && p.whoAmI != ownerProj))
                {
                    if (!initialized)
                    {
                        storedTimeLeft = p.timeLeft;
                        initialized = true;
                    }
                    p.timeLeft = storedTimeLeft;
                    p.velocity = Vector2.Zero;
                    p.oldVelocity = Vector2.Zero;
                    return false;
                }

                if (initDelay < 6)
                {
                    initDelay++;
                    return base.PreAI(p);
                }

                if (!initialized)
                {
                    storedVelocity = p.velocity;
                    currentVelocity = p.velocity;
                    storedRotation = p.rotation;
                    storedTimeLeft = p.timeLeft;
                    initialized = true;
                }

                p.timeLeft = storedTimeLeft;
                currentVelocity *= 0.85f;
                if (currentVelocity.Length() < 0.1f) currentVelocity = Vector2.Zero;

                p.position += currentVelocity;
                p.velocity = Vector2.Zero;
                p.oldVelocity = Vector2.Zero;
                p.rotation = storedRotation;

                return false;
            }

            public override void PostAI(Projectile p)
            {
                if (!timeStopped && initialized)
                {
                    p.velocity = storedVelocity;
                    if (storedVelocity != Vector2.Zero) p.rotation = storedVelocity.ToRotation();
                    initialized = false;
                    initDelay = 0;
                }
            }
        }

        public class GlobalItemFreeze : GlobalItem
        {
            public override bool InstancePerEntity => true;

            public override void PostUpdate(Item item)
            {
                if (!timeStopped || !item.active) return;
                item.velocity = Vector2.Zero;
            }
        }

        public class GlobalPlayerFreeze : ModPlayer
        {
            public override void SetControls()
            {
                if (timeStopped && Player.whoAmI != owner)
                {
                    Player.controlUseItem = false;
                    Player.controlUseTile = false;
                    Player.controlLeft = false;
                    Player.controlRight = false;
                    Player.controlUp = false;
                    Player.controlDown = false;
                    Player.controlJump = false;
                    Player.controlHook = false;
                    Player.controlMount = false;
                    Player.controlSmart = false;
                }
            }

            public override void PreUpdate()
            {
                if (!timeStopped || (owner >= 0 && Player.whoAmI == owner))
                {
                    if (playerStates.ContainsKey(Player.whoAmI))
                        playerStates.Remove(Player.whoAmI);
                    return;
                }

                if (!playerStates.ContainsKey(Player.whoAmI))
                {
                    playerStates[Player.whoAmI] = new PlayerVisualState
                    {
                        bodyFrame = Player.bodyFrame,
                        legFrame = Player.legFrame,
                        headFrame = Player.headFrame,
                        wingFrame = (int)Player.wingFrame,
                        wings = Player.wings,
                        wingTime = Player.wingTime,
                        wingFrameCounter = Player.wingFrameCounter,
                        direction = Player.direction,
                        itemAnimation = Player.itemAnimation,
                        itemTime = Player.itemTime,
                        itemRotation = Player.itemRotation,
                        itemLocation = Player.itemLocation
                    };
                }

                Player.velocity = Vector2.Zero;
                Player.oldVelocity = Vector2.Zero;
                Player.gravity = 0f;
                Player.maxFallSpeed = 0f;
                Player.position = Player.oldPosition;
            }

            public override void PostUpdate()
            {
                if (!timeStopped || Player.whoAmI == owner) return;

                Player.velocity = Vector2.Zero;

                if (playerStates.TryGetValue(Player.whoAmI, out PlayerVisualState s))
                {
                    Player.bodyFrame = s.bodyFrame;
                    Player.legFrame = s.legFrame;
                    Player.headFrame = s.headFrame;
                    Player.direction = s.direction;
                    Player.itemAnimation = s.itemAnimation;
                    Player.itemTime = s.itemTime;
                    Player.itemRotation = s.itemRotation;
                    Player.itemLocation = s.itemLocation;

                    if (s.wings > 0)
                    {
                        Player.wings = s.wings;
                        Player.wingFrame = s.wingFrame;
                        Player.wingTime = s.wingTime > 0 ? s.wingTime : 1f;
                        Player.wingFrameCounter = s.wingFrameCounter;
                    }
                }
            }
        }
    }
}