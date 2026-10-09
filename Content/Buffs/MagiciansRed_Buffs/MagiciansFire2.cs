using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Buffs.MagiciansRed_Buffs;

namespace Jojo.Content.Buffs.MagiciansRed_Buffs
{
    // Buff de JUGADOR (no confundir con "MagiciansFire", que es el debuff que
    // se le pone a los NPCs cuando los toca el Fuego1). Este buff solo indica
    // "la habilidad F esta activa"; toda la logica del aro de fuegos vive en
    // Fuego2_Tier_4 y en MagiciansFire2Helper para no sobrecargar este script.
    public class MagiciansFire2 : ModBuff
    {
        // Esto es lo que determina cuanto dura la habilidad.
        // 60 ticks = 1 segundo -> 600 ticks = 10 segundos.
        public const int DurationTicks = 600;

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // No necesita hacer nada aca: cada proyectil Fuego2_Tier_4 comprueba
            // por su cuenta, cada frame, si el jugador sigue teniendo este buff
            // (player.HasBuff) para saber si debe seguir orbitando o si debe
            // explotar y desaparecer.
        }
    }
}