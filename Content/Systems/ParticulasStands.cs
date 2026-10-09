using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Jojo.Systems
{
    public class ParticulasStands : ModSystem
    {
        // =========================
        // VISUAL DATA
        // =========================
        public class StandVisualData
        {
            public string IdleTexture;
            public string SpawnTexture;

            public int FrameWidth = 88;
            public int IdleHeight = 68;
            public int SpawnHeight = 80; // Este es el valor por defecto para TODOS

            public int SpawnFrames = 7;
        }

        // =========================
        // STANDS REGISTRY
        // =========================
        public static class Stands
        {

            public static StandVisualData TheWorld_1 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/TheWorld/TheWorld_Tier_1/THEWORLDSTAND_Tier_1",
                SpawnTexture = "Jojo/Content/Projectiles/TheWorld/TheWorld_Tier_1/TW_Spawn_Tier_1"
            };

            public static StandVisualData TheWorld_2 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/TheWorld/TheWorld_Tier_2/THEWORLDSTAND_Tier_2",
                SpawnTexture = "Jojo/Content/Projectiles/TheWorld/TheWorld_Tier_2/TW_Spawn_Tier_2"
            };

            public static StandVisualData TheWorld_3 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/TheWorld/TheWorld_Tier_3/THEWORLDSTAND_Tier_3",
                SpawnTexture = "Jojo/Content/Projectiles/TheWorld/TheWorld_Tier_3/TW_Spawn_Tier_3"
            };

            public static StandVisualData TheWorld_4 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/TheWorld/TheWorld_Tier_4/THEWORLDSTAND_Tier_4",
                SpawnTexture = "Jojo/Content/Projectiles/TheWorld/TheWorld_Tier_4/TW_Spawn_Tier_4"
            };

            public static StandVisualData Tusk1 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/Tusk/Tusk_Tier_1/TUSKSTAND_Tier_1",
                SpawnTexture = "Jojo/Content/Projectiles/Tusk/Tusk_Tier_1/Tusk_Spawn"
            };

            public static StandVisualData ScaryMonsters1 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_1/MONSTERSTAND_Tier_1",
                SpawnTexture = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_1/ScaryMonster_Spawn"
            };

            public static StandVisualData ScaryMonsters2 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_2/MONSTERSTAND_Tier_2",
                SpawnTexture = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_2/ScaryMonster_Spawn"
            };

            public static StandVisualData ScaryMonsters3 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_3/MONSTERSTAND_Tier_3",
                SpawnTexture = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_3/ScaryMonster_Spawn"
            };

            public static StandVisualData ScaryMonsters4 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_4/MONSTERSTAND_Tier_4",
                SpawnTexture = "Jojo/Content/Projectiles/ScaryMonsters/ScaryMonsters_Tier_4/ScaryMonster_Spawn"
            };

            public static StandVisualData CrazyDiamond1 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/CrazyDiamond/CrazyDiamond_Tier_1/CRAZYDIAMONDSTAND_Tier_1",
                SpawnTexture = "Jojo/Content/Projectiles/CrazyDiamond/CrazyDiamond_Tier_1/CrazyDiamond_Spawn"
            };

            public static StandVisualData CrazyDiamond2 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/CrazyDiamond/CrazyDiamond_Tier_2/CRAZYDIAMONDSTAND_Tier_2",
                SpawnTexture = "Jojo/Content/Projectiles/CrazyDiamond/CrazyDiamond_Tier_2/CrazyDiamond_Spawn"
            };

            public static StandVisualData CrazyDiamond3 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/CrazyDiamond/CrazyDiamond_Tier_3/CRAZYDIAMONDSTAND_Tier_3",
                SpawnTexture = "Jojo/Content/Projectiles/CrazyDiamond/CrazyDiamond_Tier_3/CrazyDiamond_Spawn"
            };

            public static StandVisualData CrazyDiamond4 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/CrazyDiamond/CrazyDiamond_Tier_4/CRAZYDIAMONDSTAND_Tier_4",
                SpawnTexture = "Jojo/Content/Projectiles/CrazyDiamond/CrazyDiamond_Tier_4/CrazyDiamond_Spawn"
            };

            public static StandVisualData Cinderella1 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/Cinderella/Cinderella_Tier_1/CINDERELLASTAND_Tier_1",
                SpawnTexture = "Jojo/Content/Projectiles/Cinderella/Cinderella_Tier_1/Cinderella_Spawn"
            };

            public static StandVisualData Cinderella2 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/Cinderella/Cinderella_Tier_2/CINDERELLASTAND_Tier_2",
                SpawnTexture = "Jojo/Content/Projectiles/Cinderella/Cinderella_Tier_2/Cinderella_Spawn"
            };

            public static StandVisualData Cinderella3 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/Cinderella/Cinderella_Tier_3/CINDERELLASTAND_Tier_3",
                SpawnTexture = "Jojo/Content/Projectiles/Cinderella/Cinderella_Tier_3/Cinderella_Spawn"
            };

            public static StandVisualData Cinderella4 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/Cinderella/Cinderella_Tier_4/CINDERELLASTAND_Tier_4",
                SpawnTexture = "Jojo/Content/Projectiles/Cinderella/Cinderella_Tier_4/Cinderella_Spawn"
            };

            public static StandVisualData Golden1 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Tier_1/GOLDENSTAND_Tier_1",
                SpawnTexture = "Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Tier_1/Golden_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData Golden2 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Tier_2/GOLDENSTAND_Tier_2",
                SpawnTexture = "Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Tier_2/Golden_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData Golden3 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Tier_3/GOLDENSTAND_Tier_3",
                SpawnTexture = "Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Tier_3/Golden_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData Golden4 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Tier_4/GOLDENSTAND_Tier_4",
                SpawnTexture = "Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Tier_4/Golden_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData GoldenRequiem = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Requiem/GOLDENSTAND_Requiem",
                SpawnTexture = "Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Requiem/GoldenRequiem_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData HGreenParticulas4 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/HGreen/HGreen_Tier_4/HGREENSTAND_Tier_4",
                SpawnTexture = "Jojo/Content/Projectiles/HGreen/HGreen_Tier_4/HGreen_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData HGreenParticulas2 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/HGreen/HGreen_Tier_2/HGREENSTAND_Tier_2",
                SpawnTexture = "Jojo/Content/Projectiles/HGreen/HGreen_Tier_2/HGreen_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData HGreenParticulas3 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/HGreen/HGreen_Tier_3/HGREENSTAND_Tier_3",
                SpawnTexture = "Jojo/Content/Projectiles/HGreen/HGreen_Tier_3/HGreen_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData HGreenParticulas1 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/HGreen/HGreen_Tier_1/HGREENSTAND_Tier_1",
                SpawnTexture = "Jojo/Content/Projectiles/HGreen/HGreen_Tier_1/HGreen_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData WeatherParticulas1 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/WeatherReport/WeatherReport_Tier_1/WEATHERSTAND_Tier_1",
                SpawnTexture = "Jojo/Content/Projectiles/WeatherReport/WeatherReport_Tier_1/WeatherReport_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData WeatherParticulas2 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/WeatherReport/WeatherReport_Tier_2/WEATHERSTAND_Tier_2",
                SpawnTexture = "Jojo/Content/Projectiles/WeatherReport/WeatherReport_Tier_2/WeatherReport_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData WeatherParticulas3 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/WeatherReport/WeatherReport_Tier_3/WEATHERSTAND_Tier_3",
                SpawnTexture = "Jojo/Content/Projectiles/WeatherReport/WeatherReport_Tier_3/WeatherReport_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData WeatherParticulas4 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/WeatherReport/WeatherReport_Tier_4/WEATHERSTAND_Tier_4",
                SpawnTexture = "Jojo/Content/Projectiles/WeatherReport/WeatherReport_Tier_4/WeatherReport_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData PARTICULASD4C1 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/D4C/D4C_Tier_1/D4CSTAND_Tier_1",
                SpawnTexture = "Jojo/Content/Projectiles/D4C/D4C_Tier_1/D4C_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData PARTICULASD4C2 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/D4C/D4C_Tier_2/D4CSTAND_Tier_2",
                SpawnTexture = "Jojo/Content/Projectiles/D4C/D4C_Tier_2/D4C_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData PARTICULASD4C3 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/D4C/D4C_Tier_3/D4CSTAND_Tier_3",
                SpawnTexture = "Jojo/Content/Projectiles/D4C/D4C_Tier_3/D4C_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData PARTICULASD4C4 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/D4C/D4C_Tier_4/D4CSTAND_Tier_4",
                SpawnTexture = "Jojo/Content/Projectiles/D4C/D4C_Tier_4/D4C_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData MagiciansRed1 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/MagiciansRed/MagiciansRed_Tier_1/MAGICIANSREDSTAND_Tier_1",
                SpawnTexture = "Jojo/Content/Projectiles/MagiciansRed/MagiciansRed_Tier_1/MagiciansRed_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData MagiciansRed2 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/MagiciansRed/MagiciansRed_Tier_2/MAGICIANSREDSTAND_Tier_2",
                SpawnTexture = "Jojo/Content/Projectiles/MagiciansRed/MagiciansRed_Tier_2/MagiciansRed_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData MagiciansRed3 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/MagiciansRed/MagiciansRed_Tier_3/MAGICIANSREDSTAND_Tier_3",
                SpawnTexture = "Jojo/Content/Projectiles/MagiciansRed/MagiciansRed_Tier_3/MagiciansRed_Spawn",
                SpawnHeight = 98
            };

            public static StandVisualData MagiciansRed4 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/MagiciansRed/MagiciansRed_Tier_4/MAGICIANSREDSTAND_Tier_4",
                SpawnTexture = "Jojo/Content/Projectiles/MagiciansRed/MagiciansRed_Tier_4/MagiciansRed_Spawn",
                SpawnHeight = 98
            };


            public static StandVisualData RikaParticulas = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/Rika_Stand/RIKASTAND",
                SpawnTexture = "Jojo/Content/Projectiles/Rika_Stand/RIKA_Spawn"
            };

            public static StandVisualData JusticeParticulas4 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/Justice/Justice_Tier_4/JUSTICESTAND_Tier_4",
                SpawnTexture = "Jojo/Content/Projectiles/Justice/Justice_Tier_4/Justice_Spawn_Tier_4"
            };

            public static StandVisualData WhiteSnakeParticulas1 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_1/WHITESNAKESTAND_Tier_1",
                SpawnTexture = "Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_1/WhiteSnake_Tier_1_Spawn"
            };

            public static StandVisualData WhiteSnakeParticulas2 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_2/WHITESNAKESTAND_Tier_2",
                SpawnTexture = "Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_2/WhiteSnake_Tier_2_Spawn"
            };

            public static StandVisualData WhiteSnakeParticulas3 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_3/WHITESNAKESTAND_Tier_3",
                SpawnTexture = "Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_3/WhiteSnake_Tier_3_Spawn"
            };

            public static StandVisualData WhiteSnakeParticulas4 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_4/WHITESNAKESTAND_Tier_4",
                SpawnTexture = "Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_4/WhiteSnake_Tier_4_Spawn"
            };

            // ==========================================
            // EXCEPCIÓN AÑADIDA AQUÍ: Altura a 98
            // ==========================================
            public static StandVisualData CMoonParticulas1 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/CMoon/CMoon_Tier_1/CMOONSTAND_Tier_1",
                SpawnTexture = "Jojo/Content/Projectiles/CMoon/CMoon_Tier_1/CMOON_Spawn",
                SpawnHeight = 98 // <- ¡Afecta a C Moon!
            };

            public static StandVisualData CMoonParticulas2 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/CMoon/CMoon_Tier_2/CMOONSTAND_Tier_2",
                SpawnTexture = "Jojo/Content/Projectiles/CMoon/CMoon_Tier_2/CMOON_Spawn",
                SpawnHeight = 98 // <- ¡Afecta a C Moon!
            };

            public static StandVisualData CMoonParticulas3 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/CMoon/CMoon_Tier_3/CMOONSTAND_Tier_3",
                SpawnTexture = "Jojo/Content/Projectiles/CMoon/CMoon_Tier_3/CMOON_Spawn",
                SpawnHeight = 98 // <- ¡Afecta a C Moon!
            };

            public static StandVisualData MadeInHeavenParticulas = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/MadeInHeaven/MadeInHeaven_Tier_Final/MADEINHEAVENSTAND",
                SpawnTexture = "Jojo/Content/Projectiles/MadeInHeaven/MadeInHeaven_Tier_Final/MIH_Spawn",
                SpawnHeight = 98 // <- ¡Afecta a Made in Heaven!
            };

            public static StandVisualData SilverCharitoParticulas1 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Tier_1/SLIVERCHARIOTSTAND_Tier_1",
                SpawnTexture = "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Tier_1/SILVERCHARIOT_Spawn"
            };

            public static StandVisualData SilverCharitoParticulas2 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Tier_2/SLIVERCHARIOTSTAND_Tier_2",
                SpawnTexture = "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Tier_2/SILVERCHARIOT_Spawn"
            };

            public static StandVisualData SilverCharitoParticulas3 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Tier_3/SLIVERCHARIOTSTAND_Tier_3",
                SpawnTexture = "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Tier_3/SILVERCHARIOT_Spawn"
            };

            public static StandVisualData SilverCharitoParticulas4 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Tier_4/SLIVERCHARIOTSTAND_Tier_4",
                SpawnTexture = "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Tier_4/SILVERCHARIOT_Spawn"
            };

            // ==========================================
            // EXCEPCIÓN AÑADIDA AQUÍ: Altura a 96
            // ==========================================
            public static StandVisualData SCRequiemParticulas = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Requiem/SILVERCHARIOTSTAND_Requiem",
                SpawnTexture = "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Requiem/SilverChariotRequiem_Spawn",
                SpawnHeight = 96 // <- ¡Solo afecta a SCR!
            };

            public static StandVisualData KingCrimsonParticulas1 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/KingCrimson/KingCrimson_Tier_1/KINGCRIMSONSTAND_Tier_1",
                SpawnTexture = "Jojo/Content/Projectiles/KingCrimson/KingCrimson_Tier_1/KingCrimson_Spawn"
            };

            public static StandVisualData KingCrimsonParticulas2 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/KingCrimson/KingCrimson_Tier_2/KINGCRIMSONSTAND_Tier_2",
                SpawnTexture = "Jojo/Content/Projectiles/KingCrimson/KingCrimson_Tier_2/KingCrimson_Spawn"
            };

            public static StandVisualData KingCrimsonParticulas3 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/KingCrimson/KingCrimson_Tier_3/KINGCRIMSONSTAND_Tier_3",
                SpawnTexture = "Jojo/Content/Projectiles/KingCrimson/KingCrimson_Tier_3/KingCrimson_Spawn"
            };

            public static StandVisualData KingCrimsonParticulas4 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/KingCrimson/KingCrimson_Tier_4/KINGCRIMSONSTAND_Tier_4",
                SpawnTexture = "Jojo/Content/Projectiles/KingCrimson/KingCrimson_Tier_4/KingCrimson_Spawn"
            };

            public static StandVisualData AnubisParticulas1 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/Anubis/Anubis_Tier_1/ANUBISSTAND_Tier_1",
                SpawnTexture = "Jojo/Content/Projectiles/Anubis/Anubis_Tier_1/ANUBIS_Spawn"
            };

            public static StandVisualData AnubisParticulas2 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/Anubis/Anubis_Tier_2/ANUBISSTAND_Tier_2",
                SpawnTexture = "Jojo/Content/Projectiles/Anubis/Anubis_Tier_2/ANUBIS_Spawn"
            };

            public static StandVisualData AnubisParticulas3 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/Anubis/Anubis_Tier_3/ANUBISSTAND_Tier_3",
                SpawnTexture = "Jojo/Content/Projectiles/Anubis/Anubis_Tier_3/ANUBIS_Spawn"
            };

            public static StandVisualData AnubisParticulas4 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/Anubis/Anubis_Tier_4/ANUBISSTAND_Tier_4",
                SpawnTexture = "Jojo/Content/Projectiles/Anubis/Anubis_Tier_4/ANUBIS_Spawn"
            };

            public static StandVisualData StarPlatinum1 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/StarPlatinum/StarPlatinum_Tier_1/STARPLATINUMSTAND_Tier_1",
                SpawnTexture = "Jojo/Content/Projectiles/StarPlatinum/StarPlatinum_Tier_1/SP_Spawn"
            };

            public static StandVisualData StarPlatinum2 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/StarPlatinum/StarPlatinum_Tier_2/STARPLATINUMSTAND_Tier_2",
                SpawnTexture = "Jojo/Content/Projectiles/StarPlatinum/StarPlatinum_Tier_2/SP_Spawn"
            };

            public static StandVisualData StarPlatinum3 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/StarPlatinum/StarPlatinum_Tier_3/STARPLATINUMSTAND_Tier_3",
                SpawnTexture = "Jojo/Content/Projectiles/StarPlatinum/StarPlatinum_Tier_3/SP_Spawn"
            };

            public static StandVisualData StarPlatinum4 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/StarPlatinum/StarPlatinum_Tier_4/STARPLATINUMSTAND_Tier_4",
                SpawnTexture = "Jojo/Content/Projectiles/StarPlatinum/StarPlatinum_Tier_4/SP_Spawn"
            };          

            public static StandVisualData KillerP1 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_1/KILLERQUEENSTAND_Tier_1",
                SpawnTexture = "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_1/KQ_SPAWN"
            };

            public static StandVisualData KillerP2 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_2/KILLERQUEENSTAND_Tier_2",
                SpawnTexture = "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_2/KQ_SPAWN"
            };

            public static StandVisualData KillerP3 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_3/KILLERQUEENSTAND_Tier_3",
                SpawnTexture = "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_3/KQ_SPAWN"
            };

            public static StandVisualData KillerP4 = new()
            {
                IdleTexture = "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_4/KILLERQUEENSTAND_Tier_4",
                SpawnTexture = "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_4/KQ_SPAWN"
            };
        }

        // =========================
        //  RUNTIME
        // =========================
        public class StandRuntime
        {
            public bool spawning = true;
            public int frame = 0;
            public int timer = 0;

            const int speed = 5;

            public void Update(StandVisualData data)
            {
                if (!spawning) return;

                timer++;

                if (timer >= speed)
                {
                    timer = 0;
                    frame++;

                    if (frame >= data.SpawnFrames)
                    {
                        spawning = false;
                        frame = 0;
                    }
                }
            }
        }

        // =========================
        // HELPERS
        // =========================
        public static void ApplySpawnLock(Projectile p, StandRuntime rt)
        {
            if (rt.spawning)
            {
                p.friendly = false;
                p.damage = 0;
            }
        }

        public static bool CanUseSkills(StandRuntime rt) => !rt.spawning;
        public static bool CanAttack(StandRuntime rt) => !rt.spawning;

        public static void FollowPlayer(Projectile p, Player player, Vector2 offset, float speed = 0.25f)
        {
            p.Center = Vector2.Lerp(p.Center, player.Center + offset, speed);
            p.velocity = Vector2.Zero;
            p.timeLeft = 2;
        }

        // =========================
        //  DESPAWN (SIN PARTICULAS)
        // =========================
        public static void Despawn(Projectile projectile, Player player)
        {
            // ==========================================
            // MOVIMIENTO HACIA EL JUGADOR (SUAVE)
            // ==========================================

            Vector2 toPlayer = player.Center - projectile.Center;

            // Entrada más lenta al cuerpo (ajustado)
            projectile.velocity = Vector2.Lerp(projectile.velocity, toPlayer * 0.28f, 0.15f);

            // Succión visual al jugador
            projectile.Center = Vector2.Lerp(projectile.Center, player.Center, 0.14f);

            // ==========================================
            //  DESAPARICIÓN RÁPIDA
            // ==========================================

            projectile.alpha += 35;

            // ==========================================
            //  FINALIZACIÓN
            // ==========================================

            if (projectile.alpha >= 255)
            {
                projectile.Kill();
            }
        }

        // =========================
        //  SPAWN (SIN DUST TAMBIÉN)
        // =========================
        public static void Spawn(Player player, StandVisualData data)
        {
            //  eliminado todo Dust intencionalmente
            // Solo posicion base (sin efectos visuales externos)

            Vector2 basePos = player.Center + new Vector2(-20 * player.direction, -8);
        }
    }
}