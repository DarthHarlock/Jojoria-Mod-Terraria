using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using Jojo.Content.Habilidades.Cinderella.Cinderella_Tier_4.HabilidadG;

namespace Jojo.Content.Habilidades.Cinderella.Cinderella_Tier_4.ArmarioHabilida
{
    public class ArmarioUIState : UIState
    {
        UIPanel panel;
        UIText textoPelo;
        UIText textoGenero;
        UIText titulo;

        private Vector2 offset;
        private bool dragging;

        public override void OnInitialize()
        {
            panel = new UIPanel();
            panel.Width.Set(450f, 0f); // Ligeramente más ancho para acomodar el ojo
            panel.Height.Set(420f, 0f);

            panel.Left.Set(Main.screenWidth * 0.55f, 0f);
            panel.Top.Set(Main.screenHeight / 2f - 210f, 0f);
            panel.BackgroundColor = new Color(35, 35, 55, 235);
            Append(panel);

            titulo = new UIText(Traducciones.Titulo, 0.85f, true);
            titulo.HAlign = 0.5f;
            titulo.Top.Set(8f, 0f);
            panel.Append(titulo);

            var cerrar = new UIText("[X]", 0.9f);
            cerrar.Left.Set(-30f, 1f);
            cerrar.Top.Set(8f, 0f);
            cerrar.OnLeftClick += (evt, el) => ArmarioUISystem.Cerrar();
            panel.Append(cerrar);

            float y = 42f;

            // --- Género ---
            textoGenero = new UIText(Traducciones.Genero + ": " + Traducciones.Masculino, 0.85f);
            textoGenero.Left.Set(14f, 0f);
            textoGenero.Top.Set(y, 0f);
            panel.Append(textoGenero);

            var botonGenero = new UIText(Traducciones.Cambiar, 0.8f);
            botonGenero.Left.Set(180f, 0f);
            botonGenero.Top.Set(y, 0f);
            botonGenero.OnLeftClick += (evt, el) =>
            {
                Player jugador = Main.LocalPlayer;
                jugador.Male = !jugador.Male;
                ActualizarTextos(jugador);
                Sincronizar(jugador);
                SoundEngine.PlaySound(SoundID.MenuTick);
            };
            panel.Append(botonGenero);

            // --- BOTÓN OJO (ARMADURA) ---
            var ojoArmadura = new UIEyeButton();
            ojoArmadura.Left.Set(290f, 0f);
            ojoArmadura.Top.Set(y - 2f, 0f);
            panel.Append(ojoArmadura);

            var textoOjo = new UIText(Traducciones.Armadura, 0.8f);
            textoOjo.Left.Set(320f, 0f);
            textoOjo.Top.Set(y, 0f);
            panel.Append(textoOjo);

            y += 32f;

            // --- Estilo de pelo ---
            textoPelo = new UIText(Traducciones.Pelo + ": 0", 0.85f);
            textoPelo.Left.Set(14f, 0f);
            textoPelo.Top.Set(y + 8f, 0f);
            panel.Append(textoPelo);

            var flechaIzq = new UIText("<", 1f, true);
            flechaIzq.Left.Set(180f, 0f);
            flechaIzq.Top.Set(y + 6f, 0f);
            flechaIzq.OnLeftClick += (evt, el) => CambiarPelo(-1);
            panel.Append(flechaIzq);

            var peloPreview = new UIHairPreview();
            peloPreview.Left.Set(210f, 0f);
            peloPreview.Top.Set(y - 2f, 0f);
            panel.Append(peloPreview);

            var flechaDer = new UIText(">", 1f, true);
            flechaDer.Left.Set(260f, 0f);
            flechaDer.Top.Set(y + 6f, 0f);
            flechaDer.OnLeftClick += (evt, el) => CambiarPelo(1);
            panel.Append(flechaDer);

            y += 44f;

            // --- Filas de color ---
            y = AgregarFilaColorBarra(Traducciones.ColorPelo, y, (jugador, c) => jugador.hairColor = c, () => Main.LocalPlayer.hairColor);
            y = AgregarFilaColorBarra(Traducciones.ColorPiel, y, (jugador, c) => jugador.skinColor = c, () => Main.LocalPlayer.skinColor);
            y = AgregarFilaColorBarra(Traducciones.ColorOjos, y, (jugador, c) => jugador.eyeColor = c, () => Main.LocalPlayer.eyeColor);
            y = AgregarFilaColorBarra(Traducciones.ColorCamisa, y, (jugador, c) => jugador.shirtColor = c, () => Main.LocalPlayer.shirtColor);
            y = AgregarFilaColorBarra(Traducciones.ColorTorso, y, (jugador, c) => jugador.underShirtColor = c, () => Main.LocalPlayer.underShirtColor);
            y = AgregarFilaColorBarra(Traducciones.ColorPantalon, y, (jugador, c) => jugador.pantsColor = c, () => Main.LocalPlayer.pantsColor);
            y = AgregarFilaColorBarra(Traducciones.ColorZapatos, y, (jugador, c) => jugador.shoeColor = c, () => Main.LocalPlayer.shoeColor);
        }

        float AgregarFilaColorBarra(string etiqueta, float top, Action<Player, Color> aplicar, Func<Color> obtenerActual)
        {
            var texto = new UIText(etiqueta, 0.8f);
            texto.Left.Set(14f, 0f);
            texto.Top.Set(top + 2f, 0f);
            panel.Append(texto);

            var barra = new ColorBarSlider(aplicar);
            barra.Left.Set(145f, 0f);
            barra.Top.Set(top, 0f);
            panel.Append(barra);

            var preview = new ColorSwatchPreview(obtenerActual);
            preview.Left.Set(380f, 0f);
            preview.Top.Set(top, 0f);
            panel.Append(preview);

            return top + 28f;
        }

        void CambiarPelo(int direccion)
        {
            Player jugador = Main.LocalPlayer;
            jugador.hair = (jugador.hair + direccion + HairID.Count) % HairID.Count;

            ActualizarTextos(jugador);
            Sincronizar(jugador);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        public void Refrescar(Player jugador)
        {
            ActualizarTextos(jugador);
        }

        void ActualizarTextos(Player jugador)
        {
            textoPelo?.SetText(Traducciones.Pelo + ": " + jugador.hair);
            textoGenero?.SetText(Traducciones.Genero + ": " + (jugador.Male ? Traducciones.Masculino : Traducciones.Femenino));
        }

        internal static void Sincronizar(Player jugador)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, jugador.whoAmI);
            }
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            if (evt.Target == panel || evt.Target == titulo)
            {
                dragging = true;
                offset = new Vector2(evt.MousePosition.X - panel.Left.Pixels, evt.MousePosition.Y - panel.Top.Pixels);
            }
        }

        public override void LeftMouseUp(UIMouseEvent evt)
        {
            base.LeftMouseUp(evt);
            dragging = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (dragging)
            {
                panel.Left.Set(Main.mouseX - offset.X, 0f);
                panel.Top.Set(Main.mouseY - offset.Y, 0f);
                panel.Recalculate();
            }
        }

        // --- BOTÓN DEL OJO VISUAL DE TERRARIA ---
        class UIEyeButton : UIElement
        {
            public UIEyeButton()
            {
                Width.Set(24f, 0f);
                Height.Set(24f, 0f);
            }

            public override void LeftClick(UIMouseEvent evt)
            {
                base.LeftClick(evt);
                Player jugador = Main.LocalPlayer;

                // Solo se puede ocultar la armadura si Cinderella está equipada en la ranura de stand
                if (!CinderellaGPlayer.TieneCinderellaEnRanura(jugador))
                    return;

                var modPlayer = jugador.GetModPlayer<CinderellaGPlayer>();

                // Invierte el valor
                modPlayer.ocultarArmadura = !modPlayer.ocultarArmadura;

                // Sincroniza el cambio a todos los jugadores
                modPlayer.SyncArmadura();

                SoundEngine.PlaySound(SoundID.MenuTick);
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                CalculatedStyle dims = GetDimensions();
                Player jugador = Main.LocalPlayer;

                bool oculto = jugador.GetModPlayer<CinderellaGPlayer>().ocultarArmadura;
                bool disponible = CinderellaGPlayer.TieneCinderellaEnRanura(jugador);

                // Usa la textura oficial de Terraria para visibilidad (TickOn = Visible, TickOff = Oculto)
                Texture2D tex = oculto ? TextureAssets.InventoryTickOff.Value : TextureAssets.InventoryTickOn.Value;

                // Atenuado si el stand no está en la ranura (el botón no hace nada)
                Color color = disponible ? Color.White : Color.White * 0.35f;

                spriteBatch.Draw(tex, new Vector2(dims.X, dims.Y), color);
            }
        }

        class UIHairPreview : UIElement
        {
            public UIHairPreview()
            {
                Width.Set(36f, 0f);
                Height.Set(36f, 0f);
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                CalculatedStyle dims = GetDimensions();
                Player jugador = Main.LocalPlayer;

                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Rectangle bgRect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
                spriteBatch.Draw(pixel, bgRect, new Color(20, 20, 35, 200));

                if (jugador.hair >= 0 && jugador.hair < TextureAssets.PlayerHair.Length)
                {
                    Texture2D hairTex = TextureAssets.PlayerHair[jugador.hair].Value;
                    if (hairTex != null)
                    {
                        int frameHeight = hairTex.Height / 14;
                        Rectangle sourceRect = new Rectangle(0, 0, hairTex.Width, frameHeight);
                        Vector2 center = new Vector2(dims.X + dims.Width / 2f, dims.Y + dims.Height / 2f);
                        Vector2 origin = new Vector2(hairTex.Width / 2f, frameHeight / 2f);

                        spriteBatch.Draw(
                            hairTex,
                            center,
                            sourceRect,
                            jugador.hairColor,
                            0f,
                            origin,
                            0.75f,
                            SpriteEffects.None,
                            0f
                        );
                    }
                }
            }
        }

        class ColorBarSlider : UIElement
        {
            readonly Action<Player, Color> aplicar;
            bool isDragging = false;
            float currentProgress = 0.5f;

            public ColorBarSlider(Action<Player, Color> aplicar)
            {
                this.aplicar = aplicar;
                Width.Set(225f, 0f);
                Height.Set(18f, 0f);
            }

            public override void LeftMouseDown(UIMouseEvent evt)
            {
                base.LeftMouseDown(evt);
                isDragging = true;
                ActualizarColor(evt.MousePosition.X);
            }

            public override void LeftMouseUp(UIMouseEvent evt)
            {
                base.LeftMouseUp(evt);
                isDragging = false;
                ArmarioUIState.Sincronizar(Main.LocalPlayer);
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                if (isDragging)
                {
                    ActualizarColor(Main.mouseX);
                }
            }

            void ActualizarColor(float mouseX)
            {
                CalculatedStyle dims = GetDimensions();
                float progress = MathHelper.Clamp((mouseX - dims.X) / dims.Width, 0f, 1f);
                currentProgress = progress;
                Color color = ObtenerColorDesdeProgreso(progress);

                Player jugador = Main.LocalPlayer;
                aplicar(jugador, color);
            }

            public static Color ObtenerColorDesdeProgreso(float t)
            {
                t = MathHelper.Clamp(t, 0f, 1f);
                if (t < 0.12f) return Color.Lerp(Color.Black, Color.White, t / 0.12f);
                else if (t < 0.25f)
                {
                    float factor = (t - 0.12f) / 0.13f;
                    if (factor < 0.33f) return Color.Lerp(new Color(60, 30, 15), new Color(140, 80, 40), factor / 0.33f);
                    else if (factor < 0.66f) return Color.Lerp(new Color(140, 80, 40), new Color(220, 160, 120), (factor - 0.33f) / 0.33f);
                    else return Color.Lerp(new Color(220, 160, 120), new Color(255, 224, 189), (factor - 0.66f) / 0.34f);
                }
                else return Main.hslToRgb((t - 0.25f) / 0.75f, 1f, 0.5f);
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                CalculatedStyle dims = GetDimensions();
                Texture2D pixel = TextureAssets.MagicPixel.Value;

                Rectangle border = new Rectangle((int)dims.X - 1, (int)dims.Y - 1, (int)dims.Width + 2, (int)dims.Height + 2);
                spriteBatch.Draw(pixel, border, Color.Black);

                int w = (int)dims.Width;
                int h = (int)dims.Height;
                for (int x = 0; x < w; x++)
                {
                    float t = (float)x / w;
                    Color col = ObtenerColorDesdeProgreso(t);
                    Rectangle rect = new Rectangle((int)dims.X + x, (int)dims.Y, 1, h);
                    spriteBatch.Draw(pixel, rect, col);
                }

                int cursorX = (int)(dims.X + currentProgress * dims.Width);
                Rectangle cursorBorder = new Rectangle(cursorX - 2, (int)dims.Y - 2, 4, h + 4);
                spriteBatch.Draw(pixel, cursorBorder, Color.White);
                Rectangle cursorInner = new Rectangle(cursorX - 1, (int)dims.Y - 1, 2, h + 2);
                spriteBatch.Draw(pixel, cursorInner, Color.Black);
            }
        }

        class ColorSwatchPreview : UIElement
        {
            readonly Func<Color> obtenerColor;

            public ColorSwatchPreview(Func<Color> obtenerColor)
            {
                this.obtenerColor = obtenerColor;
                Width.Set(18f, 0f);
                Height.Set(18f, 0f);
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                CalculatedStyle dims = GetDimensions();
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Rectangle rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
                Rectangle border = new Rectangle(rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2);

                spriteBatch.Draw(pixel, border, Color.White);
                spriteBatch.Draw(pixel, rect, obtenerColor());
            }
        }
    }
}