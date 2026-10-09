using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Jojo.Content.Players;
using Jojo.Content.Rarities; // Importamos el espacio de nombres de tus rarezas personalizadas

namespace Jojo.Content.Items
{
    public class FundaAnubis : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true; // Define que es un accesorio
            Item.value = Item.sellPrice(gold: 5);
            Item.rare = ModContent.RarityType<AnubisSkinVerde>();
        }

        // Esta función se ejecuta constantemente mientras el accesorio está en una ranura funcional
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // Verificación para Anubis Tier 1
            if (player.TryGetModPlayer(out AnubisSoulPlayer_Tier_1 anubisPlayerTier1))
            {
                anubisPlayerTier1.fundaAnubisEquipada = true;
            }

            // Verificación para Anubis Tier 4
            if (player.TryGetModPlayer(out AnubisSoulPlayer_Tier_4 anubisPlayerTier4))
            {
                anubisPlayerTier4.fundaAnubisEquipada = true;
            }

            // Verificación para Anubis Tier 2
            if (player.TryGetModPlayer(out AnubisSoulPlayer_Tier_2 anubisPlayerTier2))
            {
                anubisPlayerTier2.fundaAnubisEquipada = true;
            }
        }

        // Receta: Forja infernal (o superior: la Forja de Adamantita/Titanio también vale)
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.AncientCloth, 20)                // Ropa antigua (ID 3794)
                .AddIngredient(ItemID.AncientBattleArmorMaterial, 1)   // Fragmento prohibido (ID 3783)
                .AddIngredient(ItemID.SoulofNight, 10)                 // Alma de noche (ID 521)
                .AddTile(TileID.Hellforge)                             // Forja infernal
                .Register();
        }
    }
}