using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Jojo.Content.Items; // Importante para detectar LingoteMeteoritoJojo

namespace Jojo.Content.Items.Potenciadores
{
    public class ItemMovilidad1 : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true; // Define que es un accesorio
            Item.value = Item.sellPrice(gold: 3);
            Item.rare = ItemRarityID.Green;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // --- CONFIGURACIÓN DE PORCENTAJE ---
            // Cambia este número directamente para alterar el rango otorgado (ej: 50f = +50%)
            float porcentajeAumento = 10f;

            if (player.TryGetModPlayer(out RangoStandPlayer modPlayer))
            {
                modPlayer.multiplicadorRango += porcentajeAumento / 100f;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<LingoteMeteoritoJojo>(), 5)
                .AddRecipeGroup("Jojo:CobaltOrPalladium", 8)
                .AddIngredient(ItemID.Amethyst, 10)
                .AddTile(TileID.Anvils) // Yunque normal (Hierro/Plomo)
                .Register();
        }
    }

    // Un ModPlayer simple encargado de almacenar el bonificador de rango para los Stands
    public class RangoStandPlayer : ModPlayer
    {
        public float multiplicadorRango = 1f;

        public override void ResetEffects()
        {
            // Se resetea a 1f (100% de rango base) cada frame
            multiplicadorRango = 1f;
        }
    }
}