using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.KingCrimson_Buffs;

namespace Jojo.Content.Systems
{
    public class TimeErasedMusicSystem : ModSystem
    {
        public static float MusicMultiplier = 1f; // 0 = silencio, 1 = normal
        private const float FadeSpeed = 0.03f;

        public override void PostUpdateEverything()
        {
            bool anyPlayerTimeErased = false;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.active && !p.dead && p.HasBuff(ModContent.BuffType<TimeErased>()))
                { anyPlayerTimeErased = true; break; }
            }

            float target = anyPlayerTimeErased ? 0f : 1f;
            if (MusicMultiplier > target)
                MusicMultiplier = System.Math.Max(target, MusicMultiplier - FadeSpeed);
            else if (MusicMultiplier < target)
                MusicMultiplier = System.Math.Min(target, MusicMultiplier + FadeSpeed);

            // Aplica el multiplicador al volumen real sin tocar el setting guardado
            Main.musicFade[Main.curMusic] *= MusicMultiplier;
        }
    }
}