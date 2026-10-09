using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_3
{
    public class KingCrimsonHeavyTrail_Tier_3 : ModProjectile
    {
        public override string Texture => "Jojo/Content/Projectiles/KingCrimson/KingCrimson_Tier_3/GolpeFuerte_Tier_3";

        private float fadeAlpha = 1f;
        private const float FadeSpeed = 0.08f;

        public override void SetDefaults()
        {
            Projectile.width = 88;
            Projectile.height = 74;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 9999;
            Projectile.hide = true;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.rotation = Projectile.ai[2];
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = Projectile.ai[2];
            fadeAlpha -= FadeSpeed;
            if (fadeAlpha <= 0f) Projectile.Kill();
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
            => behindNPCs.Add(index);

        public override bool PreDraw(ref Color lightColor)
        {
            if (fadeAlpha <= 0f) return false;

            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Rectangle r = new Rectangle((int)Projectile.ai[0] * 88, 0, 88, 74);
            Vector2 o = new Vector2(44, 37);

            bool flipVisual = Projectile.ai[1] == 1f;
            Color colorSombra = new Color(119, 21, 55, 80) * 2.6f * fadeAlpha;

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, r,
                colorSombra, Projectile.rotation, o, Projectile.scale,
                flipVisual ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
            return false;
        }
    }
}