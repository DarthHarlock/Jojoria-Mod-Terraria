using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_3;
using Jojo.Content.Projectiles.Cinderella.Cinderella_Tier_3;
using Jojo.Content.Projectiles.CrazyDiamond.CrazyDiamond_Tier_3;
using Jojo.Content.Projectiles.Anubis.Anubis_Tier_3;
using Jojo.Content.Projectiles.D4C.D4C_Tier_3;
using Jojo.Content.Projectiles.HGreen.HGreen_Tier_3;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_3;
using Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_3;
using Jojo.Content.Projectiles.StarPlatinum.StarPlatinum_Tier_3;
using Jojo.Content.Projectiles.TheWorld.TheWorld_Tier_3;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_3;
using Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_3;
using Jojo.Content.Projectiles.KingCrimson.KingCrimson_Tier_3;
using Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_3;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_3;

namespace Jojo.Content.Buffs
{
    // EL BUFF MEJORADO
    public class DespertandoStandMejorado : ModBuff
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

    // EL JUGADOR QUE DETECTA EL FIN DEL BUFF MEJORADO Y TIENE LA LISTA DE DROPS
    public class StandPlayerBuffMejorado : ModPlayer
    {
        private bool teniaBuffEfectoMejorado = false;

        public override void PreUpdate()
        {
            bool tieneBuffActual = Player.HasBuff(ModContent.BuffType<DespertandoStandMejorado>());

            // Si el fotograma anterior tenía el buff, pero en este ya no
            if (teniaBuffEfectoMejorado && !tieneBuffActual)
            {
                // 3. Comprobamos que el jugador ESTÉ VIVO (!Player.dead)
                if (Player.whoAmI == Main.myPlayer && !Player.dead)
                {
                    // LISTA DE POSIBILIDADES - Todas con el mismo peso (misma probabilidad)
                    int[] tiposDeItems = new int[]
                    {
                        ModContent.ItemType<WhiteSnakeItem_Tier_3>(),
                        ModContent.ItemType<CinderellaItem_Tier_3>(),
                        
                        ModContent.ItemType<WeatherReportItem_Tier_3>(),
                        ModContent.ItemType<ScaryMonstersItem_Tier_3>(),
                        ModContent.ItemType<CrazyDiamondItem_Tier_3>(),
                        ModContent.ItemType<HGreenItem_Tier_3>(),
                        ModContent.ItemType<D4CItem_Tier_3>(),
                        ModContent.ItemType<AnubisItem_Tier_3>(),
                        ModContent.ItemType<SilverChariotItem_Tier_3>(),
                        ModContent.ItemType<StarPlatinumItem_Tier_3>(),
                        ModContent.ItemType<TheWorldItem_Tier_3>(),
                        ModContent.ItemType<MRedItem_Tier_3>(),
                        ModContent.ItemType<KingCrimsonItem_Tier_3>(),
                        ModContent.ItemType<GoldenItem_Tier_3>(),
                        ModContent.ItemType<KillerQueenItem_Tier_3>()
                    };

                    int itemFinalType = tiposDeItems[Main.rand.Next(tiposDeItems.Length)];

                    Player.QuickSpawnItem(Player.GetSource_Misc("DespertarStandMejorado"), itemFinalType, 1);
                }
            }

            teniaBuffEfectoMejorado = tieneBuffActual;
        }
    }
}