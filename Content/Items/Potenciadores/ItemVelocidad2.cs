using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Players;
using Jojo.Content.Rarities;

namespace Jojo.Content.Items.Potenciadores
{
    public class ItemVelocidad2 : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;

            Item.accessory = true;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ModContent.RarityType<MoradoAnubis>();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<StandStatsPlayer>().standSpeed += 100f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<ItemVelocidad1>(), 1)
                .AddIngredient(ItemID.HallowedBar, 15)
                .AddIngredient(ItemID.FallenStar, 10)
                .AddTile(TileID.Loom)
                .Register();
        }
    }
}