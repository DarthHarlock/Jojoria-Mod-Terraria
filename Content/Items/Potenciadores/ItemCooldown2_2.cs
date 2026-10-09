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
    public class ItemCooldown2_2 : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Registra la animación vertical de 6 frames (6 ticks por frame)
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 6));
        }

        public override void SetDefaults()
        {
            // Atributos originales conservados
            Item.width = 24;
            Item.height = 28;
            Item.accessory = true;
            Item.value = Item.sellPrice(0, 5, 0, 0);
            Item.rare = Terraria.ID.ItemRarityID.Orange;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // Suma un 80% de reducción EXCLUSIVA para el Cooldown 2 (INTACTO)
            player.GetModPlayer<StandStatsPlayer>().standCooldown2Reduction += 0.20f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<ItemCooldown2_1>(), 1)
                .AddIngredient(ModContent.ItemType<LingoteMeteoritoJojo>(), 15)
                .AddIngredient(ItemID.LargeTopaz, 1)
                .AddIngredient(ModContent.ItemType<Voluntad_Conquistador>(), 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

        // Este método obliga a Terraria a reproducir la animación fotograma a fotograma mientras cae o descansa en el suelo
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = TextureAssets.Item[Item.type].Value;

            // Obtiene el cuadro (frame) actual según la animación registrada
            Rectangle frame = Main.itemAnimations[Item.type].GetFrame(texture);

            // Centra el renderizado en la posición física del objeto en el mundo (se adapta dinámicamente al width 24 y height 28)
            Vector2 position = Item.position - Main.screenPosition + new Vector2(Item.width / 2f, Item.height / 2f);
            Vector2 origin = frame.Size() / 2f;

            spriteBatch.Draw(texture, position, frame, lightColor, rotation, origin, scale, SpriteEffects.None, 0f);

            // Retornamos false para cancelar el dibujado estático por defecto de Terraria
            return false;
        }
    }
}