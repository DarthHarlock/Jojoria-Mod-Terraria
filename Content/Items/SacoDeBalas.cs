using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Projectiles.Tusk.Tusk_Tier_1.Disparo;

namespace Jojo.Content.Items
{
    // Accesorio: las balas de Tusk se regeneran un 700% más rápido (x8).
    // Funciona con cualquier tier de Tusk, porque el efecto vive en TuskAimPlayer.
    // Necesita el sprite SacoDeBalas.png en esta misma carpeta (Items).
    public class SacoDeBalas : ModItem
    {
        // +700% de velocidad de regeneración => 1 + 7 = x8
        public const float VelocidadRegen = 8f;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.maxStack = 1;
            Item.value = Item.sellPrice(gold: 5);
            Item.rare = ItemRarityID.Pink;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            TuskAimPlayer tusk = player.GetModPlayer<TuskAimPlayer>();
            tusk.regenSpeed = System.Math.Max(tusk.regenSpeed, VelocidadRegen);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Leather, 5)
                .AddIngredient(ItemID.MusketBall, 50)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}