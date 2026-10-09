using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using System;

namespace Jojo.Content.Projectiles.CMoon.CMoon_Tier_Final // ¡Actualizado a la carpeta de C-Moon!
{
    public class GravedadCeroNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool enGravedadCero;
        private bool estadoAnterior;

        public override void ResetEffects(NPC npc)
        {
            enGravedadCero = false;
        }

        public override bool PreAI(NPC npc)
        {
            if (enGravedadCero && !npc.boss && !npc.dontTakeDamage)
            {
                if (!estadoAnterior)
                {
                    npc.velocity = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-6f, -3f));
                    estadoAnterior = true;
                }

                Vector2 velColision = Collision.TileCollision(npc.position, npc.velocity, npc.width, npc.height, false, false);

                if (velColision.X != npc.velocity.X) npc.velocity.X *= -0.8f;
                if (velColision.Y != npc.velocity.Y) npc.velocity.Y *= -0.8f;

                npc.position += npc.velocity;

                if (npc.velocity != Vector2.Zero)
                {
                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                }

                return false;
            }

            if (estadoAnterior && !enGravedadCero)
            {
                estadoAnterior = false;
                npc.rotation = 0f;
            }

            return base.PreAI(npc);
        }
    }
}