using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
// Asumo que RangoStandPlayer está en este namespace, igual que StandStatsPlayer
using Jojo.Content.Players;

namespace Jojo.Content.Items.Potenciadores
{
    public class ItemMovilidad2 : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Registra la animación vertical de 6 frames (6 ticks por frame)
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 6));
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true; // Define que es un accesorio
            Item.value = Item.sellPrice(gold: 15); // Le subí el valor por ser mejorado
            Item.rare = ItemRarityID.LightRed;    // Rareza más alta
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // --- CONFIGURACIÓN DE PORCENTAJE ---
            // +200% de rango para esta versión mejorada
            float porcentajeAumento = 20f;

            if (player.TryGetModPlayer(out RangoStandPlayer modPlayer))
            {
                modPlayer.multiplicadorRango += porcentajeAumento / 100f;
            }
        }

        // Este método obliga a Terraria a reproducir la animación fotograma a fotograma mientras cae o descansa en el suelo
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = TextureAssets.Item[Item.type].Value;

            // Obtiene el cuadro (frame) actual según la animación registrada
            Rectangle frame = Main.itemAnimations[Item.type].GetFrame(texture);

            // Centra el renderizado en la posición física del objeto en el mundo
            Vector2 position = Item.position - Main.screenPosition + new Vector2(Item.width / 2f, Item.height / 2f);
            Vector2 origin = frame.Size() / 2f;

            spriteBatch.Draw(texture, position, frame, lightColor, rotation, origin, scale, SpriteEffects.None, 0f);

            // Retornamos false para cancelar el dibujado estático por defecto de Terraria
            return false;
        }

        // --- RECETA AÑADIDA ---
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<ItemMovilidad1>()) // El anterior anillo
                .AddIngredient(ItemID.HallowedBar, 10)                // 10 barras de material sagrado
                .AddIngredient(ItemID.SoulofFlight, 8)                // 8 almas de vuelo (corregido a SoulofFlight)
                .AddIngredient(ItemID.LargeAmethyst, 1)               // 1 amatista grande
                .AddTile(TileID.MythrilAnvil)                         // Yunque de Hardmode (Mithril/Oricalco)
                .Register();
        }
    }
}