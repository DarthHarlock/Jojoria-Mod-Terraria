using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.NPCs.StandsNpc; // Necesario para reconocer la interfaz IStandNpcProjectile

namespace Jojo.Content.Items
{
    // ==========================================
    // 1. EL ACCESORIO (Tu ítem principal)
    // ==========================================
    public class Npc_Killer : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true; // Define que es un accesorio
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(gold: 5);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<NpcKillerPlayer>().canKillTownNPCs = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            // AQUÍ ESTÁ EL CAMBIO: Usamos el nuevo grupo personalizado de Hierro/Plomo
            recipe.AddRecipeGroup("Jojo:IronOrLead", 10);

            // 1 Alma_Npc
            recipe.AddIngredient(ModContent.ItemType<Alma_Npc>(), 1);

            // Estación: Yunque
            recipe.AddTile(TileID.Anvils);

            recipe.Register();
        }
    }

    // ==========================================
    // 2. EL JUGADOR (Guarda si tienes el ítem puesto)
    // ==========================================
    public class NpcKillerPlayer : ModPlayer
    {
        public bool canKillTownNPCs;

        public override void ResetEffects()
        {
            canKillTownNPCs = false; // Se desactiva en cada frame si no lo tienes equipado
        }
    }

    // ==========================================
    // 3. LOS NPCs (Permite que reciban daño)
    // ==========================================
    public class NpcKillerGlobalNPC : GlobalNPC
    {
        // Para cuando pegas con Espadas, Picos, etc.
        public override bool? CanBeHitByItem(NPC npc, Player player, Item item)
        {
            if (npc.townNPC && player.GetModPlayer<NpcKillerPlayer>().canKillTownNPCs)
            {
                if (item.damage > 0)
                {
                    return true;
                }
                return false;
            }
            return null;
        }

        // Para cuando disparas Proyectiles (Balas, Stands, Magia, etc.)
        public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
        {
            // Verificación de seguridad para evitar errores de índice de array
            if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                Player player = Main.player[projectile.owner];

                if (npc.townNPC && player.active && player.GetModPlayer<NpcKillerPlayer>().canKillTownNPCs)
                {
                    // Si el proyectil es un Stand de NPC, dejamos que el Stand use su propia lógica
                    if (projectile.ModProjectile is IStandNpcProjectile)
                    {
                        return null;
                    }

                    // Permitimos el golpe si el proyectil es del jugador y tiene daño
                    if (projectile.friendly && projectile.damage > 0)
                    {
                        return true;
                    }

                    return false;
                }
            }
            return null;
        }
    }
}