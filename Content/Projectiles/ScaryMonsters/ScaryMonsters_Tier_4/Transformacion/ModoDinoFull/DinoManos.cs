using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_3;

namespace Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4.Transformacion.ModoDinoFull
{
    public class DinoManosPlayer : ModPlayer
    {
        public const int FrameIdle = 0;
        public const int AttackStart = 1;
        public const int AttackEnd = 5;
        public const int FrameAir = 7;

        const int TicksPorFrameAtaque = 3;

        public int ManosFrame;
        int attackCounter;

        // Garra activa de CUALQUIER tier de Scary Monsters
        bool GarraActiva()
        {
            return Player.ownedProjectileCounts[ModContent.ProjectileType<GarraSlash_Tier_4>()] > 0
                || Player.ownedProjectileCounts[ModContent.ProjectileType<GarraSlash_Tier_3>()] > 0;
        }

        public override void PostUpdate()
        {
            if (!Player.GetModPlayer<DinoPlayer>().IsDino)
            {
                ManosFrame = FrameIdle;
                attackCounter = 0;
                return;
            }

            bool atacando = GarraActiva();
            bool enAire = Player.velocity.Y != 0f;

            if (atacando)
            {
                int total = AttackEnd - AttackStart + 1;
                ManosFrame = AttackStart + (attackCounter / TicksPorFrameAtaque) % total;
                attackCounter++;
            }
            else
            {
                attackCounter = 0;
                ManosFrame = enAire ? FrameAir : FrameIdle;
            }
        }
    }

    public class DinoManosDrawLayer : PlayerDrawLayer
    {
        public const int FrameHeight = 58;
        public const int FrameStride = 58;

        const float OffsetX = 3f;
        const float OffsetY = 10f;

        public override Position GetDefaultPosition()
            => new AfterParent(ModContent.GetInstance<DinoDrawLayer>());

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            return p != null && p.active && !p.dead && p.GetModPlayer<DinoPlayer>().IsDino;
        }

        static Texture2D LoadTexture()
        {
            string path = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_4/Transformacion/ModoDinoFull/FormaDinoManos";
            if (!ModContent.HasAsset(path))
                path = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_4/Transformacion/FormaDinoManos";
            if (!ModContent.HasAsset(path))
                path = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_4/FormaDinoManos";
            if (!ModContent.HasAsset(path))
                path = "Jojo/Content/Projectiles/ScaryMonsters/FormaDinoManos";
            if (!ModContent.HasAsset(path))
                return null;
            return ModContent.Request<Texture2D>(path).Value;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            Texture2D tex = LoadTexture();
            if (tex == null) return;

            int f = player.GetModPlayer<DinoManosPlayer>().ManosFrame;

            int maxFrames = Math.Max(1, tex.Height / FrameStride);
            f = Math.Min(f, maxFrames - 1);

            int srcH = Math.Min(FrameHeight, tex.Height - f * FrameStride);
            Rectangle source = new Rectangle(0, f * FrameStride, tex.Width, srcH);

            Vector2 origin = new Vector2(tex.Width / 2f, srcH);
            Vector2 pos = new Vector2(
                (int)(player.Center.X - Main.screenPosition.X + OffsetX * player.direction),
                (int)(player.Bottom.Y - Main.screenPosition.Y + player.gfxOffY + OffsetY));

            Color color = Lighting.GetColor(
                (int)(player.Center.X / 16f), (int)(player.Center.Y / 16f));

            SpriteEffects fx = player.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            drawInfo.DrawDataCache.Add(new DrawData(tex, pos, source, color, 0f, origin, 1f, fx, 0));
        }
    }
}