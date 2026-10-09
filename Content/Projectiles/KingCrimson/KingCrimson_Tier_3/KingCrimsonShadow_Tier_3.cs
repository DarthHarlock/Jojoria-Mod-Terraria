using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_3
{
    public class KingCrimsonShadow_Tier_3 : ModProjectile
    {
        public override string Texture => "Terraria/Images/NPC_0";
        private bool fadingOut = false;
        private float fadeAlpha = 1f;
        private const float FadeSpeed = 0.07f;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 9999;
            Projectile.alpha = 150;
            Projectile.hide = true;
        }

        public void StartFadeOut()
        {
            fadingOut = true;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            if (fadingOut)
            {
                fadeAlpha -= FadeSpeed;
                if (fadeAlpha <= 0f)
                {
                    fadeAlpha = 0f;
                    Projectile.Kill();
                }
                return;
            }

            int npcType = (int)Projectile.ai[0];
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly) continue;
                if (npc.type != npcType) continue;

                KingCrimsonTimeTracker_Tier_3 tracker = npc.GetGlobalNPC<KingCrimsonTimeTracker_Tier_3>();
                if (!tracker.isReplaying) continue;

                float dist = Vector2.Distance(npc.Center, Projectile.Center);
                if (dist < 24f)
                {
                    fadingOut = true;
                    break;
                }
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (fadeAlpha <= 0f) return false;

            int npcType = (int)Projectile.ai[0];
            int npcDirection = (int)Projectile.ai[1];
            int frameY = (int)Projectile.ai[2];

            Main.instance.LoadNPC(npcType);
            Texture2D texture = Terraria.GameContent.TextureAssets.Npc[npcType].Value;
            int frameHeight = texture.Height / Main.npcFrameCount[npcType];
            Rectangle sourceRect = new Rectangle(0, frameY, texture.Width, frameHeight);
            Vector2 origin = sourceRect.Size() / 2f;

            SpriteEffects effects = npcDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Color colorSombra = new Color(119, 21, 55, 80) * 2.6f * fadeAlpha;

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                sourceRect,
                colorSombra,
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0
            );
            return false;
        }
    }
}