using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.SilverChariotRequiem_Buffs;


namespace Jojo.Content.Buffs
{
    public class DebuffUI_GlobalNPC : GlobalNPC
    {
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Lista de los 3 debufos que queremos mostrar
            int[] buffsObjetivo = {
                ModContent.BuffType<RequiemDebilidadDebuff>(),
                ModContent.BuffType<RequiemLentitudDebuff>(),
                ModContent.BuffType<RequiemSangradoDebuff>()
            };

            // Contamos cuántos de estos debufos tiene el enemigo para poder centrarlos
            int buffsActivos = 0;
            foreach (int buff in buffsObjetivo)
            {
                if (npc.HasBuff(buff))
                {
                    buffsActivos++;
                }
            }

            // Si no tiene ninguno de los 3 debufos, no dibujamos nada
            if (buffsActivos == 0) return;

            // Configuramos el espaciado y la posición para que floten encima del NPC
            int separacionIconos = 34; // Un icono de buff en Terraria suele ser de 32x32 píxeles
            float anchuraTotal = buffsActivos * separacionIconos;

            // Calculamos la posición inicial (encima de la cabeza del NPC)
            Vector2 posicionInicial = npc.Top - screenPos - new Vector2(anchuraTotal / 2f - (separacionIconos / 2f), 40f);
            posicionInicial.Y += npc.gfxOffY; // Ajuste por si el enemigo sube/baja escaleras

            // Dibujamos cada icono correspondiente
            int indiceActual = 0;
            foreach (int buff in buffsObjetivo)
            {
                if (npc.HasBuff(buff))
                {
                    // Obtenemos la textura oficial del debufo
                    Texture2D texturaDebufo = TextureAssets.Buff[buff].Value;

                    // Calculamos la posición exacta de este icono en la fila
                    Vector2 posicionDibujado = posicionInicial + new Vector2(indiceActual * separacionIconos, 0);

                    // Color blanco con 200 de transparencia (sobre 255) para que sea semitransparente
                    Color colorTransparente = new Color(255, 255, 255, 200);

                    spriteBatch.Draw(
                        texturaDebufo,
                        posicionDibujado,
                        null,
                        colorTransparente,
                        0f, // Sin rotación
                        new Vector2(texturaDebufo.Width / 2f, texturaDebufo.Height / 2f), // Centro del icono
                        1f, // Escala normal
                        SpriteEffects.None,
                        0f
                    );

                    indiceActual++;
                }
            }
        }
    }
}