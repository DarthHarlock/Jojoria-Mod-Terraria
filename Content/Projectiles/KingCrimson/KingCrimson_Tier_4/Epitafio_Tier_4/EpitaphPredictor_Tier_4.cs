using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Buffs.KingCrimson_Buffs;

namespace Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_4.Epitafio_Tier_4
{
    public class EpitaphPredictor_Tier_4 : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        protected List<Vector2> predictedPath = new List<Vector2>();

        // Configuración (60 ticks = 1s futuro). Sirve para Tier 3 y Tier 4.
        protected virtual int PredictionTicks => 60;

        // Tier 3 y Tier 4 comparten esta lógica: basta con tener el buff EpitaphVision.
        protected virtual bool IsActiveForPlayer(Player p)
        {
            return p.active && p.HasBuff(ModContent.BuffType<EpitaphVision>());
        }

        public override void PostAI(NPC npc)
        {
            bool epitaphActive = false;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (IsActiveForPlayer(Main.player[i]))
                {
                    epitaphActive = true;
                    break;
                }
            }

            if (!epitaphActive || npc.friendly || !npc.active)
            {
                predictedPath.Clear();
                return;
            }

            predictedPath.Clear();
            Vector2 simPos = npc.position;
            Vector2 simVel = npc.velocity;

            int ticks = PredictionTicks;
            for (int i = 0; i < ticks; i++)
            {
                if (!npc.noGravity)
                {
                    simVel.Y += 0.3f;
                    if (simVel.Y > 10f) simVel.Y = 10f;
                }

                simVel = Collision.TileCollision(simPos, simVel, npc.width, npc.height, fallThrough: false, fall2: false);
                simPos += simVel;
                predictedPath.Add(simPos + npc.Size / 2f);
            }
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (predictedPath.Count == 0) return;

            for (int i = 0; i < predictedPath.Count; i += 5)
            {
                Vector2 drawPos = predictedPath[i] - screenPos;
                Texture2D magicTexture = Terraria.GameContent.TextureAssets.MagicPixel.Value;
                Rectangle rect = new Rectangle(0, 0, 4, 4);
                spriteBatch.Draw(magicTexture, drawPos, rect, new Color(255, 0, 0, 100), 0f, rect.Size() / 2f, 1f, SpriteEffects.None, 0f);
            }

            Vector2 futureCenter = predictedPath[predictedPath.Count - 1];

            Main.instance.LoadNPC(npc.type);
            Texture2D texture = Terraria.GameContent.TextureAssets.Npc[npc.type].Value;

            int frameHeight = texture.Height / Main.npcFrameCount[npc.type];
            Rectangle sourceRect = new Rectangle(0, npc.frame.Y, texture.Width, frameHeight);
            Vector2 origin = sourceRect.Size() / 2f;
            SpriteEffects effects = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Color futureColor = new Color(255, 50, 50, 0) * 0.6f;

            spriteBatch.Draw(
                texture,
                futureCenter - screenPos,
                sourceRect,
                futureColor,
                npc.rotation,
                origin,
                npc.scale,
                effects,
                0f
            );
        }
    }

    public class EpitaphProjectilePredictor_Tier_4 : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        protected List<Vector2> predictedPath = new List<Vector2>();

        // Configuración (45 ticks). Sirve para Tier 3 y Tier 4.
        protected virtual int PredictionTicks => 45;

        // Tier 3 y Tier 4 comparten esta lógica: basta con tener el buff EpitaphVision.
        protected virtual bool IsActiveForPlayer(Player p)
        {
            return p.active && p.HasBuff(ModContent.BuffType<EpitaphVision>());
        }

        public override void PostAI(Projectile projectile)
        {
            bool epitaphActive = false;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (IsActiveForPlayer(Main.player[i]))
                {
                    epitaphActive = true;
                    break;
                }
            }

            if (!epitaphActive || projectile.friendly || !projectile.hostile || !projectile.active)
            {
                predictedPath.Clear();
                return;
            }

            predictedPath.Clear();
            Vector2 simPos = projectile.position;
            Vector2 simVel = projectile.velocity;

            int ticks = PredictionTicks;
            for (int i = 0; i < ticks; i++)
            {
                if (projectile.aiStyle == 1 || projectile.aiStyle == 2)
                    simVel.Y += 0.15f;

                simVel = Collision.TileCollision(simPos, simVel, projectile.width, projectile.height);
                simPos += simVel;
                predictedPath.Add(simPos + projectile.Size / 2f);
            }
        }

        public override void PostDraw(Projectile projectile, Color lightColor)
        {
            if (predictedPath.Count == 0) return;

            SpriteBatch spriteBatch = Main.spriteBatch;
            Texture2D magicTexture = Terraria.GameContent.TextureAssets.MagicPixel.Value;
            Rectangle rect = new Rectangle(0, 0, 2, 2);

            foreach (Vector2 point in predictedPath)
            {
                Vector2 drawPos = point - Main.screenPosition;
                spriteBatch.Draw(magicTexture, drawPos, rect, new Color(255, 50, 50, 150), 0f, rect.Size() / 2f, 1f, SpriteEffects.None, 0f);
            }
        }
    }
}