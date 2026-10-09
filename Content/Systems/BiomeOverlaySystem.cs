using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Systems
{
    public class BiomeOverlaySystem : ModSystem
    {
        public static bool DrawOverlay = false;
        private static Asset<Texture2D> _texture;

        private const int TEX_W = 100;
        private const int TEX_H = 100;
        private const float PARALLAX = 0f;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                _texture = ModContent.Request<Texture2D>(
                    "Jojo/Content/Backgrounds/CustomBiomeBg",
                    AssetRequestMode.ImmediateLoad
                );

                On_Main.DrawBackground += DrawCustomBackground;
            }
        }

        public override void Unload()
        {
            if (!Main.dedServ)
                On_Main.DrawBackground -= DrawCustomBackground;

            _texture = null;
        }

        private static void DrawCustomBackground(On_Main.orig_DrawBackground orig, Main self)
        {
            orig(self);

            if (!DrawOverlay || _texture == null)
                return;

            SpriteBatch sb = Main.spriteBatch;
            Texture2D tex = _texture.Value;

            sb.End();

            sb.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Matrix.Identity
            );

            // 1. Mantenemos tu escala basada en la pantalla, pero aumentamos drásticamente el área de cobertura.
            float scale = (float)Main.screenHeight / TEX_H;

            // 2. Multiplicamos la altura por 3 para asegurar que el zoom máximo nunca revele bordes.
            int drawH = Main.screenHeight * 3;
            int drawW = (int)(TEX_W * scale);

            float rawOffset = (Main.screenPosition.X * PARALLAX);
            int offsetX = (int)(rawOffset % drawW);
            if (offsetX < 0) offsetX += drawW;

            int startX = -offsetX;
            // 3. Subimos el punto de inicio Y por encima de la pantalla para centrar este fondo gigante.
            int startY = -Main.screenHeight;

            // 4. Agregamos tiles extra por si el zoom horizontal también muestra bordes laterales.
            int tilesNeeded = (Main.screenWidth / drawW) + 5;

            // 5. Empezamos a dibujar desde más atrás en X por seguridad.
            for (int i = -2; i < tilesNeeded; i++)
            {
                Rectangle dest = new Rectangle(
                    startX + i * drawW,
                    startY,
                    drawW,
                    drawH
                );
                sb.Draw(tex, dest, Color.White);
            }

            sb.End();

            sb.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );
        }
    }
}