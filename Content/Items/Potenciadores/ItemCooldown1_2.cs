using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Players;
using Jojo.Content.Items;

namespace Jojo.Content.Items.Potenciadores
{
    public class ItemCooldown1_2 : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Registra la animación vertical de 6 frames (6 ticks por frame)
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 6));
        }

        public override void SetDefaults()
        {
            // Medidas del hitbox ajustadas a un solo frame (32x32)
            Item.width = 32;
            Item.height = 32;

            Item.accessory = true;
            Item.value = Item.sellPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Orange;
            // No activamos ItemNoGravity para que caiga normalmente con gravedad
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // Reducción del 20% exclusiva para el Cooldown 1
            player.GetModPlayer<StandStatsPlayer>().standCooldown1Reduction += 0.20f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<ItemCooldown1_1>(), 1)
                .AddIngredient(ModContent.ItemType<LingoteMeteoritoJojo>(), 15)
                .AddIngredient(ItemID.LargeSapphire, 1)
                .AddIngredient(ModContent.ItemType<Voluntad_Bondadosa>(), 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
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
    }
}