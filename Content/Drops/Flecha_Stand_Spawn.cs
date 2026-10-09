using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Drops
{
    public class Flecha_Stand_Spawn : ModSystem
    {
        public override void PostWorldGen()
        {
            int itemAñadir = ModContent.ItemType<Items.Flecha_Stand>();

            for (int i = 0; i < Main.maxChests; i++)
            {
                Chest cofre = Main.chest[i];

                if (cofre != null && Main.tile[cofre.x, cofre.y].TileType == TileID.Containers)
                {
                    bool esCofreDePiramide = false;

                    for (int j = 0; j < 40; j++)
                    {
                        int tipoItem = cofre.item[j].type;

                        if (tipoItem == 934 || tipoItem == 935 || tipoItem == 857)
                        {
                            esCofreDePiramide = true;
                            break;
                        }
                    }

                    if (esCofreDePiramide)
                    {
                        for (int j = 0; j < 40; j++)
                        {
                            if (cofre.item[j].IsAir)
                            {
                                cofre.item[j].SetDefaults(itemAñadir);
                                cofre.item[j].stack = 1;
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}