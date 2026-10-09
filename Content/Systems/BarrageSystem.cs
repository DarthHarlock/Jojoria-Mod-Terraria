using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Players;
using System;
// stands

using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_1;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_2;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_3;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_4;


using Jojo.Content.Projectiles.D4C.D4C_Tier_1;
using Jojo.Content.Projectiles.D4C.D4C_Tier_2;
using Jojo.Content.Projectiles.D4C.D4C_Tier_3;
using Jojo.Content.Projectiles.D4C.D4C_Tier_4;

using Jojo.Content.Projectiles.CrazyDiamond.CrazyDiamond_Tier_1;
using Jojo.Content.Projectiles.CrazyDiamond.CrazyDiamond_Tier_2;
using Jojo.Content.Projectiles.CrazyDiamond.CrazyDiamond_Tier_3;
using Jojo.Content.Projectiles.CrazyDiamond.CrazyDiamond_Tier_4;

using Jojo.Content.Projectiles.CMoon.CMoon_Tier_1;
using Jojo.Content.Projectiles.CMoon.CMoon_Tier_2;
using Jojo.Content.Projectiles.CMoon.CMoon_Tier_3;

using Jojo.Content.Projectiles.MadeInHeaven.MadeInHeaven_Tier_Final;

using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_1;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_2;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_3;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_4;

using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem;

using Jojo.Content.Projectiles.Cinderella.Cinderella_Tier_1;
using Jojo.Content.Projectiles.Cinderella.Cinderella_Tier_2;
using Jojo.Content.Projectiles.Cinderella.Cinderella_Tier_3;
using Jojo.Content.Projectiles.Cinderella.Cinderella_Tier_4;

using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_1;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_2;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_3;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_4;

using Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_1;
using Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_2;
using Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_3;
using Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_4;

using Jojo.Content.Projectiles.TheWorld.TheWorld_Tier_1;
using Jojo.Content.Projectiles.TheWorld.TheWorld_Tier_2;
using Jojo.Content.Projectiles.TheWorld.TheWorld_Tier_3;
using Jojo.Content.Projectiles.TheWorld.TheWorld_Tier_4;

using Jojo.Content.Projectiles.StarPlatinum.StarPlatinum_Tier_1;
using Jojo.Content.Projectiles.StarPlatinum.StarPlatinum_Tier_2;
using Jojo.Content.Projectiles.StarPlatinum.StarPlatinum_Tier_3;
using Jojo.Content.Projectiles.StarPlatinum.StarPlatinum_Tier_4;

using Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_1;
using Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_2;
using Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_3;
using Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_4;

using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Requiem;

using Jojo.Content.Projectiles.Rika_Stand;

namespace Jojo.Content.Systems
{
    public static class BarrageSystem
    {
        public static float PunchSpeed = 2.5f;
        public static float PunchDistance = 4f;

        static float spawnTimer;

        public static void SpawnPunches(
            Projectile stand,
            Player player,
            Vector2 off
        )
        {
            int punchType = GetPunchProjectile(stand);

            if (punchType == -1)
                return;

            SpawnPunches(stand, player, off, punchType);
        }

        static void SpawnPunches(
            Projectile stand,
            Player player,
            Vector2 off,
            int punchProjectile
        )
        {
            float speed = player.GetModPlayer<StandStatsPlayer>().standSpeed;

            float ghostSpeed = speed > 0f ? speed : 20f;

            spawnTimer += ghostSpeed / 60f;

            int punchesToSpawn = (int)spawnTimer;

            if (punchesToSpawn <= 0)
                return;

            spawnTimer -= punchesToSpawn;

            Vector2 center = stand.Center;

            Vector2 raw = off;
            if (raw == Vector2.Zero)
                raw = Vector2.UnitX;

            // ======================================================
            // 🛠️ FILTRO ANTIBUG: DETECCIÓN DE ZONA ROJA / AZUL
            // ======================================================
            Vector2 trueDir;
            if (stand.rotation == 0f)
            {
                // Zona Roja: El Stand está estricto horizontal (viendo a izquierda o derecha).
                // Forzamos que el puñetazo sea perfectamente horizontal para evitar atracciones al techo/suelo.
                trueDir = new Vector2(raw.X >= 0f ? 1f : -1f, 0f);
            }
            else
            {
                // Zona Azul: usamos la dirección REAL hacia el objetivo/mouse (raw),
                // NUNCA stand.rotation directamente, porque stand.rotation le suma +Pi
                // cuando off.X < 0 (es un hack exclusivo para el flip visual del sprite del stand,
                // no representa la dirección física real). Usar stand.rotation aquí invertía
                // 180° la dirección de los puños cada vez que se atacaba a la izquierda.
                trueDir = Vector2.Normalize(raw);
            }

            Vector2 truePerp = new Vector2(-trueDir.Y, trueDir.X);

            // 🔥 CORRECCIÓN FINAL: Unificamos el valor de 'forward' a -1f para todas las direcciones.
            // Esto elimina cualquier asimetría lógica. Al usar el nuevo sprite balanceado,
            // la barraca se verá y se sentirá simétrica.
            float forward = -1f;
            float spread = 33f;

            for (int i = 0; i < punchesToSpawn; i++)
            {
                Vector2 startOffset =
                    (trueDir * forward) +
                    (truePerp * Main.rand.NextFloat(-spread, spread)) +
                    (trueDir * Main.rand.NextFloat(-15f, 15f));

                // Compatibilidad de texturas basada en la dirección real final
                int basePacked = Main.rand.Next(1, 4) + (trueDir.X >= 0f ? 0 : 10);

                // Codificamos el ángulo real limpio en los decimales para SCR
                float angle = trueDir.ToRotation();
                float normalizedAngle = (angle + MathHelper.Pi) / (MathHelper.TwoPi + 0.01f);
                float encodedAi1 = basePacked + normalizedAngle;

                int projIndex = Projectile.NewProjectile(
                    stand.GetSource_FromThis(),
                    center + startOffset,
                    startOffset,
                    punchProjectile,
                    0,
                    0f,
                    player.whoAmI,
                    stand.whoAmI,
                    encodedAi1
                );
            }
        }

        static int GetPunchProjectile(Projectile stand)
        {
            var mp = stand.ModProjectile;

            if (mp == null)
                return -1;

            if (mp is THEWORLDSTAND_Tier_1)
                return ModContent.ProjectileType<TW_BarragePunch_Tier_1>();

            if (mp is THEWORLDSTAND_Tier_2)
                return ModContent.ProjectileType<TW_BarragePunch_Tier_2>();

            if (mp is THEWORLDSTAND_Tier_3)
                return ModContent.ProjectileType<TW_BarragePunch_Tier_3>();

            if (mp is THEWORLDSTAND_Tier_4)
                return ModContent.ProjectileType<TW_BarragePunch_Tier_4>();
            //--------------------------------------------------
            
            //--------------------------------------------------
            if (mp is CRAZYDIAMONDSTAND_Tier_1)
                return ModContent.ProjectileType<CrazyDiamond_BarragePunch_Tier_1>();

            if (mp is CRAZYDIAMONDSTAND_Tier_2)
                return ModContent.ProjectileType<CrazyDiamond_BarragePunch_Tier_2>();

            if (mp is CRAZYDIAMONDSTAND_Tier_3)
                return ModContent.ProjectileType<CrazyDiamond_BarragePunch_Tier_3>();

            if (mp is CRAZYDIAMONDSTAND_Tier_4)
                return ModContent.ProjectileType<CrazyDiamond_BarragePunch_Tier_4>();
            //--------------------------------------------------
            if (mp is D4CSTAND_Tier_1)
                return ModContent.ProjectileType<D4C_BarragePunch_Tier_1>();

            if (mp is D4CSTAND_Tier_2)
                return ModContent.ProjectileType<D4C_BarragePunch_Tier_2>();

            if (mp is D4CSTAND_Tier_3)
                return ModContent.ProjectileType<D4C_BarragePunch_Tier_3>();

            if (mp is D4CSTAND_Tier_4)
                return ModContent.ProjectileType<D4C_BarragePunch_Tier_4>();
            //--------------------------------------------------
            if (mp is CINDERELLASTAND_Tier_1)
                return ModContent.ProjectileType<Cinderella_BarragePunch_Tier_1>();

            if (mp is CINDERELLASTAND_Tier_2)
                return ModContent.ProjectileType<Cinderella_BarragePunch_Tier_2>();

            if (mp is CINDERELLASTAND_Tier_3)
                return ModContent.ProjectileType<Cinderella_BarragePunch_Tier_3>();

            if (mp is CINDERELLASTAND_Tier_4)
                return ModContent.ProjectileType<Cinderella_BarragePunch_Tier_4>();
            //--------------------------------------------------
            if (mp is GOLDENSTAND_Tier_1)
                return ModContent.ProjectileType<Golden_BarragePunch_Tier_1>();

            if (mp is GOLDENSTAND_Tier_2)
                return ModContent.ProjectileType<Golden_BarragePunch_Tier_2>();

            if (mp is GOLDENSTAND_Tier_3)
                return ModContent.ProjectileType<Golden_BarragePunch_Tier_3>();

            if (mp is GOLDENSTAND_Tier_4)
                return ModContent.ProjectileType<Golden_BarragePunch_Tier_4>();

            if (mp is GOLDENSTAND_Requiem)
                return ModContent.ProjectileType<Golden_BarragePunch_Requiem>();
            //--------------------------------------------------
            if (mp is WHITESNAKESTAND_Tier_1)
                return ModContent.ProjectileType<WhiteSnake_BarragePunch_Tier_1>();

            if (mp is WHITESNAKESTAND_Tier_2)
                return ModContent.ProjectileType<WhiteSnake_BarragePunch_Tier_2>();

            if (mp is WHITESNAKESTAND_Tier_3)
                return ModContent.ProjectileType<WhiteSnake_BarragePunch_Tier_3>();

            if (mp is WHITESNAKESTAND_Tier_4)
                return ModContent.ProjectileType<WhiteSnake_BarragePunch_Tier_4>();
            //--------------------------------------------------
            if (mp is CMOONSTAND_Tier_1)
                return ModContent.ProjectileType<CMOON_BarragePunch_Tier_1>();

            if (mp is CMOONSTAND_Tier_2)
                return ModContent.ProjectileType<CMOON_BarragePunch_Tier_2>();

            if (mp is CMOONSTAND_Tier_3)
                return ModContent.ProjectileType<CMOON_BarragePunch>();
            //--------------------------------------------------
            if (mp is MADEINHEAVENSTAND)
                return ModContent.ProjectileType<MIH_BarragePunch>();
            //--------------------------------------------------
            if (mp is KINGCRIMSONSTAND_Tier_1)
                return ModContent.ProjectileType<KC_BarragePunch_Tier_1>();

            if (mp is KINGCRIMSONSTAND_Tier_2)
                return ModContent.ProjectileType<KC_BarragePunch_Tier_2>();

            if (mp is KINGCRIMSONSTAND_Tier_3)
                return ModContent.ProjectileType<KC_BarragePunch_Tier_3>();

            if (mp is KINGCRIMSONSTAND_Tier_4)
                return ModContent.ProjectileType<KC_BarragePunch_Tier_4>();
            //--------------------------------------------------
            if (mp is STARPLATINUMSTAND_Tier_1)
                return ModContent.ProjectileType<SP_BarragePunch_Tier_1>();

            if (mp is STARPLATINUMSTAND_Tier_2)
                return ModContent.ProjectileType<SP_BarragePunch_Tier_2>();

            if (mp is STARPLATINUMSTAND_Tier_3)
                return ModContent.ProjectileType<SP_BarragePunch_Tier_3>();

            if (mp is STARPLATINUMSTAND_Tier_4)
                return ModContent.ProjectileType<SP_BarragePunch_Tier_4>();
            //--------------------------------------------------
            if (mp is RIKASTAND)
                return ModContent.ProjectileType<RIKA_BarragePunch>();
            //--------------------------------------------------
            if (mp is WEATHERSTAND_Tier_1)
                return ModContent.ProjectileType<Weather_BarragePunch_Tier_1>();

            if (mp is WEATHERSTAND_Tier_2)
                return ModContent.ProjectileType<Weather_BarragePunch_Tier_2>();

            if (mp is WEATHERSTAND_Tier_3)
                return ModContent.ProjectileType<Weather_BarragePunch_Tier_3>();

            if (mp is WEATHERSTAND_Tier_4)
                return ModContent.ProjectileType<Weather_BarragePunch_Tier_4>();
            //--------------------------------------------------
            if (mp is SILVERCHARIOTSTAND_Requiem)
                return ModContent.ProjectileType<SCR_BarragePunch_Requiem>();
            //--------------------------------------------------
            if (mp is KILLERQUEENSTAND_Tier_1)
                return ModContent.ProjectileType<KQ_BarragePunch_Tier_1>();

            if (mp is KILLERQUEENSTAND_Tier_2)
                return ModContent.ProjectileType<KQ_BarragePunch_Tier_2>();

            if (mp is KILLERQUEENSTAND_Tier_3)
                return ModContent.ProjectileType<KQ_BarragePunch_Tier_3>();

            if (mp is KILLERQUEENSTAND_Tier_4)
                return ModContent.ProjectileType<KQ_BarragePunch_Tier_4>();

            return -1;
        }
    }
}