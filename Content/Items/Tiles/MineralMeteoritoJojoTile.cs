using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace Jojo.Content.Items.Tiles
{
    public class MineralMeteoritoJojoTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            TileID.Sets.Ore[Type] = true;
            Main.tileSpelunker[Type] = true;
            Main.tileShine2[Type] = true;
            Main.tileShine[Type] = 975;
            Main.tileMergeDirt[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileLighted[Type] = true;

            // --- CORRECCIÓN DEL MAPA ---
            // CreateMapEntryName() genera la entrada automática para que lea el nombre de tu archivo de localización (.hjson)
            LocalizedText nombreMapa = CreateMapEntryName();
            AddMapEntry(new Color(250, 223, 125), nombreMapa);

            DustType = DustID.Gold; // Partículas doradas/amarillas al picar para que encajen con la luz
            HitSound = SoundID.Tink;
            MineResist = 3f;

            // Requisito estricto: 50% de poder de picado o superior
            MinPick = 50;
        }

        // Genera el brillo ambiental en el mundo con el color exacto que pediste
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            // Conversión de #fadf7d a valores de Terraria
            r = 0.98f;
            g = 0.87f;
            b = 0.49f;
        }
    }
}