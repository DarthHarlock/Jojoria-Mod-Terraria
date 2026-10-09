using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Buffs.ScaryMonsters_Buffs;

namespace Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4.Transformacion.ModoDinoFull
{
    public class FromaDino : ModMount
    {
        public const int TotalFrames = 20;

        public override void SetStaticDefaults()
        {
            MountData.buff = ModContent.BuffType<Fosilizacion>();

            // Valores genéricos iniciales (se sobrescriben dinámicamente)
            MountData.runSpeed = 11f;
            MountData.dashSpeed = 11f;
            MountData.acceleration = 0.45f;
            MountData.swimSpeed = 8f;
            MountData.jumpHeight = 12;
            MountData.jumpSpeed = 6.5f;
            MountData.fallDamage = 0f;
            MountData.blockExtraJumps = false;
            MountData.constantJump = true;
            MountData.flightTimeMax = 0;
            MountData.fatigueMax = 0;
            MountData.usesHover = false;

            MountData.heightBoost = 20;
            MountData.xOffset = 0;
            MountData.yOffset = 4;
            MountData.bodyFrame = 3;
            MountData.playerHeadOffset = 22;

            int[] offsets = new int[TotalFrames];
            for (int i = 0; i < offsets.Length; i++) offsets[i] = 24;
            MountData.playerYOffsets = offsets;

            MountData.totalFrames = TotalFrames;

            MountData.standingFrameCount = 5;
            MountData.standingFrameStart = 0;
            MountData.standingFrameDelay = 10;

            MountData.runningFrameCount = 8;
            MountData.runningFrameStart = 8;
            MountData.runningFrameDelay = 100;

            MountData.inAirFrameCount = 1;
            MountData.inAirFrameStart = 5;
            MountData.inAirFrameDelay = 10;

            MountData.idleFrameCount = 5;
            MountData.idleFrameStart = 0;
            MountData.idleFrameDelay = 10;
            MountData.idleFrameLoop = true;

            MountData.swimFrameCount = 1;
            MountData.swimFrameStart = 5;
            MountData.swimFrameDelay = 10;

            if (!Main.dedServ)
            {
                MountData.textureWidth = MountData.backTexture.Width();
                MountData.textureHeight = MountData.backTexture.Height();
            }
        }

        public override void SetMount(Player player, ref bool skipDust)
        {
            skipDust = true;
            EfectoAzul(player);
        }

        public override void Dismount(Player player, ref bool skipDust)
        {
            skipDust = true;
            EfectoAzul(player);
        }

        static void EfectoAzul(Player player)
        {
            if (Main.dedServ) return;

            for (int i = 0; i < 30; i++)
            {
                Dust d = Dust.NewDustDirect(player.position, player.width, player.height, DustID.UltraBrightTorch, 0f, 0f, 100, default, 1.6f);
                d.noGravity = true;
                d.velocity *= 3f;
            }
            SoundEngine.PlaySound(SoundID.Item103, player.Center);
        }

        // AQUÍ ES DONDE SUCEDE LA MAGIA DINÁMICA
        public override void UpdateEffects(Player player)
        {
            MountData.runSpeed = DinoStatsHelper.GetVelocidadDino(player);
            MountData.dashSpeed = DinoStatsHelper.GetVelocidadDino(player);
            MountData.jumpHeight = DinoStatsHelper.GetAlturaSalto(player);
            MountData.jumpSpeed = DinoStatsHelper.GetFuerzaSalto(player);

            if (player.velocity.Y == 0f && System.Math.Abs(player.velocity.X) > 8f && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustDirect(
                    player.BottomLeft + new Vector2(0, -4), player.width, 4,
                    DustID.Smoke, -player.velocity.X * 0.2f, -1f, 150, default, 1f);
                d.noGravity = true;
            }
        }
    }
}