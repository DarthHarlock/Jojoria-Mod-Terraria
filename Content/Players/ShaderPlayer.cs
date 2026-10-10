using Terraria;
using Terraria.ModLoader;
using Terraria.Graphics.Effects;
using Jojo.Content.Systems;

namespace Jojo.Content.Players
{
    public class ShaderPlayer : ModPlayer
    {
        public bool shaderActivo;
        public int teleportFlash;

        private const string ShaderName = "Jojo:TimeStop";

        public override void PostUpdate()
        {
            // Las pantallas y filtros no se ejecutan en servidores dedicados
            if (Main.dedServ)
                return;

            // El filtro visual solo debe configurarse basándose en el jugador local de esta PC
            if (Player.whoAmI != Main.myPlayer)
                return;

            bool debeActivarShader = TimeStopSystem.timeStopped || (teleportFlash > 0);

            if (debeActivarShader)
            {
                shaderActivo = true;

                if (!Filters.Scene[ShaderName].IsActive())
                    Filters.Scene.Activate(ShaderName);

                var shader = Filters.Scene[ShaderName].GetShader();

                if (teleportFlash > 0)
                {
                    teleportFlash--;
                    float t = teleportFlash / 10f;
                    if (t > 1f) t = 1f;

                    shader?.UseOpacity(0.85f * t);
                }
                else
                {
                    shader?.UseOpacity(0.85f);
                }
            }
            else
            {
                shaderActivo = false;

                if (Filters.Scene[ShaderName].IsActive())
                    Filters.Scene[ShaderName].Deactivate();
            }
        }
    }
}