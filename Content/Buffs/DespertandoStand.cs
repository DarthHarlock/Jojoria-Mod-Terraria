using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_1;
using Jojo.Content.Projectiles.Tusk.Tusk_Tier_1;
using Jojo.Content.Projectiles.CrazyDiamond.CrazyDiamond_Tier_1;
using Jojo.Content.Projectiles.Cinderella.Cinderella_Tier_1;
using Jojo.Content.Projectiles.Anubis.Anubis_Tier_1;
using Jojo.Content.Projectiles.D4C.D4C_Tier_1;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_1;
using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_1;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_1;
using Jojo.Content.Projectiles.StarPlatinum.StarPlatinum_Tier_1;
using Jojo.Content.Projectiles.TheWorld.TheWorld_Tier_1;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_1;
using Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_1;
using Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_1;
using Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_1;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_1;

namespace Jojo.Content.Buffs
{
    // EL BUFF DE 13 SEGUNDOS
    public class DespertandoStand : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // Cambiado a false para que se guarde al salir del mundo
            Main.buffNoSave[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;

            // 1. Lo convertimos en un "Debuff" para evitar que se quite con Click Derecho
            Main.debuff[Type] = true;

            // 2. Evita que la Enfermera te quite el buffo curándote
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
    }

    // EL JUGADOR QUE DETECTA EL FIN DEL BUFF Y TIENE LA LISTA DE DROPS
    public class StandPlayerBuff : ModPlayer
    {
        private bool teniaBuffEfecto = false;

        public override void PreUpdate()
        {
            bool tieneBuffActual = Player.HasBuff(ModContent.BuffType<DespertandoStand>());

            // Si el fotograma anterior tenía el buff, pero en este ya no
            if (teniaBuffEfecto && !tieneBuffActual)
            {
                // 3. Comprobamos que el jugador ESTÉ VIVO (!Player.dead)
                if (Player.whoAmI == Main.myPlayer && !Player.dead)
                {
                    // LISTA DE POSIBILIDADES - Todas con el mismo peso (misma probabilidad)
                    int[] tiposDeItems = new int[]
                    {
                        ModContent.ItemType<WhiteSnakeItem_Tier_1>(),
                        ModContent.ItemType<TuskItem_Tier_1>(),
                        ModContent.ItemType<WeatherReportItem_Tier_1>(),
                        ModContent.ItemType<CrazyDiamondItem_Tier_1>(),
                        ModContent.ItemType<ScaryMonstersItem_Tier_1>(),
                        ModContent.ItemType<CinderellaItem_Tier_1>(),
                        ModContent.ItemType<HGreenItem_Tier_1>(),
                        ModContent.ItemType<D4CItem_Tier_1>(),
                        ModContent.ItemType<AnubisItem_Tier_1>(),
                        ModContent.ItemType<SilverChariotItem_Tier_1>(),
                        ModContent.ItemType<StarPlatinumItem_Tier_1>(),
                        ModContent.ItemType<TheWorldItem_Tier_1>(),
                        ModContent.ItemType<MRedItem_Tier_1>(),
                        ModContent.ItemType<KingCrimsonItem_Tier_1>(),
                        ModContent.ItemType<GoldenItem_Tier_1>(),
                        ModContent.ItemType<KillerQueenItem_Tier_1>()
                    };

                    int itemFinalType = tiposDeItems[Main.rand.Next(tiposDeItems.Length)];

                    Player.QuickSpawnItem(Player.GetSource_Misc("DespertarStand"), itemFinalType, 1);
                }
            }

            teniaBuffEfecto = tieneBuffActual;
        }
    }
}