using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using Jojo.Content.Clases;
using Jojo.Content.NPCs.StandsNpc;

namespace Jojo.Content.NPCs.StandsNpc.HirofantGreen
{
    public class EsmeraldaNpc_Tier_1 : ModProjectile, IStandNpcProjectile
    {
        static readonly Color NeonGreen = new Color(60, 255, 90);

        public NPC OwnerNpc
        {
            get
            {
                int npcIndex = (int)Projectile.ai[1];
                if (npcIndex < 0 || npcIndex >= Main.maxNPCs) return null;
                NPC owner = Main.npc[npcIndex];
                return owner.active ? owner : null;
            }
        }

        public bool ResistsTimeStop => true;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.ArmorPenetration = 20;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            if (Projectile.timeLeft < 225) Projectile.tileCollide = true;

            bool isHostile = Projectile.ai[0] == 1f;
            Projectile.hostile = isHostile;
            Projectile.friendly = !isHostile;

            if (++Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 4;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.2f, 0.9f, 0.3f);

            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.GreenFairy, 0f, 0f, 100, default, 0.65f);
                dust.noGravity = true;
                dust.color = NeonGreen;
                dust.velocity = Projectile.velocity * -0.05f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int frameWidth = texture.Width / 4;
            Rectangle sourceRect = new Rectangle(frameWidth * Projectile.frame, 0, frameWidth, texture.Height);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, sourceRect, Color.White, Projectile.rotation, new Vector2(frameWidth / 2f, texture.Height / 2f), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}