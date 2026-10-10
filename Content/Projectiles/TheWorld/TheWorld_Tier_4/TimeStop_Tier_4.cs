using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Jojo.Content.Buffs;
using Jojo.Content.Systems;

namespace Jojo.Content.Habilidades
{
    public class TimeStop_TW_Tier_4
    {
        public const int Duration = 60 * 10; // 9 segundos

        private const string StartSound = "Jojo/Content/Sonidos/Start_Timestop";
        private const string EndSound = "Jojo/Content/Sonidos/End_Timestop";

        /// <summary>
        /// Devuelve el mensaje traducido al idioma del cliente actual.
        /// </summary>
        public static string GetTimeManipulatedText()
        {
            string culture = Language.ActiveCulture.Name;

            if (culture.StartsWith("es"))
                return "El tiempo ya está siendo manipulado...";
            if (culture.StartsWith("zh"))
                return "时间已经被操控了...";
            if (culture.StartsWith("fr"))
                return "Le temps est déjà manipulé...";
            if (culture.StartsWith("ru"))
                return "Время уже контролируется...";
            if (culture.StartsWith("pt"))
                return "O tempo já está sendo manipulado...";

            // Inglés por defecto
            return "Time is already being manipulated...";
        }

        public static void Use(Player player)
        {
            if (TimeStopSystem.timeStopped)
                return;

            // 🆕 Si King Crimson sigue manipulando el tiempo, se muestra el mensaje en amarillo y no se ejecuta.
            if (TimeEraseNetHandler.IsTimeManipulationActive())
            {
                Main.NewText(GetTimeManipulatedText(), Color.Yellow);
                return;
            }

            // Sonido de inicio
            SoundEngine.PlaySound(new SoundStyle(StartSound));

            // Configurar variables locales del cliente actual
            TimeStopSystem.endSoundPath = EndSound;
            TimeStopSystem.timeStopped = true;
            TimeStopSystem.owner = player.whoAmI;

            player.AddBuff(ModContent.BuffType<TimeStoped>(), Duration);

            // Avisar inmediatamente al servidor para congelar el mundo de todos
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = ModContent.GetInstance<Jojo>().GetPacket();
                packet.Write(Jojo.PacketType_TimeStopSync);
                packet.Write(true);
                packet.Write(player.whoAmI);
                packet.Write(EndSound);
                packet.Send();
            }
        }
    }
}