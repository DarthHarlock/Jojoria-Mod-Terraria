using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4.Transformacion
{
    // =====================================================================
    // 1. CONTROLADOR VISUAL
    // Se encarga EXCLUSIVAMENTE de mostrar la ropa si el Stand se lo pide.
    // =====================================================================
    public class ScaryMonstersVisualPlayer : ModPlayer
    {
        public float visualAlpha = 0f;
        public int standActiveTimer = 0;

        // Cualquier Stand (Tier 3, Tier 4, etc.) llamará a este método en su AI()
        public void KeepVisualsAlive()
        {
            standActiveTimer = 2;
        }

        public override void PreUpdate()
        {
            if (standActiveTimer > 0 && !Player.dead)
            {
                standActiveTimer--;
                if (visualAlpha < 1f)
                {
                    visualAlpha += 0.1f;
                    if (visualAlpha > 1f) visualAlpha = 1f;
                }
            }
            else
            {
                // Desaparece instantáneamente si el Stand se desconvoca o el jugador muere
                visualAlpha = 0f;
            }
        }
    }

    // =====================================================================
    // 2. CAPAS DE DIBUJO
    // =====================================================================

    // --- COLA (Detrás de las piernas) ---
    public class SMTailLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Leggings);
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.drawPlayer.GetModPlayer<ScaryMonstersVisualPlayer>().visualAlpha > 0f;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            float alpha = player.GetModPlayer<ScaryMonstersVisualPlayer>().visualAlpha;

            string path = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_4/Transformacion/Tail_Tier_4";
            if (!ModContent.HasAsset(path)) return;
            Texture2D texture = ModContent.Request<Texture2D>(path).Value;

            int totalFrames = 20;
            int frameHeight = texture.Height / totalFrames;
            int currentFrame = player.bodyFrame.Y / player.bodyFrame.Height;
            if (currentFrame >= totalFrames) currentFrame = 0;

            Rectangle sourceRect = new Rectangle(0, currentFrame * frameHeight, 60, frameHeight);
            Vector2 drawPos = new Vector2((int)(drawInfo.Position.X - Main.screenPosition.X + player.width / 2f), (int)(drawInfo.Position.Y - Main.screenPosition.Y + player.height / 2f));
            drawPos.X -= 12f * player.direction;
            drawPos.Y += 4f;
            drawPos += player.legPosition;

            SpriteEffects effects = player.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            DrawData data = new DrawData(texture, drawPos, sourceRect, drawInfo.colorArmorLegs * alpha, player.legRotation, new Vector2(30f, frameHeight / 2f), 1f, effects, 0);
            drawInfo.DrawDataCache.Add(data);
        }
    }

    // --- GARRAS PIES (Encima de las piernas) ---
    public class SMFeetLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Leggings);
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.drawPlayer.GetModPlayer<ScaryMonstersVisualPlayer>().visualAlpha > 0f;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            float alpha = player.GetModPlayer<ScaryMonstersVisualPlayer>().visualAlpha;

            string path = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_4/Transformacion/GarrasPies";
            if (!ModContent.HasAsset(path)) return;
            Texture2D texture = ModContent.Request<Texture2D>(path).Value;

            Vector2 pos = new Vector2((int)(drawInfo.Position.X - Main.screenPosition.X - player.legFrame.Width / 2f + player.width / 2f), (int)(drawInfo.Position.Y - Main.screenPosition.Y + player.height - player.legFrame.Height + 4f)) + player.legPosition + drawInfo.legVect;
            DrawData data = new DrawData(texture, pos, player.legFrame, drawInfo.colorArmorLegs * alpha, player.legRotation, drawInfo.legVect, 1f, drawInfo.playerEffect, 0);
            drawInfo.DrawDataCache.Add(data);
        }
    }

    // --- GARRAS MANOS (Guantes base normales) ---
    public abstract class SMHandsBaseLayer : PlayerDrawLayer
    {
        protected abstract bool IsFront { get; }
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.drawPlayer.GetModPlayer<ScaryMonstersVisualPlayer>().visualAlpha > 0f;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            float alpha = player.GetModPlayer<ScaryMonstersVisualPlayer>().visualAlpha;

            string path = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_4/Transformacion/GarrasManos";
            if (!ModContent.HasAsset(path))
                path = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_4/GarrasManos"; // Respaldo
            if (!ModContent.HasAsset(path)) return;

            Texture2D texture = ModContent.Request<Texture2D>(path).Value;

            Vector2 pos = new Vector2((int)(drawInfo.Position.X - Main.screenPosition.X - player.bodyFrame.Width / 2f + player.width / 2f), (int)(drawInfo.Position.Y - Main.screenPosition.Y + player.height - player.bodyFrame.Height + 4f)) + player.bodyPosition + new Vector2(player.bodyFrame.Width / 2f, player.bodyFrame.Height / 2f);

            Rectangle source = IsFront ? drawInfo.compFrontArmFrame : drawInfo.compBackArmFrame;
            if (source.Width <= 0 || source.Height <= 0) source = player.bodyFrame;

            int dir = player.direction;
            Vector2 pivotOffset = IsFront ? new Vector2(-5f * dir, 0f) : new Vector2(6f * dir, 2f);
            float rotation = IsFront ? (player.compositeFrontArm.enabled ? player.compositeFrontArm.rotation : player.bodyRotation) : (player.compositeBackArm.enabled ? player.compositeBackArm.rotation : player.bodyRotation);

            DrawData data = new DrawData(texture, pos + pivotOffset, source, drawInfo.colorArmorBody * alpha, rotation, drawInfo.bodyVect + pivotOffset, 1f, drawInfo.playerEffect, 0);
            drawInfo.DrawDataCache.Add(data);
        }
    }

    // Mano Base Delantera
    public class SMFrontHandLayer : SMHandsBaseLayer
    {
        protected override bool IsFront => true;
        // CORREGIDO: ArmOverItem en lugar de ArmOverBody
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.ArmOverItem);
    }

    // Mano Base Trasera
    public class SMBackHandLayer : SMHandsBaseLayer
    {
        protected override bool IsFront => false;
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.HandOnAcc);
    }

    // --- GARRAS MANOS ON (El accesorio "encendido") ---
    public class SMFrontHandOnLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.LastVanillaLayer);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.drawPlayer.GetModPlayer<ScaryMonstersVisualPlayer>().visualAlpha > 0f;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player drawPlayer = drawInfo.drawPlayer;
            float alpha = drawPlayer.GetModPlayer<ScaryMonstersVisualPlayer>().visualAlpha;

            string path = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_4/Transformacion/GarrasManosOn";
            if (!ModContent.HasAsset(path))
                path = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_4/GarrasManosOn";
            if (!ModContent.HasAsset(path))
                path = "Jojo/Content/Projectiles/ScaryMonsters/GarrasManosOn";
            if (!ModContent.HasAsset(path))
                return;

            Texture2D texture = ModContent.Request<Texture2D>(path).Value;

            Vector2 pos = new Vector2(
                (int)(drawInfo.Position.X - Main.screenPosition.X - drawPlayer.bodyFrame.Width / 2f + drawPlayer.width / 2f),
                (int)(drawInfo.Position.Y - Main.screenPosition.Y + drawPlayer.height - drawPlayer.bodyFrame.Height + 4f)
            ) + drawPlayer.bodyPosition + new Vector2(drawPlayer.bodyFrame.Width / 2f, drawPlayer.bodyFrame.Height / 2f);

            Rectangle source = drawInfo.compFrontArmFrame;
            if (source.Width <= 0 || source.Height <= 0)
                source = drawPlayer.bodyFrame;

            int dir = drawPlayer.direction;
            Vector2 pivotOffset = new Vector2(-5f * dir, 0f);

            float rotation = drawPlayer.compositeFrontArm.enabled
                ? drawPlayer.compositeFrontArm.rotation
                : drawPlayer.bodyRotation;

            Color color = drawInfo.colorArmorBody * alpha;

            DrawData data = new DrawData(
                texture,
                pos + pivotOffset,
                source,
                color,
                rotation,
                drawInfo.bodyVect + pivotOffset,
                1f,
                drawInfo.playerEffect,
                0
            );

            drawInfo.DrawDataCache.Add(data);
        }
    }
}