using System.ComponentModel;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace Jojo.Content.Systems
{
    public class JojoConfig : ModConfig
    {
        // ClientSide: cada jugador tiene su propio valor. No se sincroniza ni afecta a los demás.
        public override ConfigScope Mode => ConfigScope.ClientSide;

        public static JojoConfig Instance;

        [Label("Volumen de Sonidos del Mod")]
        [Tooltip("30 = volumen normal (como suenan los WAV originales). 0 = silencio. 100 = máximo.")]
        [Range(0, 100)]
        [Increment(1)]
        [DefaultValue(30)]
        public int VolumenSonidos = 30;

        [Label("UI Transparency")]
        [Tooltip("Visibility of the mod's UI elements (such as Tusk's ammo bar). 0 = hidden, 50 = normal, 100 = brightest and most visible. Only affects you.")]
        [Range(0, 100)]
        [Increment(1)]
        [DefaultValue(50)]
        public int UITransparencia = 50;

        // 30 -> 1.0x (normal) | 0 -> 0.0x (silencio) | 100 -> ~3.33x
        [JsonIgnore]
        public float MultiplicadorVolumen => VolumenSonidos / 30f;

        // Opacidad: 0 -> 0.0 (invisible) | 50 -> 1.0 (normal) | 100 -> 1.0 (el resto lo da el realce)
        [JsonIgnore]
        public float MultiplicadorOpacidadUI => System.Math.Min(UITransparencia / 50f, 1f);

        // Realce visual: 0 hasta 50 -> 0.0 | 100 -> 1.0 (más brillo, más ancho y con borde)
        [JsonIgnore]
        public float RealceUI => System.Math.Max(0f, (UITransparencia - 50) / 50f);
    }

    public class JojoAudioSystem : ModSystem
    {
        public override void Load()
        {
            // Un servidor dedicado no reproduce audio, no hace falta el hook
            if (Main.dedServ)
                return;

            On_SoundEngine.PlaySound_refSoundStyle_Nullable1_SoundUpdateCallback += ControlarVolumenGlobalJojo;
        }

        private SlotId ControlarVolumenGlobalJojo(
            On_SoundEngine.orig_PlaySound_refSoundStyle_Nullable1_SoundUpdateCallback orig,
            ref SoundStyle style,
            Vector2? position,
            SoundUpdateCallback updateCallback)
        {
            if (style.SoundPath != null && style.SoundPath.StartsWith("Jojo/Content/Sonidos/"))
            {
                if (JojoConfig.Instance != null)
                {
                    float mult = JojoConfig.Instance.MultiplicadorVolumen;

                    // 0 = silencio absoluto: no se reproduce nada
                    if (mult <= 0f)
                        return SlotId.Invalid;

                    // Copia temporal del estilo con el volumen escalado
                    style = style with { Volume = style.Volume * mult };
                }
            }

            return orig(ref style, position, updateCallback);
        }
    }
}