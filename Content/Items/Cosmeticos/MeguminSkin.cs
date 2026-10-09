using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
namespace Jojo.Content.Items.Cosmeticos
{
    public class MeguminSkin : ModItem
    {
        public override void Load()
        {
            EquipLoader.AddEquipTexture(Mod, "Jojo/Content/Items/Armor/Megumin_Head", EquipType.Head, this);
        }

        public override void SetStaticDefaults()
        {
            int equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            ArmorIDs.Head.Sets.DrawHead[equipSlot] = true;

            // ✔ Le dice al juego que es un sombrero alto (como el de mago) para que use el marco extendido de 60px y no lo mutile
            ArmorIDs.Head.Sets.IsTallHat[equipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;

            Item.maxStack = 1;

            // ✔ Precio correcto (5 oro)
            Item.value = 187500;

            Item.rare = ItemRarityID.Pink;

            Item.accessory = false;

            Item.vanity = true;

            Item.headSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {

        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Silk, 8)
                .AddIngredient(ItemID.Dynamite, 10)
                .AddTile(TileID.Loom)
                .Register();
        }
    }
}