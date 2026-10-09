using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_1;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_2;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_3;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4;

namespace Jojo.Systems
{
    /// <summary>
    /// Estadísticas compartidas por Dino Virus, Dinos transformados y Mini Dinos.
    /// Cada tier de Scary Monsters rellena este perfil con sus propios valores.
    /// </summary>
    public struct ScaryProfile
    {
        // Mini dinos
        public int MiniDinoDano;
        public float MiniDinoVelocidad;
        public float MiniDinoSalto;
        public float MiniDinoRangoDeteccion;
        public float MiniDinoDistanciaVuelo;

        // Dino virus y dinos transformados
        public float DinoVirusDuracionEfecto;
        public float DinoVirusTiempoTransformar;
        public float DinoVirusDuracionDino;
        public int TransformedDinoBonusVida;
        public int TransformedDinoDano;
        public float TransformedDinoVelocidad;
        public float TransformedDinoSalto;
    }

    /// <summary>
    /// Registro central de tiers de Scary Monsters.
    /// Para añadir un tier nuevo:
    ///   1) Añádelo en GetTier(), GetActiveTier(), GetStandType() y DesconvocarTodos()
    ///   2) Añade su case en GetProfile()
    /// </summary>
    public static class ScaryTiers
    {
        /// <summary>Devuelve el tier (1, 2, 3, 4...) de un tipo de proyectil de stand, o 0 si no es un stand de Scary Monsters.</summary>
        public static int GetTier(int projectileType)
        {
            if (projectileType == ModContent.ProjectileType<MONSTERSTAND_Tier_4>()) return 4;
            if (projectileType == ModContent.ProjectileType<MONSTERSTAND_Tier_3>()) return 3;
            if (projectileType == ModContent.ProjectileType<MONSTERSTAND_Tier_2>()) return 2;
            if (projectileType == ModContent.ProjectileType<MONSTERSTAND_Tier_1>()) return 1;
            return 0;
        }

        /// <summary>Tipo de proyectil del stand de un tier (útil para el ítem al invocar). -1 si no existe.</summary>
        public static int GetStandType(int tier)
        {
            switch (tier)
            {
                case 1: return ModContent.ProjectileType<MONSTERSTAND_Tier_1>();
                case 2: return ModContent.ProjectileType<MONSTERSTAND_Tier_2>();
                case 3: return ModContent.ProjectileType<MONSTERSTAND_Tier_3>();
                case 4: return ModContent.ProjectileType<MONSTERSTAND_Tier_4>();
                default: return -1;
            }
        }

        /// <summary>Tier de Scary Monsters que el jugador tiene activo ahora mismo (0 = ninguno). Prioriza el más alto.</summary>
        public static int GetActiveTier(Player p)
        {
            if (p == null || !p.active) return 0;

            if (p.ownedProjectileCounts[ModContent.ProjectileType<MONSTERSTAND_Tier_4>()] > 0) return 4;
            if (p.ownedProjectileCounts[ModContent.ProjectileType<MONSTERSTAND_Tier_3>()] > 0) return 3;
            if (p.ownedProjectileCounts[ModContent.ProjectileType<MONSTERSTAND_Tier_2>()] > 0) return 2;
            if (p.ownedProjectileCounts[ModContent.ProjectileType<MONSTERSTAND_Tier_1>()] > 0) return 1;
            return 0;
        }

        /// <summary>¿Tiene el jugador algún stand de Scary Monsters activo?</summary>
        public static bool StandActivo(Player p) => GetActiveTier(p) != 0;

        /// <summary>
        /// Desconvoca TODOS los stands de Scary Monsters del jugador (con su sonido y limpieza propia).
        /// Úsalo desde el ítem cuando el jugador ya tiene un stand activo.
        /// </summary>
        public static void DesconvocarTodos(Player p)
        {
            if (p == null) return;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (!pr.active || pr.owner != p.whoAmI) continue;

                if (pr.ModProjectile is MONSTERSTAND_Tier_4 t4) t4.StartDying();
                else if (pr.ModProjectile is MONSTERSTAND_Tier_3 t3) t3.StartDying();
                else if (pr.ModProjectile is MONSTERSTAND_Tier_2 t2) t2.StartDying();
                else if (pr.ModProjectile is MONSTERSTAND_Tier_1 t1) t1.StartDying();
            }
        }

        /// <summary>Perfil de estadísticas de un tier. Tier desconocido (0) = Tier 4 por defecto.</summary>
        public static ScaryProfile GetProfile(int tier)
        {
            switch (tier)
            {
                case 1:
                    return new ScaryProfile
                    {
                        MiniDinoDano = MONSTERSTAND_Tier_1.MiniDinoDano,
                        MiniDinoVelocidad = MONSTERSTAND_Tier_1.MiniDinoVelocidad,
                        MiniDinoSalto = MONSTERSTAND_Tier_1.MiniDinoSalto,
                        MiniDinoRangoDeteccion = MONSTERSTAND_Tier_1.MiniDinoRangoDeteccion,
                        MiniDinoDistanciaVuelo = MONSTERSTAND_Tier_1.MiniDinoDistanciaVuelo,

                        DinoVirusDuracionEfecto = MONSTERSTAND_Tier_1.DinoVirusDuracionEfecto,
                        DinoVirusTiempoTransformar = MONSTERSTAND_Tier_1.DinoVirusTiempoTransformar,
                        DinoVirusDuracionDino = MONSTERSTAND_Tier_1.DinoVirusDuracionDino,
                        TransformedDinoBonusVida = MONSTERSTAND_Tier_1.TransformedDinoBonusVida,
                        TransformedDinoDano = MONSTERSTAND_Tier_1.TransformedDinoDano,
                        TransformedDinoVelocidad = MONSTERSTAND_Tier_1.TransformedDinoVelocidad,
                        TransformedDinoSalto = MONSTERSTAND_Tier_1.TransformedDinoSalto
                    };

                case 2:
                    return new ScaryProfile
                    {
                        MiniDinoDano = MONSTERSTAND_Tier_2.MiniDinoDano,
                        MiniDinoVelocidad = MONSTERSTAND_Tier_2.MiniDinoVelocidad,
                        MiniDinoSalto = MONSTERSTAND_Tier_2.MiniDinoSalto,
                        MiniDinoRangoDeteccion = MONSTERSTAND_Tier_2.MiniDinoRangoDeteccion,
                        MiniDinoDistanciaVuelo = MONSTERSTAND_Tier_2.MiniDinoDistanciaVuelo,

                        DinoVirusDuracionEfecto = MONSTERSTAND_Tier_2.DinoVirusDuracionEfecto,
                        DinoVirusTiempoTransformar = MONSTERSTAND_Tier_2.DinoVirusTiempoTransformar,
                        DinoVirusDuracionDino = MONSTERSTAND_Tier_2.DinoVirusDuracionDino,
                        TransformedDinoBonusVida = MONSTERSTAND_Tier_2.TransformedDinoBonusVida,
                        TransformedDinoDano = MONSTERSTAND_Tier_2.TransformedDinoDano,
                        TransformedDinoVelocidad = MONSTERSTAND_Tier_2.TransformedDinoVelocidad,
                        TransformedDinoSalto = MONSTERSTAND_Tier_2.TransformedDinoSalto
                    };

                case 3:
                    return new ScaryProfile
                    {
                        MiniDinoDano = MONSTERSTAND_Tier_3.MiniDinoDano,
                        MiniDinoVelocidad = MONSTERSTAND_Tier_3.MiniDinoVelocidad,
                        MiniDinoSalto = MONSTERSTAND_Tier_3.MiniDinoSalto,
                        MiniDinoRangoDeteccion = MONSTERSTAND_Tier_3.MiniDinoRangoDeteccion,
                        MiniDinoDistanciaVuelo = MONSTERSTAND_Tier_3.MiniDinoDistanciaVuelo,

                        DinoVirusDuracionEfecto = MONSTERSTAND_Tier_3.DinoVirusDuracionEfecto,
                        DinoVirusTiempoTransformar = MONSTERSTAND_Tier_3.DinoVirusTiempoTransformar,
                        DinoVirusDuracionDino = MONSTERSTAND_Tier_3.DinoVirusDuracionDino,
                        TransformedDinoBonusVida = MONSTERSTAND_Tier_3.TransformedDinoBonusVida,
                        TransformedDinoDano = MONSTERSTAND_Tier_3.TransformedDinoDano,
                        TransformedDinoVelocidad = MONSTERSTAND_Tier_3.TransformedDinoVelocidad,
                        TransformedDinoSalto = MONSTERSTAND_Tier_3.TransformedDinoSalto
                    };

                case 4:
                default:
                    return new ScaryProfile
                    {
                        MiniDinoDano = MONSTERSTAND_Tier_4.MiniDinoDano,
                        MiniDinoVelocidad = MONSTERSTAND_Tier_4.MiniDinoVelocidad,
                        MiniDinoSalto = MONSTERSTAND_Tier_4.MiniDinoSalto,
                        MiniDinoRangoDeteccion = MONSTERSTAND_Tier_4.MiniDinoRangoDeteccion,
                        MiniDinoDistanciaVuelo = MONSTERSTAND_Tier_4.MiniDinoDistanciaVuelo,

                        DinoVirusDuracionEfecto = MONSTERSTAND_Tier_4.DinoVirusDuracionEfecto,
                        DinoVirusTiempoTransformar = MONSTERSTAND_Tier_4.DinoVirusTiempoTransformar,
                        DinoVirusDuracionDino = MONSTERSTAND_Tier_4.DinoVirusDuracionDino,
                        TransformedDinoBonusVida = MONSTERSTAND_Tier_4.TransformedDinoBonusVida,
                        TransformedDinoDano = MONSTERSTAND_Tier_4.TransformedDinoDano,
                        TransformedDinoVelocidad = MONSTERSTAND_Tier_4.TransformedDinoVelocidad,
                        TransformedDinoSalto = MONSTERSTAND_Tier_4.TransformedDinoSalto
                    };
            }
        }
    }
}