using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent;
using Terraria.DataStructures;

namespace Jojo.Content.Buffs.StarPlatinum_Buffs
{
    // Layer GLOBAL: funciona con las 4 variantes del buff (Tier_1 a Tier_4).
    // No hace falta crear un Aura por tier, esta sola detecta cualquiera de las 4.
    public class PlatinumRageAuraLayer : PlayerDrawLayer
    {
        int frame;
        int frameTimer;

        public override Position GetDefaultPosition()
        {
            return new AfterParent(PlayerDrawLayers.MountBack);
        }

        // Verifica si el jugador tiene CUALQUIERA de los 4 tiers del buff activo
        static bool HasAnyPlatinumRageTier(Player player)
        {
            return player.HasBuff(ModContent.BuffType<PlatinumRage_Tier_2>())
                || player.HasBuff(ModContent.BuffType<PlatinumRage_Tier_3>())
                || player.HasBuff(ModContent.BuffType<PlatinumRage_Tier_4>());
              
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return HasAnyPlatinumRageTier(drawInfo.drawPlayer);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;

            Texture2D tex = ModContent.Request<Texture2D>(
                "Jojo/Content/Buffs/StarPlatinum_Buffs/PlatinumRageAura"
            ).Value;

            // 🔥 animación 6 frames
            frameTimer++;
            if (frameTimer >= 6)
            {
                frameTimer = 0;
                frame++;
                if (frame >= 6)
                    frame = 0;
            }

            Rectangle source = new Rectangle(frame * 60, 0, 60, 80);

            Vector2 pos = player.Center - Main.screenPosition;

            drawInfo.DrawDataCache.Add(new DrawData(
                tex,
                pos,
                source,
                Color.White * 0.9f,
                0f,
                new Vector2(30f, 40f), // centro del frame 60x80
                1f,
                SpriteEffects.None,
                0f
            ));
        }
    }
}