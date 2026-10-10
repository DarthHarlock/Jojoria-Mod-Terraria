using Terraria.ModLoader;

using Jojo.Content.Projectiles.Justice.Justice_Tier_4;

namespace Jojo.Content.Projectiles.Justice.Justice_Tier_3
{
    public class JUSTICESTAND_Tier_3 : JUSTICESTAND_Tier_4
    {
        // Mientras no tengas un .png propio para este proyectil, usa el del Tier 4.
        // Si creas "JUSTICESTAND_Tier_3.png" en esta carpeta, borra esta línea.
        public override string Texture => "Jojo/Content/Projectiles/Justice/Justice_Tier_4/JUSTICESTAND_Tier_4";

        // ================== CONFIGURACIÓN TIER 3 (un poco más débil) ==================
        public override float MultVida => 1f;                     // vida del enemigo al ser minion (1 = sin boost)
        public override float MultDano => 1f;                     // daño del minion (1 = daño base del enemigo)
        public override float MultDefensa => 1f;                  // defensa del minion (1 = defensa base)
        public override int CooldownGolpe => 60;                  // ticks entre golpes del minion (60 = 1 segundo)
        public override float RadioNiebla => 500f;                // tamaño de la niebla
        public override int TiempoInfeccion => 240;               // ticks dentro de la niebla para convertir a un enemigo (60 = 1 segundo)
        public override int DuracionMarca => 300;                 // ticks que dura la marca
        public override float DistanciaTeletransporte => 500f;    // si el minion se aleja más de esto, vuelve a ti al instante
        public override float RadioBusquedaObjetivo => 1300f;     // distancia a la que los minions buscan enemigos

        // ================== HABILIDADES (propias del Tier 3) ==================
        public override int CooldownHabilidad1 => 9000;            // cooldown del Aura Curativa (F), 60 = 1 segundo
        public override int CooldownHabilidad2 => 900;            // cooldown de Reorganizar (G)
        public override float CuraEficacia => 0.75f;              // eficacia de la curación (1 = la del Tier 4)
        public override float RangoCuracion => 0.8f;              // rango de la curación (1 = el del Tier 4)
    }
}