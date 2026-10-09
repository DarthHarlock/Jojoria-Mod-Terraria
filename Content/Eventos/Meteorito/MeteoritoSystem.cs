using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Jojo.Content.Eventos.Meteorito
{
    public class MeteoritoSystem : ModSystem
    {
        public static bool meteoritoCaido = false;

        public override void ClearWorld()
        {
            meteoritoCaido = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (meteoritoCaido)
            {
                tag["meteoritoCaido"] = true;
            }
        }

        public override void LoadWorldData(TagCompound tag)
        {
            meteoritoCaido = tag.GetBool("meteoritoCaido");
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(meteoritoCaido);
        }

        public override void NetReceive(BinaryReader reader)
        {
            meteoritoCaido = reader.ReadBoolean();
        }
    }
}