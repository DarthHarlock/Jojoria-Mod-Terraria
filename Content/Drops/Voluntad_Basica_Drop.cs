using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Items;
using Jojo.Content.Projectiles.WeatherReport.WeatherReport_Tier_4.LluviaDeRanas_Tier_4;

namespace Jojo.Content.Drops
{
    public class Voluntad_Basica_Drop : GlobalNPC
    {
        private const int DropChancePercent = 40;

        public override void OnKill(NPC npc)
        {
            // EVITAR DROPS DE LA RANA DE WEATHER REPORT
            if (npc.type == ModContent.NPCType<RanaVenenosa_Tier_4>())
                return;

            if (npc.SpawnedFromStatue)
                return;

            if (npc.friendly)
                return;

            // EVITAR DROPS DE ANIMALES Y CRIATURAS PACÍFICAS
            if (NPCID.Sets.CountsAsCritter[npc.type] || npc.damage == 0)
                return;

            Player player = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)];

            if (player == null || !player.active)
                return;

            if (EstaEnBiomaExcluido(player))
                return;

            if (!Main.rand.NextBool(DropChancePercent, 100))
                return;

            Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Voluntad_Basica>());
        }

        private bool EstaEnBiomaExcluido(Player player)
        {
            return player.ZoneUnderworldHeight
                || player.ZoneCorrupt
                || player.ZoneCrimson
                || player.ZoneHallow
                || player.ZoneSkyHeight;
        }
    }
}