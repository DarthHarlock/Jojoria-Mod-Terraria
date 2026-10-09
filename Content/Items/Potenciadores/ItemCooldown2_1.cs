using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Players;
using Jojo.Content.Items;

namespace Jojo.Content.Items.Potenciadores
{
    public class ItemCooldown2_1 : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.accessory = true;
            Item.value = Item.sellPrice(0, 5, 0, 0);
            Item.rare = Terraria.ID.ItemRarityID.Orange;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // Suma un 80% de reducción EXCLUSIVA para el Cooldown 2
            player.GetModPlayer<StandStatsPlayer>().standCooldown2Reduction += 0.10f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<LingoteMeteoritoJojo>(), 10)
                .AddIngredient(ItemID.Topaz, 10)
                .AddTile(TileID.Hellforge)
                .Register();
        }
    }
}