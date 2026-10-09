using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace Jojo.Content.Items
{
    public class DetonadorMejorado : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.value = Item.sellPrice(gold: 8);
            Item.rare = ItemRarityID.Orange;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (player.TryGetModPlayer(out BombaDamagePlayer modPlayer))
            {
                // Sumamos 0.50f (50%) al daño de la bomba. 
                // Si pones otro accesorio en el futuro, usará += con su propio valor y se sumarán perfectamente.
                modPlayer.bombaDamageMult += 0.50f;
            }
        }
    }

    // Sistema global para almacenar y acumular los porcentajes de daño de la bomba de manera aditiva
    public class BombaDamagePlayer : ModPlayer
    {
        public float bombaDamageMult = 0f;

        public override void ResetEffects()
        {
            // Resetea a 0 en cada frame para recalcular según los accesorios equipados actualmente
            bombaDamageMult = 0f;
        }
    }
}