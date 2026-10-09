using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Items.Tiles;

namespace Jojo.Content.Items
{
    public class MineralMeteoritoJojo : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 100;
        }

        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.autoReuse = true;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<MineralMeteoritoJojoTile>();
            Item.width = 12;
            Item.height = 12;
            Item.value = Item.sellPrice(silver: 2);
            Item.rare = ModContent.RarityType<Rarities.MineralStand>();
        }
    }
}