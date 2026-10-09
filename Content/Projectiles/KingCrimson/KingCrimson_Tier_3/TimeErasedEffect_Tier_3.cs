using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ReLogic.Content;
using Terraria.Audio;

namespace Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_3
{
    public class TimeErasedEffect_Tier_3 : ModProjectile
    {
        public override string Texture => "Jojo/Content/Projectiles/KingCrimson/KingCrimson_Tier_3/Frames/1";

        private const int TotalFrames = 18;
        private const int TicksPerFrame = 3;

        private static Asset<Texture2D>[] frameAssets;

        static readonly SoundStyle TimeErasedSound = new("Jojo/Content/Sonidos/TimeErased");

        public override void Load()
        {
            frameAssets = new Asset<Texture2D>[TotalFrames];
            for (int i = 0; i < TotalFrames; i++)
            {
                frameAssets[i] = ModContent.Request<Texture2D>(
                    $"Jojo/Content/Projectiles/KingCrimson/KingCrimson_Tier_3/Frames/{i + 1}",
                    AssetRequestMode.ImmediateLoad
                );
            }
        }

        public override void Unload() => frameAssets = null;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 999;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source) => SoundEngine.PlaySound(TimeErasedSound, null);

        public override void AI()
        {
            Projectile.Center = Main.player[Projectile.owner].Center;
            if (++Projectile.frameCounter >= TicksPerFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= TotalFrames) Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public override void PostDraw(Color lightColor)
        {
            if (frameAssets == null) return;

            Texture2D tex = frameAssets[Projectile.frame].Value;

            // Forzamos el uso del SpriteBatch de pantalla para que ocupe todo el área de la interfaz del jugador
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

            // Dibujamos el fotograma abarcando toda la resolución de pantalla actual
            Main.spriteBatch.Draw(
                tex,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                null,
                Color.White
            );

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}