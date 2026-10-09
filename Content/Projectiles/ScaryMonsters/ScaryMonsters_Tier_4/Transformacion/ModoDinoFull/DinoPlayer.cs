using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_3;

namespace Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4.Transformacion.ModoDinoFull
{
    // ==============================================================
    // HELPER: Decide qué estadísticas usar dependiendo del Tier activo
    // ==============================================================
    public static class DinoStatsHelper
    {
        public static bool HasTier4(Player player) => player.ownedProjectileCounts[ModContent.ProjectileType<MONSTERSTAND_Tier_4>()] > 0;
        public static bool HasTier3(Player player) => player.ownedProjectileCounts[ModContent.ProjectileType<MONSTERSTAND_Tier_3>()] > 0;

        public static bool HasStandOut(Player player) => HasTier4(player) || HasTier3(player);

        public static float GetVelocidadDino(Player player) => HasTier4(player) ? MONSTERSTAND_Tier_4.VelocidadDino : (HasTier3(player) ? MONSTERSTAND_Tier_3.VelocidadDino : 11f);
        public static int GetAlturaSalto(Player player) => HasTier4(player) ? MONSTERSTAND_Tier_4.AlturaSalto : (HasTier3(player) ? MONSTERSTAND_Tier_3.AlturaSalto : 12);
        public static float GetFuerzaSalto(Player player) => HasTier4(player) ? MONSTERSTAND_Tier_4.FuerzaSalto : (HasTier3(player) ? MONSTERSTAND_Tier_3.FuerzaSalto : 6.5f);
        public static float GetFuerzaSaltoExtra(Player player) => HasTier4(player) ? MONSTERSTAND_Tier_4.FuerzaSaltoExtra : (HasTier3(player) ? MONSTERSTAND_Tier_3.FuerzaSaltoExtra : 9.5f);
        public static int GetMaxSaltosExtra(Player player) => HasTier4(player) ? MONSTERSTAND_Tier_4.MaxSaltosExtra : (HasTier3(player) ? MONSTERSTAND_Tier_3.MaxSaltosExtra : 1);
        public static float GetDanoPisoton(Player player) => HasTier4(player) ? MONSTERSTAND_Tier_4.DanoPisoton : (HasTier3(player) ? MONSTERSTAND_Tier_3.DanoPisoton : 130f);
    }

    public class DinoPlayer : ModPlayer
    {
        public static float PorcentajeAumentoDanio = 0.04f;
        public static float PorcentajeVelocidadMovimiento = 0.80f;
        public static float MultiplicadorVelocidadMaxima = 1.80f;
        public static float MultiplicadorAceleracion = 1.80f;
        public static float ImpulsoPotenciaSalto = 3.5f;

        const int IdleStart = 0, IdleCount = 5;
        const int AirUp = 5;
        const int JumpPrep = 6;
        const int AirFall = 7;
        const int WalkStart = 8, WalkCount = 8;

        public int DinoFrame;
        int idleCounter, walkCounter, airTicks;

        public int saltosExtraRestantes;
        private bool jumpPressedPrev;

        public bool IsDino => Player.mount.Active && Player.mount.Type == ModContent.MountType<FromaDino>();

        public override void PostUpdateEquips()
        {
            if (DinoStatsHelper.HasStandOut(Player) && !IsDino)
            {
                Player.GetDamage(DamageClass.Generic) += PorcentajeAumentoDanio;
                Player.moveSpeed += PorcentajeVelocidadMovimiento;
            }
        }

        public override void PostUpdateRunSpeeds()
        {
            if (DinoStatsHelper.HasStandOut(Player) && !IsDino)
            {
                Player.maxRunSpeed *= MultiplicadorVelocidadMaxima;
                Player.accRunSpeed *= MultiplicadorVelocidadMaxima;
                Player.runAcceleration *= MultiplicadorAceleracion;
                Player.jumpSpeedBoost += ImpulsoPotenciaSalto;
            }
        }

        public override void HideDrawLayers(PlayerDrawSet drawInfo)
        {
            if (!IsDino) return;

            PlayerDrawLayers.Skin.Hide();
            PlayerDrawLayers.Leggings.Hide();
            PlayerDrawLayers.Shoes.Hide();
            PlayerDrawLayers.Torso.Hide();
            PlayerDrawLayers.Head.Hide();
            PlayerDrawLayers.Wings.Hide();
            PlayerDrawLayers.BackAcc.Hide();
            PlayerDrawLayers.FrontAccFront.Hide();
            PlayerDrawLayers.Shield.Hide();
            PlayerDrawLayers.HeldItem.Hide();
            PlayerDrawLayers.ArmOverItem.Hide();
            PlayerDrawLayers.HandOnAcc.Hide();
            PlayerDrawLayers.BladedGlove.Hide();
            PlayerDrawLayers.ProjectileOverArm.Hide();
            PlayerDrawLayers.MountBack.Hide();
            PlayerDrawLayers.MountFront.Hide();
        }

        public override void PostUpdate()
        {
            if (!IsDino)
            {
                idleCounter = walkCounter = airTicks = 0;
                DinoFrame = 0;
                saltosExtraRestantes = 0;
                return;
            }

            bool grounded = Player.velocity.Y == 0f;
            float vx = Math.Abs(Player.velocity.X);

            if (grounded)
            {
                saltosExtraRestantes = DinoStatsHelper.GetMaxSaltosExtra(Player);
            }
            else
            {
                bool pressJumpNow = Player.controlJump && !jumpPressedPrev;
                if (pressJumpNow && saltosExtraRestantes > 0)
                {
                    Player.velocity.Y = -DinoStatsHelper.GetFuerzaSaltoExtra(Player);
                    saltosExtraRestantes--;
                    SoundEngine.PlaySound(SoundID.DoubleJump, Player.Center);
                    for (int i = 0; i < 8; i++)
                    {
                        Dust d = Dust.NewDustDirect(Player.BottomLeft, Player.width, 4, DustID.Smoke, 0f, 2f);
                        d.noGravity = true;
                    }
                }
            }
            jumpPressedPrev = Player.controlJump;

            if (!grounded)
            {
                airTicks++;
                if (airTicks <= 5 && Player.velocity.Y < 0f) DinoFrame = JumpPrep;
                else if (Player.velocity.Y > 1f) DinoFrame = AirFall;
                else DinoFrame = AirUp;
            }
            else
            {
                airTicks = 0;
                if (vx < 0.3f)
                {
                    walkCounter = 0;
                    idleCounter++;
                    DinoFrame = IdleStart + (idleCounter / 10) % IdleCount;
                }
                else
                {
                    idleCounter = 0;
                    walkCounter += (int)(vx * 10f);
                    DinoFrame = WalkStart + (walkCounter / 700) % WalkCount;
                }
            }
        }
    }

    public class DinoDrawLayer : PlayerDrawLayer
    {
        public const int FrameHeight = 58;
        public const int FrameStride = 58;
        const float OffsetX = 0f;
        const float OffsetY = 6f;

        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.EyebrellaCloud);
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.drawPlayer.GetModPlayer<DinoPlayer>().IsDino;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            DinoPlayer dp = player.GetModPlayer<DinoPlayer>();
            drawInfo.DrawDataCache.Clear();

            Texture2D tex = ModContent.Request<Texture2D>(ModContent.GetInstance<FromaDino>().Texture).Value;
            int maxFrames = Math.Max(1, tex.Height / FrameStride);
            int f = Math.Min(dp.DinoFrame, maxFrames - 1);

            int srcH = Math.Min(FrameHeight, tex.Height - f * FrameStride);
            Rectangle source = new Rectangle(0, f * FrameStride, tex.Width, srcH);

            Vector2 origin = new Vector2(tex.Width / 2f, srcH);
            Vector2 pos = new Vector2(
                (int)(player.Center.X - Main.screenPosition.X + OffsetX),
                (int)(player.Bottom.Y - Main.screenPosition.Y + player.gfxOffY + OffsetY));

            Color color = Lighting.GetColor((int)(player.Center.X / 16f), (int)(player.Center.Y / 16f));
            SpriteEffects fx = player.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            drawInfo.DrawDataCache.Add(new DrawData(tex, pos, source, color, 0f, origin, 1f, fx, 0));
        }
    }
}