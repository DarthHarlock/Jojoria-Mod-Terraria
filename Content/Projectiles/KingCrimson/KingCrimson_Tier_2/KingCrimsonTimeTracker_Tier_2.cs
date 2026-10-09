using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Systems;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.KingCrimson_Buffs;

namespace Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_2
{
    public class VirtualShadow
    {
        public Vector2 Position;
        public float Rotation;
        public int SpriteDirection;
        public Rectangle Frame;
        public float FadeAlpha = 1f;
        public bool FadingOut = false;
    }

    // Hereda de GlobalNPC y de nuestra nueva interfaz IKingCrimsonTimeTracker
    public class KingCrimsonTimeTracker_Tier_2 : GlobalNPC, IKingCrimsonTimeTracker
    {
        public override bool InstancePerEntity => true;

        public bool isRecording { get; private set; } = false;
        public bool isReplaying { get; private set; } = false;

        public bool HasRecordedData => recordedPath.Count > 0;

        private readonly List<Vector2> recordedPath = new();
        private readonly List<float> recordedRotations = new();
        private readonly List<int> recordedDirections = new();
        private readonly List<Rectangle> recordedFrames = new();

        private readonly List<VirtualShadow> shadowList = new();

        private Vector2 initialPosition = Vector2.Zero;
        private int replayIndex = 0;
        private int recordTimer = 0;
        private int ownerPlayer = 0;

        private bool waitingForReplay = false;
        private int replayDelay = 0;

        private int shadowLifeTimer = 0;
        private const int MAX_SHADOW_LIFE = 1800;

        private readonly float[] savedAI = new float[4];
        private readonly float[] savedLocalAI = new float[4];

        public Vector2 GetInitialPosition() => initialPosition;
        public void ForceInitialPosition(Vector2 pos) => initialPosition = pos;

        // ───────────────────────────────────────────────────────────
        // Exponemos el dueño del tracker y si sigue "ocupando"
        // la habilidad (grabando, rebobinando, esperando o con sombras).
        // ───────────────────────────────────────────────────────────
        public int OwnerPlayer => ownerPlayer;

        public bool HasActiveShadows => shadowList.Count > 0;

        // 🆕 Implementa la interfaz IKingCrimsonTimeTracker.
        // Mientras esto sea true, el TimeStop debe quedar bloqueado.
        public bool IsTimeEraseLockActive =>
            isRecording || isReplaying || waitingForReplay || HasActiveShadows;

        /// <summary>
        /// True si el jugador indicado debe tener el slot de Stand bloqueado
        /// porque su King Crimson Tier 4 (Time Erase) sigue en curso.
        /// </summary>
        public static bool IsStandLockedFor(int playerWhoAmI)
        {
            // 1) La habilidad está corriendo activamente y es de este jugador
            if (TimeEraseNetHandler.timeEraseActive
                && TimeEraseNetHandler.activeTier == 4
                && TimeEraseNetHandler.timeEraseOwner == playerWhoAmI)
                return true;

            // 2) Chequeo extra por el buff, por seguridad si el handler
            //    ya limpió su estado pero el jugador sigue con el buff
            Player p = Main.player[playerWhoAmI];
            if (p != null && p.active && p.HasBuff(ModContent.BuffType<TimeErased>()))
                return true;

            // 3) Aunque el buff/handler ya terminaron, si aún quedan NPCs
            //    grabando/rebobinando/con sombras de ESE jugador, bloqueamos
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active) continue;

                var tracker = npc.GetGlobalNPC<KingCrimsonTimeTracker_Tier_2>();
                if (tracker.IsTimeEraseLockActive && tracker.OwnerPlayer == playerWhoAmI)
                    return true;
            }

            return false;
        }

        public override void OnSpawn(NPC npc, Terraria.DataStructures.IEntitySource source)
        {
            TimeEraseNetHandler.TryRegisterNewNPC(npc);
        }

        public void IniciarGrabacion(int playerWhoAmI = 0)
        {
            isRecording = true;
            isReplaying = false;
            waitingForReplay = false;

            recordedPath.Clear();
            recordedRotations.Clear();
            recordedDirections.Clear();
            recordedFrames.Clear();

            LimpiarSombras();
            recordTimer = 0;
            replayIndex = 0;
            ownerPlayer = playerWhoAmI;
            initialPosition = Vector2.Zero;
            shadowLifeTimer = 0;
        }

        public void IniciarRebobinado(NPC npc)
        {
            if (recordedPath.Count == 0)
            {
                TimeEraseNetHandler.NotifyReplayFinished();
                return;
            }

            for (int k = 0; k < 4; k++)
            {
                savedAI[k] = npc.ai[k];
                savedLocalAI[k] = npc.localAI[k];
            }

            isRecording = false;
            isReplaying = false;
            waitingForReplay = true;
            replayDelay = 55;

            npc.Center = initialPosition;
            if (recordedRotations.Count > 0) npc.rotation = recordedRotations[0];
            if (recordedDirections.Count > 0)
            {
                npc.spriteDirection = recordedDirections[0];
                npc.direction = recordedDirections[0];
            }
            if (recordedFrames.Count > 0) npc.frame = recordedFrames[0];

            npc.velocity = Vector2.Zero;
            npc.netUpdate = true;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player p = Main.player[ownerPlayer];
                Projectile.NewProjectile(
                    npc.GetSource_FromAI(),
                    p.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<TimeErasedEffect_Tier_2>(),
                    0, 0, ownerPlayer
                );
            }
        }

        public void DetenerHabilidad(NPC npc)
        {
            isRecording = false;
            isReplaying = false;
            waitingForReplay = false;
            recordedPath.Clear();
            recordedRotations.Clear();
            recordedDirections.Clear();
            recordedFrames.Clear();
            LimpiarSombras();
        }

        public override bool PreAI(NPC npc)
        {
            // Comprobamos && TimeEraseNetHandler.activeTier == 4
            // Esto evita que el Tier 4 grabe si el que se activó fue el Tier 3 o cualquier otro.
            if (TimeEraseNetHandler.timeEraseActive && TimeEraseNetHandler.activeTier == 4 && npc.active && !npc.friendly && npc.lifeMax > 5)
            {
                if (!isRecording && !isReplaying && !waitingForReplay)
                {
                    int owner = TimeEraseNetHandler.timeEraseOwner;
                    if (owner >= 0 && owner < 255)
                    {
                        Player p = Main.player[owner];
                        if (p.active && !p.dead)
                        {
                            if (npc.Distance(p.Center) < 2200f)
                            {
                                IniciarGrabacion(owner);
                            }
                        }
                    }
                }
            }

            if (isRecording && TimeEraseNetHandler.timeEraseActive)
            {
                for (int i = shadowList.Count - 1; i >= 0; i--)
                {
                    VirtualShadow shadow = shadowList[i];
                    if (shadow.FadingOut)
                    {
                        shadow.FadeAlpha -= 0.07f;
                        if (shadow.FadeAlpha <= 0f) { shadowList.RemoveAt(i); continue; }
                    }
                }
            }
            else
            {
                for (int i = shadowList.Count - 1; i >= 0; i--)
                {
                    VirtualShadow shadow = shadowList[i];
                    if (shadow.FadingOut)
                    {
                        shadow.FadeAlpha -= 0.07f;
                        if (shadow.FadeAlpha <= 0f) { shadowList.RemoveAt(i); continue; }
                    }
                    else if (isReplaying && Vector2.Distance(npc.Center, shadow.Position) < 24f)
                    {
                        shadow.FadingOut = true;
                    }
                }
            }

            if (shadowList.Count > 0 || isRecording || isReplaying || waitingForReplay)
            {
                shadowLifeTimer++;
                if (shadowLifeTimer >= MAX_SHADOW_LIFE)
                {
                    FadeOutTodasLasSombras();
                    isRecording = false;
                    isReplaying = false;
                    waitingForReplay = false;
                    recordedPath.Clear();
                    recordedRotations.Clear();
                    recordedDirections.Clear();
                    recordedFrames.Clear();
                    return true;
                }
            }

            if (waitingForReplay)
            {
                npc.Center = initialPosition;
                npc.velocity = Vector2.Zero;
                replayDelay--;
                if (replayDelay <= 0)
                    ComenzarReplay(npc);
                return false;
            }

            if (isReplaying)
            {
                if (replayIndex < recordedPath.Count)
                {
                    npc.Center = recordedPath[replayIndex];
                    npc.rotation = recordedRotations[replayIndex];
                    npc.spriteDirection = recordedDirections[replayIndex];
                    npc.direction = recordedDirections[replayIndex];
                    npc.frame = recordedFrames[replayIndex];
                    npc.velocity = Vector2.Zero;
                    replayIndex++;

                    if (replayIndex >= recordedPath.Count)
                        FinalizarReplay(npc);
                }
                else
                {
                    FinalizarReplay(npc);
                }
                return false;
            }

            return true;
        }

        public override void PostAI(NPC npc)
        {
            if (!isRecording) return;

            if (recordedPath.Count == 0)
                initialPosition = npc.Center;

            recordedPath.Add(npc.Center);
            recordedRotations.Add(npc.rotation);
            recordedDirections.Add(npc.spriteDirection);
            recordedFrames.Add(npc.frame);

            recordTimer++;
            if (recordTimer % 9 == 0)
            {
                shadowList.Add(new VirtualShadow
                {
                    Position = npc.Center,
                    Rotation = npc.rotation,
                    SpriteDirection = npc.spriteDirection,
                    Frame = npc.frame,
                    FadeAlpha = 1f,
                    FadingOut = false
                });
            }
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            if (isReplaying && replayIndex > 0 && replayIndex <= recordedFrames.Count)
            {
                npc.frame = recordedFrames[replayIndex - 1];
                npc.spriteDirection = recordedDirections[replayIndex - 1];
            }
            else if (waitingForReplay && recordedFrames.Count > 0)
            {
                npc.frame = recordedFrames[0];
                npc.spriteDirection = recordedDirections[0];
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (shadowList.Count == 0) return true;

            Main.instance.LoadNPC(npc.type);
            Texture2D texture = Terraria.GameContent.TextureAssets.Npc[npc.type].Value;

            foreach (VirtualShadow shadow in shadowList)
            {
                if (shadow.FadeAlpha <= 0f) continue;

                Vector2 origin = shadow.Frame.Size() / 2f;
                SpriteEffects effects = shadow.SpriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Color colorSombra = new Color(119, 21, 55, 80) * 2.6f * shadow.FadeAlpha;

                spriteBatch.Draw(texture, shadow.Position - screenPos,
                    shadow.Frame, colorSombra, shadow.Rotation, origin, npc.scale, effects, 0f);
            }
            return true;
        }

        public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
            => waitingForReplay ? false : null;

        public override bool? CanBeHitByItem(NPC npc, Player player, Item item)
            => waitingForReplay ? false : null;

        public override void OnKill(NPC npc)
        {
            if (isReplaying || waitingForReplay)
                TimeEraseNetHandler.NotifyReplayFinished();

            FadeOutTodasLasSombras();
            isRecording = false;
            isReplaying = false;
            waitingForReplay = false;
            recordedPath.Clear();
            recordedRotations.Clear();
            recordedDirections.Clear();
            recordedFrames.Clear();
        }

        private void ComenzarReplay(NPC npc)
        {
            waitingForReplay = false;
            isReplaying = true;
            replayIndex = 0;

            for (int k = 0; k < 4; k++)
            {
                npc.ai[k] = savedAI[k];
                npc.localAI[k] = savedLocalAI[k];
            }

            npc.Center = initialPosition;
            if (recordedRotations.Count > 0) npc.rotation = recordedRotations[0];
            if (recordedDirections.Count > 0)
            {
                npc.spriteDirection = recordedDirections[0];
                npc.direction = recordedDirections[0];
            }
            if (recordedFrames.Count > 0) npc.frame = recordedFrames[0];

            npc.velocity = Vector2.Zero;
            npc.netUpdate = true;
        }

        private void FinalizarReplay(NPC npc)
        {
            isReplaying = false;
            waitingForReplay = false;
            isRecording = false;

            recordedPath.Clear();
            recordedRotations.Clear();
            recordedDirections.Clear();
            recordedFrames.Clear();

            for (int k = 0; k < 4; k++)
            {
                npc.ai[k] = savedAI[k];
                npc.localAI[k] = savedLocalAI[k];
            }
            npc.netUpdate = true;

            FadeOutTodasLasSombras();
            TimeEraseNetHandler.NotifyReplayFinished();
        }

        private void FadeOutTodasLasSombras()
        {
            foreach (VirtualShadow shadow in shadowList)
                shadow.FadingOut = true;
            shadowLifeTimer = 0;
        }

        private void LimpiarSombras()
        {
            shadowList.Clear();
            shadowLifeTimer = 0;
        }
    }
}