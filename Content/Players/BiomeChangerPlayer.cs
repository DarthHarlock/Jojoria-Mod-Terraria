using Terraria;
using Terraria.ModLoader;
using Terraria.Graphics.Effects;
using Jojo.Content.Systems;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.KingCrimson_Buffs;

namespace Jojo.Content.Players
{
    public class BiomeChangerPlayer : ModPlayer
    {
        public override void ResetEffects()
        {
            BiomeOverlaySystem.DrawOverlay = false;
        }

        public override void UpdateEquips()
        {
            if (Player.HasBuff(ModContent.BuffType<TimeErased>()))
            {
                if (Player.whoAmI == Main.myPlayer)
                    BiomeOverlaySystem.DrawOverlay = true;
            }
        }

        public override void PostUpdate()
        {
            if (Main.dedServ || Player.whoAmI != Main.myPlayer)
                return;

            if (BiomeOverlaySystem.DrawOverlay)
            {
                if (!Filters.Scene[Jojo.RedShaderName].IsActive())
                    Filters.Scene.Activate(Jojo.RedShaderName);
            }
            else
            {
                if (Filters.Scene[Jojo.RedShaderName].IsActive())
                    Filters.Scene[Jojo.RedShaderName].Deactivate();
            }
        }
    }
}