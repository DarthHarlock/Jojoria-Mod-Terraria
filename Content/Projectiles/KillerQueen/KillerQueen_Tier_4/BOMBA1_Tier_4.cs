using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using System;
using System.IO;
using Jojo.Content.Clases;
using Jojo.Content.UI;
using Jojo.Content.Items;
using Jojo.Content.Items.Potenciadores;

namespace Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_4
{
    public class BOMBA1_Tier_4 : ModProjectile
    {
        public int attachedNPC = -1;
        Vector2 npcOffset;

        private bool firstTickSync = false;
        public float ExplosionKnockback = 0f;

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 999999;
            Projectile.hide = false;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override bool? CanDamage() => false;
        public override bool? CanCutTiles() => false;
        public override bool MinionContactDamage() => false;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(attachedNPC);
            writer.Write(npcOffset.X);
            writer.Write(npcOffset.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            attachedNPC = reader.ReadInt32();
            npcOffset = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            if (!firstTickSync && Projectile.owner == Main.myPlayer)
            {
                firstTickSync = true;
                if (Main.netMode != NetmodeID.SinglePlayer)
                {
                    Projectile.netUpdate = true;
                }
            }

            if (attachedNPC != -1)
            {
                NPC npc = Main.npc[attachedNPC];

                if (!npc.active || npc.life <= 0)
                {
                    Projectile.Kill();
                    return;
                }

                Projectile.Center = npc.Center + npcOffset;
            }
        }

        public void AttachToNPC(NPC npc)
        {
            attachedNPC = npc.whoAmI;
            npcOffset = Projectile.Center - npc.Center;

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                Projectile.netUpdate = true;
            }
        }

        public void Explode(Player owner)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                // Antes (Versión gigante): 180f | Ahora: 160f - Mantiene la escala masiva pero un pelín más reducida.
                float radius = 160f;

                float baseDamage = 160f;
                float variation = Main.rand.NextFloat(0.85f, 1.15f);
                float finalBaseDamage = baseDamage * variation;

                if (owner.TryGetModPlayer(out BombaDamagePlayer bombaPlayer))
                {
                    finalBaseDamage *= (1f + bombaPlayer.bombaDamageMult);
                }

                int dmg = (int)owner
                    .GetTotalDamage(ModContent.GetInstance<ClaseStand>())
                    .ApplyTo(finalBaseDamage);

                int critChance = StandCritSystem.GetFinalCritChance(owner, 5);

                foreach (NPC npc in Main.npc)
                {
                    if (!npc.active || npc.life <= 0)
                        continue;

                    if (Vector2.Distance(npc.Center, Projectile.Center) > radius)
                        continue;

                    if (npc.immune[owner.whoAmI] > 0)
                        continue;

                    NPC.HitInfo hit = new()
                    {
                        Damage = dmg,
                        Knockback = 0f,
                        HitDirection = npc.Center.X > Projectile.Center.X ? 1 : -1,
                        Crit = Main.rand.Next(100) < critChance
                    };

                    npc.StrikeNPC(hit);
                    npc.immune[owner.whoAmI] = Projectile.localNPCHitCooldown;

                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        NetMessage.SendStrikeNPC(npc, hit);
                    }
                }

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player targetPlayer = Main.player[i];

                    if (targetPlayer.active && !targetPlayer.dead && targetPlayer.whoAmI != owner.whoAmI && targetPlayer.hostile && owner.hostile)
                    {
                        if (Vector2.Distance(targetPlayer.Center, Projectile.Center) <= radius)
                        {
                            int hitDirection = targetPlayer.Center.X > Projectile.Center.X ? 1 : -1;
                            var deathReason = Terraria.DataStructures.PlayerDeathReason.ByProjectile(owner.whoAmI, Projectile.whoAmI);

                            if (Main.netMode == NetmodeID.SinglePlayer)
                            {
                                targetPlayer.Hurt(deathReason, dmg, hitDirection, true);
                            }
                            else
                            {
                                NetMessage.SendData(MessageID.HurtPlayer, -1, -1, null, targetPlayer.whoAmI, hitDirection, dmg, 1f, 0, 0, 0);
                            }
                        }
                    }
                }
            }

            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            // Antes: 200 | Ahora: 175
            int smokeParticles = 175;
            for (int i = 0; i < smokeParticles; i++)
            {
                // Antes: 15.0f | Ahora: 13.0f - Sigue siendo enorme pero un poco más controlada
                Vector2 velocity = Main.rand.NextVector2Circular(13.0f, 13.0f);
                Dust smoke = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Smoke,
                    velocity.X, velocity.Y, 130, Color.DarkGray, Main.rand.NextFloat(2.5f, 3.8f));

                smoke.noGravity = true;
                smoke.fadeIn = 1.4f;
            }

            // Antes: 120 | Ahora: 110
            int scatterFire = 110;
            for (int i = 0; i < scatterFire; i++)
            {
                // Antes: 14.0f | Ahora: 12.0f
                Vector2 velocity = Main.rand.NextVector2Circular(12.0f, 12.0f);
                Dust fire = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Torch,
                    velocity.X, velocity.Y, 100, default, Main.rand.NextFloat(2.0f, 3.0f));

                fire.noGravity = true;
                fire.velocity = velocity * 1.35f;
            }

            // Antes: 150 | Ahora: 130
            int fireRingParticles = 130;
            // Antes: 12.0f | Ahora: 10.5f
            float ringSpeed = 10.5f;

            for (int i = 0; i < fireRingParticles; i++)
            {
                float angle = i * (MathHelper.TwoPi / fireRingParticles);
                Vector2 velocity = angle.ToRotationVector2() * ringSpeed;

                Dust ringDust = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Torch,
                    velocity.X, velocity.Y, 100, default, Main.rand.NextFloat(1.8f, 2.5f));

                ringDust.noGravity = true;
                ringDust.velocity = velocity;
            }

            for (int g = 0; g < 3; g++)
            {
                Terraria.Gore.NewGore(
                    Projectile.GetSource_Death(),
                    Projectile.Center,
                    new Vector2(Main.rand.NextFloat(-4.0f, 4.0f), Main.rand.NextFloat(-4.0f, 4.0f)),
                    Main.rand.Next(61, 64),
                    1.1f
                );
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            bool megumin = StandSlotSystem.HasMeguminSkinFor(owner);
            bool isRed = StandSlotSystem.HasKillerQueenRedSkinFor(owner);
            bool isBlue = StandSlotSystem.HasKillerQueenBlueSkinFor(owner);
            bool isGreen = StandSlotSystem.HasKillerQueenGreenSkinFor(owner);

            string path;
            if (megumin) path = "Jojo/Content/Projectiles/Skins/KillerQueen/Megumin/BOMBA1_Megumin";
            else if (isRed) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Red/BOMBA1_Red";
            else if (isBlue) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Blue/BOMBA1_Blue";
            else if (isGreen) path = "Jojo/Content/Projectiles/Skins/KillerQueen/KillerQueen_Green/BOMBA1_Green";
            else path = "Jojo/Content/Projectiles/KillerQueen/KillerQueen_Tier_4/BOMBA1_Tier_4";

            Texture2D texture = ModContent.Request<Texture2D>(path).Value;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() / 2f;

            Color baseColor = Color.White;
            Color glowColor = Color.White * 1.2f;

            Main.EntitySpriteDraw(texture, drawPos, null, baseColor, 0f, origin, 1f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPos, null, glowColor, 0f, origin, 1f, SpriteEffects.None);

            return false;
        }
    }
}