using Terraria.ID;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem.Animales
{
    public class PajaroRequiem : AnimalStandRequiem
    {
        public override string Texture => "Terraria/Images/NPC_" + NPCID.Bird;

        protected override int NPCVanillaBase => NPCID.Bird;
        protected override float DañoBase => 10f;
        protected override float RadioDeteccion => 500f;
        protected override int TiempoVidaMaximo => 30 * 60; // 30 segundos

        // Los pájaros son los más "decididos" persiguiendo, sin dejar de
        // volar con su patrón vanilla errático de fondo.
        protected override float ImpulsoHaciaObjetivo => 0.06f;
    }
}