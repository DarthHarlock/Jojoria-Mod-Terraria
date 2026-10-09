using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Jojo.Content.Systems
{
    public class RecipeGroups : ModSystem
    {
        public override void AddRecipeGroups()
        {
            // --- GRUPO 1: MARIPOSAS ---
            RecipeGroup butterflyGroup = new RecipeGroup(
                () => $"{Language.GetTextValue("LegacyMisc.37")} Mariposa",
                ItemID.JuliaButterfly,
                ItemID.MonarchButterfly,
                ItemID.PurpleEmperorButterfly,
                ItemID.RedAdmiralButterfly,
                ItemID.SulphurButterfly,
                ItemID.TreeNymphButterfly,
                ItemID.UlyssesButterfly,
                ItemID.ZebraSwallowtailButterfly
            );
            RecipeGroup.RegisterGroup("Jojo:AnyButterfly", butterflyGroup);

            // --- GRUPO 2: COBALTO O PALADIO ---
            RecipeGroup cobaltPalladiumGroup = new RecipeGroup(
                () => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.CobaltBar)}",
                ItemID.CobaltBar,
                ItemID.PalladiumBar
            );
            RecipeGroup.RegisterGroup("Jojo:CobaltOrPalladium", cobaltPalladiumGroup);

            // --- GRUPO 3: DEMONÍACO O CARMESÍ ---
            RecipeGroup demoniteCrimtaneGroup = new RecipeGroup(
                () => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.DemoniteBar)}",
                ItemID.DemoniteBar,
                ItemID.CrimtaneBar
            );
            RecipeGroup.RegisterGroup("Jojo:DemoniteOrCrimtane", demoniteCrimtaneGroup);

            // --- GRUPO 4: ORO O PLATINO ---
            RecipeGroup goldPlatinumGroup = new RecipeGroup(
                () => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.GoldBar)}",
                ItemID.GoldBar,
                ItemID.PlatinumBar
            );
            RecipeGroup.RegisterGroup("Jojo:GoldOrPlatinum", goldPlatinumGroup);

            // --- GRUPO 5: TITANIO O ADAMANTITA ---
            RecipeGroup titaniumAdamantiteGroup = new RecipeGroup(
                () => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.AdamantiteBar)}",
                ItemID.AdamantiteBar,
                ItemID.TitaniumBar
            );
            RecipeGroup.RegisterGroup("Jojo:TitaniumOrAdamantite", titaniumAdamantiteGroup);

            // --- GRUPO 6: HIERRO O PLOMO ---
            RecipeGroup ironLeadGroup = new RecipeGroup(
                () => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.IronBar)}",
                ItemID.IronBar,
                ItemID.LeadBar
            );
            RecipeGroup.RegisterGroup("Jojo:IronOrLead", ironLeadGroup);

            // --- GRUPO 7: PLATA O TUNGSTENO ---
            RecipeGroup silverTungstenGroup = new RecipeGroup(
                () => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.SilverBar)}",
                ItemID.SilverBar,
                ItemID.TungstenBar
            );
            RecipeGroup.RegisterGroup("Jojo:SilverOrTungsten", silverTungstenGroup);

            // --- GRUPO 8: RELOJ DE ORO O PLATINO (NUEVO) ---
            RecipeGroup goldPlatinumWatchGroup = new RecipeGroup(
                () => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.GoldWatch)}",
                ItemID.GoldWatch,
                ItemID.PlatinumWatch
            );
            RecipeGroup.RegisterGroup("Jojo:GoldOrPlatinumWatch", goldPlatinumWatchGroup);
        }
    }
}