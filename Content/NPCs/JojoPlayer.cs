using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Jojo
{
    public class JojoPlayer : ModPlayer
    {
        public bool haRecibidoFlecha = false;

        public override void SaveData(TagCompound tag)
        {
            tag["haRecibidoFlecha"] = haRecibidoFlecha;
        }

        public override void LoadData(TagCompound tag)
        {
            haRecibidoFlecha = tag.GetBool("haRecibidoFlecha");
        }
    }
}