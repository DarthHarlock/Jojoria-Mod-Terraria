using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.KingCrimson_Buffs;

namespace Jojo.Content.Players
{
    public class EpitaphPlayer : ModPlayer
    {
        // Esta es la función correcta en tModLoader 1.4.4 para esquivar golpes
        public override bool FreeDodge(Player.HurtInfo info)
        {
            // Si el jugador tiene el buff de la predicción
            if (Player.HasBuff(ModContent.BuffType<PrediccionKingCrimson>()))
            {
                // 1. Eliminamos el buff para que se gaste (como la armadura sagrada)
                Player.ClearBuff(ModContent.BuffType<PrediccionKingCrimson>());

                // 2. Le damos el tiempo de invulnerabilidad (90 ticks = 1.5 seg)
                Player.SetImmuneTimeForAllTypes(90);

                // 3. Efectos de sonido y visuales (sin el texto)
                SoundEngine.PlaySound(SoundID.Item29, Player.Center);

                for (int i = 0; i < 20; i++)
                {
                    Dust dust = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.LifeDrain, 0f, 0f, 100, default, 1.5f);
                    dust.velocity *= 2f;
                    dust.noGravity = true;
                }

                // 4. Retornar TRUE significa "Esquiva el golpe y anula el daño"
                return true;
            }

            // Si no tiene el buff, retorna FALSE y recibe el golpe normal
            return false;
        }
    }
}