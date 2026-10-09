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
    public class HirofantGreenNpcProj : ModProjectile, IStandNpcProjectile
    {
        public int life = 800;
        private int shootTimer = 0;

        public bool ResistsTimeStop => true;
        public NPC OwnerNpc => (Projectile.ai[0] >= 0 && Projectile.ai[0] < Main.maxNPCs) ? Main.npc[(int)Projectile.ai[0]] : null;

        public override void SetDefaults()
        {
            Projectile.width = 88;
            Projectile.height = 80;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.damage = 10;
        }

        public override void AI()
        {
            NPC owner = OwnerNpc;
            if (owner == null || !owner.active || life <= 0) { Projectile.Kill(); return; }

            // Configurar hostilidad
            bool ownerIsHostile = !owner.friendly && !owner.townNPC;
            Projectile.hostile = ownerIsHostile;
            Projectile.friendly = !ownerIsHostile;

            // Movimiento base (siempre cerca del dueño)
            Vector2 idlePos = owner.Center + new Vector2(-35 * owner.direction, -15);
            Projectile.Center = Vector2.Lerp(Projectile.Center, idlePos, 0.15f);
            Projectile.spriteDirection = owner.direction;

            // Buscar objetivo
            Entity target = null;
            float closestDist = 700f * 700f;

            if (ownerIsHostile)
            {
                foreach (Player p in Main.player)
                {
                    if (p.active && !p.dead && Vector2.DistanceSquared(owner.Center, p.Center) < closestDist)
                    {
                        target = p; closestDist = Vector2.DistanceSquared(owner.Center, p.Center);
                    }
                }
            }
            else
            {
                foreach (NPC n in Main.npc)
                {
                    if (n.active && !n.friendly && n.damage > 0 && Vector2.DistanceSquared(owner.Center, n.Center) < closestDist)
                    {
                        target = n; closestDist = Vector2.DistanceSquared(owner.Center, n.Center);
                    }
                }
            }

            // Disparar
            if (target != null)
            {
                Projectile.spriteDirection = (target.Center.X > owner.Center.X) ? 1 : -1; // Mirar al objetivo
                if (++shootTimer >= 40 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    shootTimer = 0;
                    Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                    SoundEngine.PlaySound(SoundID.Item103 with { Pitch = 0.2f }, Projectile.Center);

                    for (int i = -1; i <= 1; i++)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, dir.RotatedBy(MathHelper.ToRadians(12f * i)) * 14f, ModContent.ProjectileType<EsmeraldaNpc_Tier_1>(), Projectile.damage, 2f, Main.myPlayer, ownerIsHostile ? 1f : 0f, owner.whoAmI);
                    }
                }
            }
            else { shootTimer = 0; }

            // Animación simple
            if (++Projectile.frameCounter >= 9)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 4; // Asumiendo 4 frames de idle
            }

            RecibirDano(owner);
        }

        private void RecibirDano(NPC owner)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.damage > 0 && p.friendly != Projectile.friendly && p.Hitbox.Intersects(Projectile.Hitbox))
                {
                    life -= p.damage;
                    if (life <= 0)
                    {
                        owner.life -= (int)(owner.lifeMax * 0.10f); // Castigo al morir
                        if (owner.life <= 0) owner.checkDead();
                        Projectile.Kill();
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            int frameHeight = 80; // Ajusta a la altura de tu frame
            Rectangle rect = new Rectangle(0, Projectile.frame * frameHeight, 88, frameHeight);
            SpriteEffects flip = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition + new Vector2(0, -4f), rect, lightColor, Projectile.rotation, new Vector2(44, frameHeight / 2f), Projectile.scale, flip, 0f);
            return false;
        }
    }
}