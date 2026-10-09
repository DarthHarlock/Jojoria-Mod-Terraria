using Terraria.ModLoader;

namespace Jojo.Content
{
    public class JojoKeybinds : ModSystem
    {
        public static ModKeybind SummonStand;
        public static ModKeybind ToggleAuto;
        public static ModKeybind SkillH;
        public static ModKeybind SkillG;
        public static ModKeybind SkillF;

        public override void Load()
        {
            SummonStand = KeybindLoader.RegisterKeybind(Mod, "Summon Stand", "N");
            ToggleAuto = KeybindLoader.RegisterKeybind(Mod, "Toggle Auto Mode", "B");

            SkillH = KeybindLoader.RegisterKeybind(Mod, "Stand Skill H", "H");
            SkillG = KeybindLoader.RegisterKeybind(Mod, "Stand Skill G", "G");
            SkillF = KeybindLoader.RegisterKeybind(Mod, "Stand Skill F", "F");
        }

        public override void Unload()
        {
            SummonStand = null;
            ToggleAuto = null;
            SkillH = null;
            SkillG = null;
            SkillF = null;
        }

        // 🔥 FIX IMPORTANTE: fuerza acceso seguro
        public static bool Ready =>
            SummonStand != null &&
            ToggleAuto != null &&
            SkillH != null &&
            SkillG != null &&
            SkillF != null;
    }
}