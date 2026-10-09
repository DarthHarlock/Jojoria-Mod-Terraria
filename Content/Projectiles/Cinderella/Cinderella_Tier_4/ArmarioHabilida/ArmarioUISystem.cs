using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace Jojo.Content.Habilidades.Cinderella.Cinderella_Tier_4.ArmarioHabilida
{
    // Gestiona la interfaz del Armario: abrirla, cerrarla, actualizarla y dibujarla.
    public class ArmarioUISystem : ModSystem
    {
        internal UserInterface armarioInterface;
        internal ArmarioUIState armarioState;
        Idioma idiomaConstruido;

        public override void Load()
        {
            if (Main.dedServ) return; // El servidor dedicado no dibuja UI

            armarioState = new ArmarioUIState();
            armarioInterface = new UserInterface();
            idiomaConstruido = Traducciones.Actual;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            armarioInterface?.Update(gameTime);
        }

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            if (armarioInterface?.CurrentState != null)
            {
                armarioInterface.Draw(spriteBatch, new GameTime());
            }
        }

        // Abre el armario si está cerrado, lo cierra si está abierto
        public static void Toggle()
        {
            var sistema = ModContent.GetInstance<ArmarioUISystem>();
            if (sistema.armarioInterface.CurrentState == null)
            {
                // Si cambió el idioma, reconstruimos la UI para que use los textos nuevos
                if (sistema.idiomaConstruido != Traducciones.Actual)
                {
                    sistema.armarioState = new ArmarioUIState();
                    sistema.idiomaConstruido = Traducciones.Actual;
                }

                sistema.armarioInterface.SetState(sistema.armarioState);
                sistema.armarioState.Refrescar(Main.LocalPlayer);
            }
            else
            {
                sistema.armarioInterface.SetState(null);
            }
        }

        public static void Cerrar()
        {
            var sistema = ModContent.GetInstance<ArmarioUISystem>();
            sistema.armarioInterface.SetState(null);
        }
    }
}