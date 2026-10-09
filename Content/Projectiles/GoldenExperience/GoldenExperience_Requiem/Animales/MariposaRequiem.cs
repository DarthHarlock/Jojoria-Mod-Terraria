using Terraria.ID;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem.Animales
{
    public class MariposaRequiem : AnimalStandRequiem
    {
        public override string Texture => "Terraria/Images/NPC_" + NPCID.Butterfly;

        protected override int NPCVanillaBase => NPCID.Butterfly;
        protected override float DañoBase => 10f;
        protected override float RadioDeteccion => 450f;
        protected override int TiempoVidaMaximo => 30 * 60; // 30 segundos
        protected override float ImpulsoHaciaObjetivo => 0.045f;

        // Sin overrides de movimiento: usa el empuje vectorial suave por
        // defecto de la base, que combinado con el aleteo errático vanilla
        // de Butterfly da el mismo efecto "revoloteando hacia la presa" que
        // ya tenía, pero sin cancelar su vuelo natural.
    }
}