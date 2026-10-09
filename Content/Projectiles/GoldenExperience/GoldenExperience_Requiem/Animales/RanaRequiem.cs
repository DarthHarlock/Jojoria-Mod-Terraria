using System;
using Terraria;
using Terraria.ID;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem.Animales
{
    public class RanaRequiem : AnimalStandRequiem
    {
        public override string Texture => "Terraria/Images/NPC_" + NPCID.Frog;

        protected override int NPCVanillaBase => NPCID.Frog;
        protected override float DañoBase => 10f;
        protected override float RadioDeteccion => 350f;
        protected override int TiempoVidaMaximo => 30 * 60; // 30 segundos
        protected override float ImpulsoHaciaObjetivo => 0.4f;

        // La rana ya salta sola gracias a su IA vanilla (gravedad + hops
        // semi-aleatorios). Un empujón vectorial completo (como el de las
        // demás especies) le rompería el arco del salto, así que aquí solo
        // le damos un empujoncito HORIZONTAL hacia la presa, dejando que la
        // física vertical la siga manejando la IA clonada.
        protected override void OrientarHaciaObjetivo()
        {
            NPC objetivo = BuscarEnemigoCercano();
            if (objetivo == null) return;

            float dirX = objetivo.Center.X - NPC.Center.X;
            if (dirX == 0f) return;

            NPC.velocity.X += Math.Sign(dirX) * ImpulsoHaciaObjetivo;
        }
    }
}