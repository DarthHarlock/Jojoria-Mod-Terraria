using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using Jojo.Content.Systems;

namespace Jojo.Content.Projectiles.Tusk.Tusk_Tier_1.Disparo
{
    // Bloquea el uso del objeto de la mano mientras Tusk está apuntando y maneja la munición
    public class TuskAimPlayer : ModPlayer
    {
        public bool aiming;

        // Datos del brazo (los rellena el controlador, también para jugadores remotos)
        public bool armAiming;
        public float armAngle;

        // ---------- SISTEMA DE MUNICIÓN ----------
        public int municion = 10;
        public const int municionMax = 10;
        public float regenTimer = 0f;
        public int shakeTimer = 0;

        // Ticks necesarios para regenerar 1 bala a velocidad normal (120 = 2 s)
        const float TicksPorBala = 120f;

        // Multiplicador de regeneración. Los objetos (p. ej. SacoDeBalas) lo suben
        // en UpdateAccessory. 1 = normal, 8 = +700%.
        public float regenSpeed = 1f;
        // Valor aplicado este tick (el de regenSpeed del tick anterior, porque
        // los accesorios se procesan DESPUÉS de PreUpdate)
        float regenSpeedActive = 1f;

        // ---------- EFECTO DE RECARGA ----------
        public int reloadFlashTimer = 0;

        // Onda de recarga (modificable)
        const int OndaParticulas = 20;
        const float OndaVelocidad = 3.5f;
        const float OndaEscala = 1.2f;
        const float LongitudBrazo = 30f;
        static readonly Vector2 OffsetHombro = new Vector2(0f, -6f);
        static readonly Vector2 OffsetCuerpoInferior = new Vector2(0f, 16f);

        // ---------- VISUALES Y ANIMACIÓN ----------
        public float smoothXOffset = 35f;
        public float currentAlpha = 0f;
        public int idleTimer = 0;

        public override void ResetEffects()
        {
            regenSpeedActive = regenSpeed;
            regenSpeed = 1f;
        }

        public override bool CanUseItem(Item item)
        {
            return !aiming;
        }

        // Punto de origen de la onda:
        // - apuntando: la punta del brazo, hacia donde apunta
        // - sin apuntar: la parte inferior del cuerpo
        Vector2 GetReloadOrigin()
        {
            if (armAiming)
                return Player.MountedCenter + OffsetHombro + armAngle.ToRotationVector2() * LongitudBrazo;

            return Player.Center + OffsetCuerpoInferior;
        }

        // Anillo único de partículas rosas, todas iguales, que se expande en círculo
        void SpawnReloadWave(Vector2 center)
        {
            float offset = Main.rand.NextFloat(MathHelper.TwoPi);

            for (int i = 0; i < OndaParticulas; i++)
            {
                float angle = offset + MathHelper.TwoPi * i / OndaParticulas;
                Vector2 velocity = angle.ToRotationVector2() * OndaVelocidad;

                Dust d = Dust.NewDustPerfect(center, DustID.PinkFairy, velocity, 0, default, OndaEscala);
                d.noGravity = true;
            }
        }

        public override void PreUpdate()
        {
            int prevMunicion = municion;

            // 1. Recarga pasiva de balas: 1 bala cada 2 segundos a velocidad normal,
            //    acelerada por regenSpeedActive (SacoDeBalas = x8)
            if (municion < municionMax)
            {
                regenTimer += regenSpeedActive;
                while (regenTimer >= TicksPorBala && municion < municionMax)
                {
                    municion++;
                    regenTimer -= TicksPorBala;
                }

                if (municion >= municionMax)
                    regenTimer = 0f;
            }

            // Si la munición subió, activamos el efecto de recarga visual y sonoro
            if (municion > prevMunicion)
            {
                reloadFlashTimer = 45;
                idleTimer = 0;

                if (Main.netMode != NetmodeID.Server)
                {
                    Vector2 origin = GetReloadOrigin();

                    SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f, Pitch = 0.5f }, origin);
                    SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 0.4f, Pitch = 0.8f }, origin);

                    SpawnReloadWave(origin);
                }
            }

            if (reloadFlashTimer > 0)
                reloadFlashTimer--;

            // 2. Lógica del temblor
            if (shakeTimer > 0)
                shakeTimer--;

            // 3. Lógica de desvanecimiento (Alpha)
            if (aiming)
            {
                idleTimer = 0;
            }
            else if (municion == municionMax)
            {
                idleTimer++;
            }
            else
            {
                idleTimer = 0;
            }

            float targetAlpha = 0.9f;

            if (aiming)
                targetAlpha = 0f;
            else if (municion == municionMax && idleTimer > 60)
                targetAlpha = 0f;

            currentAlpha = MathHelper.Lerp(currentAlpha, targetAlpha, 0.15f);

            // 4. Lógica de Desplazamiento Suave al girar
            float targetX = Player.direction * (Player.width / 2 + 20);
            smoothXOffset = MathHelper.Lerp(smoothXOffset, targetX, 0.1f);
        }

        public void ConsumirBala()
        {
            if (municion > 0)
            {
                municion--;
                regenTimer = 0f;
                reloadFlashTimer = 0;
            }
        }

        public void TriggerEmpty()
        {
            shakeTimer = 25;
        }
    }

    // ---------- CAPA DE DIBUJO (UI DE LA BARRA) ----------
    public class TuskAmmoBarLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Skin);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            // Con la opción en 0 la barra no se dibuja en absoluto
            if (JojoConfig.Instance != null && JojoConfig.Instance.UITransparencia <= 0)
                return false;

            return drawInfo.drawPlayer.ownedProjectileCounts[ModContent.ProjectileType<TUSKSTAND_Tier_1>()] > 0;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            TuskAimPlayer tuskP = p.GetModPlayer<TuskAimPlayer>();

            // Config del jugador local
            // opacidadUI: 0 -> 0 | 50 -> 1 (tope)
            // realce:     0 hasta 50 -> 0 | 100 -> 1 (más brillo, más ancha, con borde)
            float opacidadUI = JojoConfig.Instance != null ? JojoConfig.Instance.MultiplicadorOpacidadUI : 1f;
            float realce = JojoConfig.Instance != null ? JojoConfig.Instance.RealceUI : 0f;

            // Con realce, el alpha máximo sube de 0.9 a 1.0 (manteniendo el desvanecimiento automático)
            float alpha = MathHelper.Clamp(tuskP.currentAlpha * opacidadUI * (1f + realce * 0.12f), 0f, 1f);

            if (alpha <= 0.01f)
                return;

            Vector2 position = p.Center + new Vector2(tuskP.smoothXOffset, -10) - Main.screenPosition;

            if (tuskP.shakeTimer > 0)
            {
                position.X += Main.rand.NextFloat(-2.5f, 2.5f);
                position.Y += Main.rand.NextFloat(-2.5f, 2.5f);
            }

            // La barra crece con el realce: 4 px -> 6 px de ancho, 36 -> 40 de alto
            int barWidth = 4 + (int)Math.Round(realce * 2f);
            int barHeight = 36 + (int)Math.Round(realce * 4f);

            // Fondo: más sólido y oscuro con el realce
            Color bgColor = new Color(30, 30, 30) * (alpha * MathHelper.Lerp(0.85f, 1f, realce));

            // Relleno: se aclara hacia el blanco con el realce, así se ve más brillante
            Color baseFill = new Color(255, 105, 180);

            if (tuskP.municion == 0 || tuskP.shakeTimer > 0)
                baseFill = Color.Red;
            else if (tuskP.reloadFlashTimer > 0)
                baseFill = new Color(160, 40, 110);

            Color fillColor = Color.Lerp(baseFill, Color.White, realce * 0.45f) * alpha;

            Texture2D pixel = TextureAssets.MagicPixel.Value;

            int left = (int)position.X - barWidth / 2;
            int top = (int)position.Y - barHeight / 2;

            // 0. Borde brillante (solo con realce)
            if (realce > 0.01f)
            {
                Color borderColor = Color.Lerp(baseFill, Color.White, 0.7f) * (alpha * realce);
                Rectangle borderRect = new Rectangle(left - 1, top - 1, barWidth + 2, barHeight + 2);
                drawInfo.DrawDataCache.Add(new DrawData(pixel, borderRect, borderColor));
            }

            // 1. Dibujar Fondo
            Rectangle bgRect = new Rectangle(left, top, barWidth, barHeight);
            drawInfo.DrawDataCache.Add(new DrawData(pixel, bgRect, bgColor));

            // 2. Dibujar Relleno
            if (tuskP.municion > 0)
            {
                float pct = (float)tuskP.municion / TuskAimPlayer.municionMax;
                int fillH = (int)(barHeight * pct);
                int yOff = barHeight - fillH;

                Rectangle fillRect = new Rectangle(left, top + yOff, barWidth, fillH);
                drawInfo.DrawDataCache.Add(new DrawData(pixel, fillRect, fillColor));
            }
        }
    }
}