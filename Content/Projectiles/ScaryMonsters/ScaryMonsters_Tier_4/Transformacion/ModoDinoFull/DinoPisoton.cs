using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4.Transformacion.ModoDinoFull
{
    public class DinoPisoton : ModPlayer
    {
        public const float FuerzaRebote = 9f;
        public const float VelocidadMinCaida = 2f;
        const int InmunidadNPC = 20;

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer) return;
            var dinoPlayer = Player.GetModPlayer<DinoPlayer>();
            if (!dinoPlayer.IsDino) return;
            if (Player.velocity.Y < VelocidadMinCaida) return;

            Rectangle pies = new Rectangle(
                (int)Player.position.X - 4,
                (int)Player.Bottom.Y - 10,
                Player.width + 8,
                20);

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.immortal) continue;
                if (npc.immune[Player.whoAmI] > 0) continue;
                if (!pies.Intersects(npc.Hitbox)) continue;

                if (Player.Bottom.Y > npc.Center.Y) continue;

                int dano = (int)Player.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(DinoStatsHelper.GetDanoPisoton(Player));
                int dir = Player.Center.X < npc.Center.X ? 1 : -1;

                Player.ApplyDamageToNPC(npc, dano, 6f, dir, false, ModContent.GetInstance<ClaseStand>(), false);
                npc.immune[Player.whoAmI] = InmunidadNPC;

                Player.velocity.Y = -FuerzaRebote;
                Player.RefreshExtraJumps();
                dinoPlayer.saltosExtraRestantes = DinoStatsHelper.GetMaxSaltosExtra(Player);
                Player.fallStart = (int)(Player.position.Y / 16f);

                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f }, Player.Center);
                for (int d = 0; d < 10; d++)
                {
                    Dust dust = Dust.NewDustDirect(Player.BottomLeft, Player.width, 4,
                        DustID.Smoke, Main.rand.NextFloat(-2f, 2f), -1f, 150, default, 1.2f);
                    dust.noGravity = true;
                }
                break;
            }
        }
    }
}   