using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Jojo.Content.Projectiles.D4C.D4C_Tier_4;

namespace Jojo.Content.Projectiles.D4C.D4C_Tier_4.HabilidadH
{
    public class D4CGhostWorldSystem_Tier_4 : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        private static readonly Dictionary<int, Vector2> decoyPositions = new();

        public static void SetDecoy(int playerIndex, Vector2 position) => decoyPositions[playerIndex] = position;
        public static void ClearDecoy(int playerIndex) => decoyPositions.Remove(playerIndex);
        public static bool TryGetDecoy(int playerIndex, out Vector2 position) => decoyPositions.TryGetValue(playerIndex, out position);

        private Vector2 tempOriginalPos;
        private bool isPosSwapped;
        private int swappedPlayerIndex = -1;

        // ── Intercepción de Aggro / Decoy ──
        public override bool PreAI(NPC npc)
        {
            if (!npc.active || npc.friendly) return base.PreAI(npc);

            isPosSwapped = false;
            swappedPlayerIndex = -1;

            if (npc.target >= 0 && npc.target < Main.maxPlayers)
            {
                Player targeted = Main.player[npc.target];
                if (targeted.active && targeted.GetModPlayer<D4CGhostPlayer_Tier_4>().GhostActive)
                {
                    targeted.aggro = int.MinValue / 2;
                    Player mejorAlterno = FindBestAlternativePlayer(targeted.whoAmI, npc.Center);

                    if (mejorAlterno != null)
                    {
                        npc.target = mejorAlterno.whoAmI;
                        npc.netUpdate = true;
                    }
                    else
                    {
                        if (TryGetDecoy(targeted.whoAmI, out Vector2 decoyPos))
                        {
                            swappedPlayerIndex = targeted.whoAmI;
                            tempOriginalPos = targeted.position;
                            targeted.position = decoyPos - new Vector2(targeted.width / 2f, targeted.height / 2f);
                            isPosSwapped = true;
                        }
                    }
                }
            }
            return base.PreAI(npc);
        }

        public override void PostAI(NPC npc)
        {
            if (isPosSwapped && swappedPlayerIndex >= 0 && swappedPlayerIndex < Main.maxPlayers)
            {
                Player targeted = Main.player[swappedPlayerIndex];
                targeted.position = tempOriginalPos;
                isPosSwapped = false;
                swappedPlayerIndex = -1;
            }
            base.PostAI(npc);
        }

        private static Player FindBestAlternativePlayer(int ignorePlayerIndex, Vector2 npcCenter)
        {
            Player mejorAlterno = null;
            float mejorDist = float.MaxValue;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player candidato = Main.player[i];
                if (!candidato.active || candidato.dead || candidato.whoAmI == ignorePlayerIndex) continue;
                if (candidato.GetModPlayer<D4CGhostPlayer_Tier_4>().GhostActive) continue;

                float dist = Vector2.Distance(npcCenter, candidato.Center);
                if (dist < mejorDist)
                {
                    mejorDist = dist;
                    mejorAlterno = candidato;
                }
            }
            return mejorAlterno;
        }

        // ── Oculta la barra de vida de CUALQUIER NPC mientras el jugador local está en modo fantasma ──
        public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
        {
            if (D4CGhostEngineHooks_Tier_4.IsLocalViewerGhosted)
                return false;

            return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);
        }
    }

    // ── Silenciador de Música Ambiental ──
    public class D4CGhostMusic_Tier_4 : ModSceneEffect
    {
        public override int Music => 0;
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;
        public override bool IsSceneEffectActive(Player player) => player.GetModPlayer<D4CGhostPlayer_Tier_4>().GhostActive;
    }

    // ── INTERCEPTOR ABSOLUTO DEL MOTOR DE TERRARIA ──
    public class D4CGhostEngineHooks_Tier_4 : ModSystem
    {
        private const int FADE_DURATION_TICKS = 20;

        private static int fadeTimer = -1;
        private static bool fadingOut = false;
        private static bool prevLocalGhosted = false;

        public override void Load()
        {
            On_Main.DrawNPC += Main_DrawNPC;
            On_Main.DrawProj += Main_DrawProj;
            On_Main.DrawDust += Main_DrawDust;
            On_Main.DrawGore += Main_DrawGore;
            On_SoundEngine.PlaySound_refSoundStyle_Nullable1_SoundUpdateCallback += SoundEngine_PlaySound;
        }

        public static bool IsLocalViewerGhosted =>
            Main.LocalPlayer != null && Main.LocalPlayer.active &&
            Main.LocalPlayer.GetModPlayer<D4CGhostPlayer_Tier_4>().GhostActive;

        private static bool IsPlayerGhosted(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= Main.maxPlayers) return false;
            Player pl = Main.player[playerIndex];
            return pl.active && pl.GetModPlayer<D4CGhostPlayer_Tier_4>().GhostActive;
        }

        public override void PostUpdateEverything()
        {
            bool nowGhosted = IsLocalViewerGhosted;

            if (nowGhosted && !prevLocalGhosted)
            {
                fadingOut = true;
                fadeTimer = 0;
            }
            else if (!nowGhosted && prevLocalGhosted)
            {
                fadingOut = false;
                fadeTimer = 0;
            }

            if (fadeTimer >= 0)
            {
                fadeTimer++;
                if (fadeTimer > FADE_DURATION_TICKS)
                {
                    fadeTimer = -1;
                }
            }

            prevLocalGhosted = nowGhosted;
        }

        private static int CalcularAlphaFundido(int alphaOriginal, float progreso, bool fundiendoAInvisible)
        {
            float origen = fundiendoAInvisible ? alphaOriginal : 255f;
            float destino = fundiendoAInvisible ? 255f : alphaOriginal;
            return (int)MathHelper.Clamp(MathHelper.Lerp(origen, destino, progreso), 0f, 255f);
        }

        private void Main_DrawNPC(On_Main.orig_DrawNPC orig, Main self, int i, bool behindTiles)
        {
            NPC npc = Main.npc[i];

            // Si eres fantasma y no hay fundido activo, los NPCs simplemente no se dibujan
            if (IsLocalViewerGhosted && fadeTimer < 0) return;

            // Si hay un fundido en proceso
            if (fadeTimer >= 0)
            {
                int oldAlpha = npc.alpha; // Guardamos su alpha original de forma temporal
                float progreso = MathHelper.Clamp(fadeTimer / (float)FADE_DURATION_TICKS, 0f, 1f);

                npc.alpha = CalcularAlphaFundido(oldAlpha, progreso, fadingOut);
                orig(self, i, behindTiles);

                npc.alpha = oldAlpha; // Restauramos su alpha al instante (previene el bug de off-screen)
                return;
            }

            orig(self, i, behindTiles);
        }

        private void Main_DrawProj(On_Main.orig_DrawProj orig, Main self, int i)
        {
            Projectile proj = Main.projectile[i];
            if (!proj.active) { orig(self, i); return; }

            bool esLocal = proj.owner == Main.myPlayer;
            bool ownerGhosted = IsPlayerGhosted(proj.owner);

            // 1. Ocultar los proyectiles de OTROS jugadores que estén en modo fantasma
            if (ownerGhosted && !esLocal)
                return;

            // EXCEPCIÓN ABSOLUTA: Si es TU propio proyectil (como tu Stand), se ignora TODA la invisibilidad.
            if (esLocal)
            {
                orig(self, i);
                return;
            }

            // 2. Si eres fantasma y no hay fundido, los proyectiles ajenos no se dibujan
            if (IsLocalViewerGhosted && fadeTimer < 0) return;

            // 3. Fundido dinámico para los proyectiles del resto del mundo (igual que los NPCs)
            if (fadeTimer >= 0)
            {
                int oldAlpha = proj.alpha;
                float progreso = MathHelper.Clamp(fadeTimer / (float)FADE_DURATION_TICKS, 0f, 1f);

                proj.alpha = CalcularAlphaFundido(oldAlpha, progreso, fadingOut);
                orig(self, i);

                proj.alpha = oldAlpha; // Restauramos su alpha al instante
                return;
            }

            orig(self, i);
        }

        private void Main_DrawDust(On_Main.orig_DrawDust orig, Main self)
        {
            if (IsLocalViewerGhosted)
            {
                for (int i = 0; i < Main.maxDust; i++)
                {
                    Dust d = Main.dust[i];
                    if (d.active && Vector2.Distance(Main.LocalPlayer.Center, d.position) > 250f)
                    {
                        d.active = false;
                    }
                }
            }
            orig(self);
        }

        private void Main_DrawGore(On_Main.orig_DrawGore orig, Main self)
        {
            if (IsLocalViewerGhosted)
            {
                for (int i = 0; i < Main.maxGore; i++)
                {
                    Gore g = Main.gore[i];
                    if (g.active && Vector2.Distance(Main.LocalPlayer.Center, g.position) > 250f)
                    {
                        g.active = false;
                    }
                }
            }
            orig(self);
        }

        private ReLogic.Utilities.SlotId SoundEngine_PlaySound(On_SoundEngine.orig_PlaySound_refSoundStyle_Nullable1_SoundUpdateCallback orig, ref SoundStyle style, Vector2? position, SoundUpdateCallback updateCallback)
        {
            if (Main.gameMenu || !IsLocalViewerGhosted)
                return orig(ref style, position, updateCallback);

            if (position.HasValue)
            {
                float dist = Vector2.Distance(Main.LocalPlayer.Center, position.Value);
                if (dist > 200f) return ReLogic.Utilities.SlotId.Invalid;
            }
            else
            {
                return ReLogic.Utilities.SlotId.Invalid;
            }

            return orig(ref style, position, updateCallback);
        }
    }
}