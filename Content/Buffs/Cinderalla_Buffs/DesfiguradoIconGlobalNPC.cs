using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.Cinderalla_Buffs
{
    // Este único GlobalNPC hace TRES trabajos para "Desfigurado":
    // 1) Guarda, por NPC, la intensidad (defensa/velocidad) que le puso el stand
    //    que se lo aplicó, vía Aplicar(...).
    // 2) Dibuja el icono del buff sobre la cabeza del NPC mientras esté activo.
    // 3) Tiñe al NPC de un verde enfermizo mientras dure el debuff.
    // Cada stand (cualquier tier, presente o futuro) llama a Aplicar(...) con SUS
    // propios números de intensidad y duración. Ni este archivo ni el del debuff
    // hace falta tocarlos nunca más al añadir un tier nuevo.
    public class DesfiguradoIconGlobalNPC : GlobalNPC
    {
        // IMPRESCINDIBLE: sin esto, los valores de abajo se compartirían entre
        // TODOS los NPCs en vez de ser uno por enemigo.
        public override bool InstancePerEntity => true;

        public float multiplicadorDefensa = 1f;          // 1f = sin reducción de defensa
        public float porcentajeReduccionVelocidad = 0f;  // 0f = sin frenado

        /// <summary>
        /// Punto único de entrada para que CUALQUIER stand aplique "Desfigurado"
        /// con su propia intensidad y duración, sin tocar el debuff en sí.
        /// </summary>
        public static void Aplicar(NPC npc, int duracionTicks, float multiplicadorDefensa, float porcentajeReduccionVelocidad)
        {
            var stats = npc.GetGlobalNPC<DesfiguradoIconGlobalNPC>();
            stats.multiplicadorDefensa = multiplicadorDefensa;
            stats.porcentajeReduccionVelocidad = porcentajeReduccionVelocidad;

            npc.AddBuff(ModContent.BuffType<DesfiguradoDebuff>(), duracionTicks);
        }

        // Tiñe al NPC de un verde enfermizo mientras esté "Desfigurado"
        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            int buffType = ModContent.BuffType<DesfiguradoDebuff>();
            if (!npc.HasBuff(buffType)) return;

            drawColor = Color.Lerp(drawColor, new Color(90, 150, 70), 0.5f);
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            int buffType = ModContent.BuffType<DesfiguradoDebuff>();
            if (!npc.HasBuff(buffType)) return;

            Texture2D tex = TextureAssets.Buff[buffType].Value;
            Vector2 pos = new Vector2(npc.Center.X, npc.Top.Y - 22f) - screenPos;

            spriteBatch.Draw(
                tex,
                pos,
                null,
                Color.White,
                0f,
                new Vector2(tex.Width / 2f, tex.Height / 2f),
                1f,
                SpriteEffects.None,
                0f
            );
        }
    }
}