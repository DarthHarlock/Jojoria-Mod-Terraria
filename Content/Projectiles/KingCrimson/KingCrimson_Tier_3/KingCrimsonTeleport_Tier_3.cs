using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using ReLogic.Content;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;

namespace Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_3
{
    public class KingCrimsonTeleport_Tier_3 : ModProjectile
    {
        public override string Texture => "Jojo/Content/Projectiles/KingCrimson/KingCrimson_Tier_3/Frames/1";

        private const int TotalFrames = 18;
        private const int TicksPerFrame = 3;
        private const float ConfusionRadius = 300f;
        private static Asset<Texture2D>[] frameAssets;

        // Mismo sonido que TimeErased
        static readonly SoundStyle TeleportSound = new("Jojo/Content/Sonidos/TimeErased");

        public override void Load()
        {
            frameAssets = new Asset<Texture2D>[TotalFrames];
            for (int i = 0; i < TotalFrames; i++)
                frameAssets[i] = ModContent.Request<Texture2D>(
                    $"Jojo/Content/Projectiles/KingCrimson/KingCrimson_Tier_3/Frames/{i + 1}",
                    AssetRequestMode.ImmediateLoad);
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

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // Sonido + teletransporte + confusión al instante, frame 0
            SoundEngine.PlaySound(TeleportSound, null);

            if (Projectile.owner == Main.myPlayer)
            {
                Player p = Main.player[Projectile.owner];
                Vector2 destino = new Vector2(Projectile.ai[0], Projectile.ai[1]);

                p.Teleport(destino, 1);
                NetMessage.SendData(Terraria.ID.MessageID.PlayerControls, -1, -1, null, p.whoAmI);

                // Confusión desde el destino
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (!npc.active || npc.friendly || npc.life <= 0) continue;
                    if (Vector2.Distance(destino, npc.Center) <= ConfusionRadius)
                        npc.AddBuff(Terraria.ID.BuffID.Confused, 180);
                }
            }
        }

        public override void AI()
        {
            // Sigue al jugador durante la animación
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
            int fi = System.Math.Clamp(Projectile.frame, 0, TotalFrames - 1);
            Texture2D tex = frameAssets[fi].Value;
            Vector2 screenCenter = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
            Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
            Vector2 scale = new Vector2((float)Main.screenWidth / tex.Width,
                                        (float)Main.screenHeight / tex.Height) / Main.GameViewMatrix.Zoom;
            Main.EntitySpriteDraw(tex, screenCenter, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0);
        }
    }
}