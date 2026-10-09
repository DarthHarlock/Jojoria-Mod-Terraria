using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Players;
using Jojo.Content.Items;
using Jojo.Content.Rarities;

namespace Jojo.Content.Items.Potenciadores
{
    public class ItemVelocidad1 : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;

            Item.accessory = true;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ModContent.RarityType<RojoWeatherReport>();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<StandStatsPlayer>().standSpeed += 50f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddRecipeGroup("Jojo:CobaltOrPalladium", 5)
                .AddIngredient(ItemID.Silk, 8)
                .AddIngredient(ModContent.ItemType<LingoteMeteoritoJojo>(), 5)
                .AddTile(TileID.Loom)
                .Register();
        }
    }
}