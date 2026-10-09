using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.SilverChariotRequiem_Buffs;


namespace Jojo.Content.Projectiles.SilverChariot.SilverChariot_Requiem
{
    public class ChariotRequiemVisualesGlobalNPC : GlobalNPC
    {
        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (npc.HasBuff(ModContent.BuffType<RequiemDebilidadDebuff>()))
            {
                // Fórmula de luminosidad estándar para un gris perfecto
                byte luminosidad = (byte)(drawColor.R * 0.299f + drawColor.G * 0.587f + drawColor.B * 0.114f);

                // Aplicamos el mismo valor a Rojo, Verde y Azul para forzar la escala de grises
                drawColor = new Color(luminosidad, luminosidad, luminosidad, drawColor.A);
            }
        }
    }
}