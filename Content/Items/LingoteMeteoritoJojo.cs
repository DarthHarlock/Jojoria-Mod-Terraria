using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Items
{
    public class LingoteMeteoritoJojo : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 25;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = Item.sellPrice(silver: 10);

            // Corregido: Vuelve a usar tu rareza personalizada "MineralStand"
            Item.rare = ModContent.RarityType<Rarities.MineralStand>();

            // Hacemos que el lingote sea considerado un material
            Item.material = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<MineralMeteoritoJojo>(), 3)
                .AddTile(TileID.Hellforge)
                .Register();
        }
    }
}