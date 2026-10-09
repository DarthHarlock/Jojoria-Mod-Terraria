using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Buffs.D4C_Buffs
{
    // Buff-bandera del Modo Fantasma (Skill H de D4C Tier 4).
    // La duración con la que se aplica este buff (AddBuff) ES la duración
    // de la habilidad: mientras esté activo, D4CGhostPlayer aplica todos
    // los efectos (invulnerabilidad, no-daño, shader azul, ocultar NPCs y
    // proyectiles, aggro). Al expirar, todo se revierte automáticamente:
    // el filtro azul desaparece, los enemigos vuelven a verse y el
    // señuelo se borra.
    //
    // Falta el asset de icono: Content/Buffs/D4C_Buffs/D4CGhostMode.png (32x32).
    public class D4CGhostMode : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Vacío a propósito: D4CGhostPlayer.GhostActive ya lee
            // player.HasBuff(ModContent.BuffType<D4CGhostMode>()) cada tick.
        }
    }
}