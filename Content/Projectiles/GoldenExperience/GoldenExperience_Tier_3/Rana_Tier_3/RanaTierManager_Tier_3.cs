using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_3.Rana_Tier_3
{
    public static class RanaTierManager_Tier_3
    {
        static readonly SoundStyle RanaSummonSound = SoundID.Item29 with { Volume = 0.6f, Pitch = 0.5f };

        public static void SpawnRana(Player p, Vector2 pos)
        {
            SoundEngine.PlaySound(RanaSummonSound, pos);

            int index = NPC.NewNPC(
                p.GetSource_FromThis(),
                (int)pos.X,
                (int)pos.Y,
                ModContent.NPCType<Rana_Refleja_Tier_3>(),
                0, 0f, 0f, 0f, (float)p.whoAmI
            );

            // FIX: Sincronizar la aparición de la rana a todos los clientes en Multijugador
            if (index >= 0 && index < Main.maxNPCs && Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, index);
            }
        }
    }
}