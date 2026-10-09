using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;

namespace Jojo.Content.Drops.Bosses
{
    public class PotenciadorNPCDrop : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == NPCID.WallofFlesh)
            {
                float porcentajeDrop = 50f;

                npcLoot.Add(ItemDropRule.ByCondition(
                    new PorcentajeCondition(porcentajeDrop),
                    ModContent.ItemType<Items.Potenciadores.EmblemaDeStand>(), // CAMBIADO: Añadido .Potenciadores
                    1, // Cantidad mínima
                    1  // Cantidad máxima
                ));
            }
        }
    }


    public class PotenciadorBagDrop : GlobalItem
    {
        public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
        {
            if (item.type == ItemID.WallOfFleshBossBag)
            {
                float porcentajeDrop = 50f;

                itemLoot.Add(ItemDropRule.ByCondition(
                    new PorcentajeCondition(porcentajeDrop),
                    ModContent.ItemType<Items.Potenciadores.EmblemaDeStand>(), // CAMBIADO: Añadido .Potenciadores
                    1,
                    1
                ));
            }
        }
    }

    public class PorcentajeCondition : IItemDropRuleCondition
    {
        private readonly float _porcentaje;

        public PorcentajeCondition(float porcentaje)
        {
            _porcentaje = porcentaje;
        }

        public bool CanDrop(DropAttemptInfo info)
        {
            // CORREGIDO: De 0f a 100f para que el 50f de arriba signifique realmente un 50% de probabilidad
            return Main.rand.NextFloat(0f, 100f) < _porcentaje;
        }

        public bool CanShowItemDropInUI() => true;

        public string GetConditionDescription() => null;
    }
}