using Terraria;

namespace Jojo.Content.NPCs.StandsNpc
{
    public interface IStandNpcProjectile
    {
        NPC OwnerNpc { get; }

        // Añade esta línea. Por defecto será falso a menos que lo cambies en el Stand.
        bool ResistsTimeStop => false;
    }
}