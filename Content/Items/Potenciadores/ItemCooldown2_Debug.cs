using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Players;

namespace Jojo.Content.Items.Potenciadores
{
    public class ItemCooldown2_Debug : ModItem
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
            player.GetModPlayer<StandStatsPlayer>().standCooldown2Reduction += 9.10f;
        }
    }
}