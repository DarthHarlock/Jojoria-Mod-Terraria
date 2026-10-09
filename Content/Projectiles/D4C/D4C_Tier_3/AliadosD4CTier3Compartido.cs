using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace Jojo.Content.Projectiles.D4C.D4C_Tier_3
{
    
    public static class AliadosD4CTier3Compartido
    {
        public static bool EsObjetivoValido(NPC propio, NPC n)
        {
            if (!n.active || n.friendly || n.life <= 0 || n.dontTakeDamage) return false;
            if (n.catchItem > 0 || NPCID.Sets.CountsAsCritter[n.type] || n.lifeMax <= 5) return false;
            if (propio != null && n.whoAmI == propio.whoAmI) return false;
            return true;
        }

        
        public static void MoverCaminando(
            NPC npc,
            Vector2 diff,
            bool hayObjetivo,
            float velocidadCaminar,
            float maxBloquesSalto,
            float fuerzaSaltoMinimo,
            float fuerzaSaltoMaximo,
            ref bool enElSuelo)
        {
            npc.noGravity = false;
            npc.noTileCollide = false;

            enElSuelo = npc.velocity.Y == 0f;

            bool cercaDelPunto = !hayObjetivo && diff.Length() < 80f;
            float dirX = cercaDelPunto ? 0 : (Math.Abs(diff.X) < 2f ? 0 : Math.Sign(diff.X));

            if (dirX == 0)
                npc.velocity.X *= 0.8f;
            else
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, dirX * velocidadCaminar, 0.2f);

            if (!enElSuelo) return;

            bool objetivoAbajo = diff.Y > 48f && !cercaDelPunto;
            if (objetivoAbajo)
            {
                bool puedeBajar = true;
                int startX = (int)(npc.position.X / 16f);
                int endX = (int)((npc.position.X + npc.width) / 16f);
                int y = (int)((npc.position.Y + npc.height + 4f) / 16f);

                for (int x = startX; x <= endX; x++)
                {
                    Tile t = Framing.GetTileSafely(x, y);
                    if (t.HasTile && Main.tileSolid[t.TileType] && !TileID.Sets.Platforms[t.TileType])
                    {
                        puedeBajar = false;
                        break;
                    }
                }

                if (puedeBajar)
                {
                    npc.position.Y += 6f;
                    return;
                }
            }

            bool objetivoArriba = diff.Y < -32f && !cercaDelPunto;
            bool bloqueadoEnX = dirX != 0 && Math.Abs(npc.velocity.X) < 0.5f;

            bool obstaculoAdelante = dirX != 0 && Collision.SolidCollision(
                npc.position + new Vector2(dirX * 16f, -8f), npc.width, npc.height);

            if (objetivoArriba || bloqueadoEnX || obstaculoAdelante)
            {
                float alturaDeseada = 0f;

                if (objetivoArriba)
                    alturaDeseada = -diff.Y + 32f;

                if (bloqueadoEnX || obstaculoAdelante)
                    alturaDeseada = Math.Max(alturaDeseada, 64f);

                if (alturaDeseada > 0)
                {
                    float topeSaltoPixeles = maxBloquesSalto * 16f;
                    alturaDeseada = MathHelper.Clamp(alturaDeseada, 16f, topeSaltoPixeles);
                    const float gravedadAprox = 0.4f;
                    float velocidadSalto = -(float)Math.Sqrt(2f * gravedadAprox * alturaDeseada);

                    npc.velocity.Y = MathHelper.Clamp(velocidadSalto, fuerzaSaltoMaximo, fuerzaSaltoMinimo);
                }
            }
        }

        public static void CopiarApariencia(Player origen, Player destino)
        {
            destino.skinVariant = origen.skinVariant;
            destino.hair = origen.hair;
            destino.hairColor = origen.hairColor;
            destino.skinColor = origen.skinColor;
            destino.eyeColor = origen.eyeColor;
            destino.shirtColor = origen.shirtColor;
            destino.underShirtColor = origen.underShirtColor;
            destino.pantsColor = origen.pantsColor;
            destino.shoeColor = origen.shoeColor;
            destino.name = origen.name;
            destino.Male = origen.Male;

            destino.head = origen.head;
            destino.body = origen.body;
            destino.legs = origen.legs;

            for (int i = 0; i < origen.armor.Length; i++)
            {
                if (destino.armor.Length > i && origen.armor[i] != null)
                    destino.armor[i] = origen.armor[i].Clone();
            }

            for (int i = 0; i < origen.dye.Length; i++)
            {
                if (destino.dye.Length > i && origen.dye[i] != null)
                    destino.dye[i] = origen.dye[i].Clone();
            }

            for (int i = 0; i < origen.hideVisibleAccessory.Length; i++)
            {
                if (destino.hideVisibleAccessory.Length > i)
                    destino.hideVisibleAccessory[i] = origen.hideVisibleAccessory[i];
            }
        }
    }
}