using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Buffs.D4C_Buffs;
using Jojo.Content.Projectiles.D4C.D4C_Tier_3.HabilidadH;

namespace Jojo.Content.Projectiles.D4C.D4C_Tier_3.HabilidadH
{
    public class D4CGhostPlayer_Tier_3 : ModPlayer
    {
        public const int GhostDurationTicks = 600;

        public bool GhostActive => Player.HasBuff(ModContent.BuffType<D4CGhostMode>());

        public Vector2 DecoyPosition { get; private set; }

        private bool wasGhostActive = false;

        public static readonly SoundStyle SonidoInicioFantasma = new("Jojo/Content/Sonidos/D4C_Start");
        private static readonly SoundStyle SonidoFinFantasma = new("Jojo/Content/Sonidos/D4C_End");

        public static void PlayStartSound(Vector2 position)
        {
            if (Main.netMode != NetmodeID.Server)
                SoundEngine.PlaySound(SonidoInicioFantasma, position);
        }

        // ── Efecto de aro de partículas al activar ──
        const int DURACION_EFECTO_FANTASMA = 15;
        const int PARTICULAS_ARO_FANTASMA = 48;
        const float RADIO_ARO_FANTASMA = 38f;
        const float SCALE_ARO_FANTASMA = 2.6f;
        const float SCALE_ELECTRICO_FANTASMA = 1.6f;
        const int PARTICULAS_ESTELA_FANTASMA = 6;
        const float SCALE_ESTELA_FANTASMA = 2.0f;

        int ticksEfectoFantasma = -1; // -1 = efecto inactivo
        Vector2 posAnteriorEfectoFantasma;
        List<Dust> dustsAroFantasma = new List<Dust>();

        public static void Activate(Player player, int durationTicks = GhostDurationTicks)
        {
            player.AddBuff(ModContent.BuffType<D4CGhostMode>(), durationTicks);

            var ghost = player.GetModPlayer<D4CGhostPlayer_Tier_3>();
            ghost.DecoyPosition = player.Center;
            D4CGhostWorldSystem_Tier_3.SetDecoy(player.whoAmI, ghost.DecoyPosition);
        }

        public void StartParticleEffect()
        {
            ticksEfectoFantasma = 0;
        }

        public override void PostUpdate()
        {
            bool active = GhostActive;

            if (active && !wasGhostActive)
                OnGhostStart();
            else if (!active && wasGhostActive)
                OnGhostEnd();

            wasGhostActive = active;

            ActualizarEfectoParticulasFantasma();
        }

        private void OnGhostStart()
        {
            if (!Main.dedServ && Player == Main.LocalPlayer)
            {
                if (Filters.Scene[Jojo.BlueShaderName]?.IsActive() != true)
                    Filters.Scene.Activate(Jojo.BlueShaderName);
            }
        }

        private void OnGhostEnd()
        {
            if (!Main.dedServ && Player == Main.LocalPlayer)
            {
                if (Filters.Scene[Jojo.BlueShaderName]?.IsActive() == true)
                    Filters.Scene[Jojo.BlueShaderName].Deactivate();
            }

            D4CGhostWorldSystem_Tier_3.ClearDecoy(Player.whoAmI);

            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SonidoFinFantasma, Player.Center);
            }
        }

        void GenerarAroGigante(Vector2 centro)
        {
            dustsAroFantasma.Clear();
            for (int i = 0; i < PARTICULAS_ARO_FANTASMA; i++)
            {
                float angulo = i * (MathHelper.TwoPi / PARTICULAS_ARO_FANTASMA);
                Vector2 direccion = angulo.ToRotationVector2();
                Vector2 posicion = centro + direccion * RADIO_ARO_FANTASMA;

                Vector2 velocidad = direccion * 3.2f;

                Dust dust = Dust.NewDustPerfect(posicion, DustID.IceTorch, velocidad, Scale: SCALE_ARO_FANTASMA);
                dust.noGravity = true;
                dustsAroFantasma.Add(dust);

                if (i % 2 == 0)
                {
                    Dust dustElec = Dust.NewDustPerfect(posicion, DustID.Electric, velocidad * 0.8f, Scale: SCALE_ELECTRICO_FANTASMA);
                    dustElec.noGravity = true;
                    dustsAroFantasma.Add(dustElec);
                }
            }
        }

        void GenerarEstelaAroGigante(Vector2 centro, Vector2 velBase, int tick)
        {
            for (int i = 0; i < PARTICULAS_ESTELA_FANTASMA; i++)
            {
                float angulo = (i * MathHelper.TwoPi / PARTICULAS_ESTELA_FANTASMA) + (tick * 0.3f);
                Vector2 direccion = angulo.ToRotationVector2();
                Vector2 posicion = centro + direccion * RADIO_ARO_FANTASMA;

                Vector2 velocidad = direccion * 1.6f + velBase * 0.8f;

                Dust dust = Dust.NewDustPerfect(posicion, DustID.IceTorch, velocidad, Scale: SCALE_ESTELA_FANTASMA);
                dust.noGravity = true;
            }
        }

        void ActualizarEfectoParticulasFantasma()
        {
            if (ticksEfectoFantasma < 0 || ticksEfectoFantasma >= DURACION_EFECTO_FANTASMA) return;

            if (Main.netMode != NetmodeID.Server)
            {
                Vector2 posActual = Player.Center;

                if (ticksEfectoFantasma == 0)
                {
                    GenerarAroGigante(posActual);
                    posAnteriorEfectoFantasma = posActual;
                }
                else
                {
                    Vector2 delta = posActual - posAnteriorEfectoFantasma;
                    foreach (Dust d in dustsAroFantasma)
                    {
                        if (d != null && d.active)
                        {
                            d.position += delta;
                        }
                    }
                    posAnteriorEfectoFantasma = posActual;
                }

                GenerarEstelaAroGigante(posActual, Player.velocity, ticksEfectoFantasma);
            }

            ticksEfectoFantasma++;

            if (ticksEfectoFantasma >= DURACION_EFECTO_FANTASMA)
            {
                dustsAroFantasma.Clear();
                ticksEfectoFantasma = -1;
            }
        }

        public override bool FreeDodge(Player.HurtInfo info) => GhostActive || base.FreeDodge(info);

        public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
        {
            return GhostActive || base.ImmuneTo(damageSource, cooldownCounter, dodgeable);
        }

        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot) => !GhostActive;
        public override bool CanBeHitByProjectile(Projectile proj) => !GhostActive;
        public override bool CanHitNPC(NPC target) => !GhostActive;
        public override bool? CanHitNPCWithItem(Item item, NPC target) => GhostActive ? false : null;

        public override void SetControls()
        {
            if (!GhostActive) return;

            // LA LÍNEA QUE CAUSABA EL PROBLEMA EN LA TIER 3 FUE ELIMINADA.
            // Player.controlUseItem = false;
        }

        public override void HideDrawLayers(PlayerDrawSet drawInfo)
        {
            if (Player.whoAmI == Main.myPlayer) return;

            bool viewerGhosted = Main.LocalPlayer != null && Main.LocalPlayer.active &&
                                  Main.LocalPlayer.GetModPlayer<D4CGhostPlayer_Tier_3>().GhostActive;

            if (viewerGhosted || GhostActive)
            {
                foreach (PlayerDrawLayer layer in PlayerDrawLayerLoader.Layers)
                    layer.Hide();
            }
        }
    }
}