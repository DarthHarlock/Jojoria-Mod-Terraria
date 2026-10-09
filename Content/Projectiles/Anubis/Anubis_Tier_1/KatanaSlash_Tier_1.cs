using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ID;
using System;
using System.IO;
using Jojo.Content.Players;
using Jojo.Content.UI;

namespace Jojo.Content.Projectiles.Anubis.Anubis_Tier_1
{
    public class KatanaSlash_Tier_1 : ModProjectile
    {
        private const int TotalFrames = 13;
        private const int FrameWidth = 160;
        private const int FrameHeight = 196;

        private int currentFrame = 0;
        private const float EscalaPersonalizada = 0.9f;

        private Vector2 ultimaPosicion = Vector2.Zero;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = (int)(140 * EscalaPersonalizada);
            Projectile.height = (int)(140 * EscalaPersonalizada);
            Projectile.friendly = true;
            Projectile.DamageType = ModContent.GetInstance<Clases.ClaseStand>();
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.ownerHitCheck = false;
            Projectile.aiStyle = -1;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;

            Projectile.netImportant = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.velocity.X);
            writer.Write(Projectile.velocity.Y);
            writer.Write(Projectile.scale);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            float velX = reader.ReadSingle();
            float velY = reader.ReadSingle();
            Projectile.velocity = new Vector2(velX, velY);
            Projectile.scale = reader.ReadSingle();
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            if (!player.active || player.dead || player.CCed)
            {
                Projectile.Kill();
                return;
            }

            int standID = ModContent.ProjectileType<ANUBISSTAND_Tier_1>();
            bool standActivo = false;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == standID && p.owner == Projectile.owner)
                {
                    standActivo = true;
                    break;
                }
            }

            bool isOwner = Projectile.owner == Main.myPlayer;

            if (!standActivo || (isOwner && !Main.mouseLeft))
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;

            if (isOwner)
            {
                Vector2 direccionCursor = Main.MouseWorld - player.MountedCenter;
                if (direccionCursor != Vector2.Zero)
                {
                    direccionCursor.Normalize();
                    Projectile.velocity = direccionCursor;
                }

                float escalaActual = EscalaPersonalizada;
                if (player.TryGetModPlayer(out AnubisSoulPlayer_Tier_1 anubisPlayer) && anubisPlayer.fundaAnubisEquipada)
                {
                    escalaActual *= 1.10f;
                }
                Projectile.scale = escalaActual;

                player.ChangeDir(Projectile.velocity.X > 0 ? 1 : -1);
            }

            float mitadTraseraDelSpriteEscalado = (FrameWidth / 2f) * Projectile.scale;
            float distanciaDeSeguridadAlPecho = 1f;
            float offsetDinamico = mitadTraseraDelSpriteEscalado + distanciaDeSeguridadAlPecho;

            Projectile.Center = player.MountedCenter + Projectile.velocity * offsetDinamico;
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (!isOwner)
            {
                player.ChangeDir(Projectile.velocity.X > 0 ? 1 : -1);
            }

            if (ultimaPosicion == Vector2.Zero)
            {
                ultimaPosicion = Projectile.Center;
            }

            float distanciaRecorrida = Vector2.Distance(ultimaPosicion, Projectile.Center);
            int pasosDeRelleno = Math.Max(1, (int)(distanciaRecorrida / 8f));

            bool tieneSkinRed = StandSlotSystem.HasRedSkinFor(player);
            bool tieneSkinGreen = StandSlotSystem.HasGreenSkinFor(player);
            bool tieneSkinBlue = StandSlotSystem.HasBlueSkinFor(player);

            int tipoDust = DustID.PurpleTorch;
            if (tieneSkinRed) tipoDust = DustID.RedTorch;
            else if (tieneSkinGreen) tipoDust = DustID.GreenTorch;
            else if (tieneSkinBlue) tipoDust = DustID.IceTorch;

            for (int paso = 0; paso < pasosDeRelleno; paso++)
            {
                float progreso = (float)paso / pasosDeRelleno;
                Vector2 posicionBaseSegmento = Vector2.Lerp(ultimaPosicion, Projectile.Center, progreso);

                int particulasPorSegmento = Main.rand.Next(3, 6);
                for (int i = 0; i < particulasPorSegmento; i++)
                {
                    float rangoX = (FrameWidth * Projectile.scale) * 0.45f;
                    float rangoY = (FrameHeight * Projectile.scale) * 0.45f;

                    Vector2 offsetAleatorio = new Vector2(
                        Main.rand.NextFloat(-rangoX, rangoX),
                        Main.rand.NextFloat(-rangoY, rangoY)
                    );

                    offsetAleatorio = offsetAleatorio.RotatedBy(Projectile.rotation);
                    Vector2 posicionParticula = posicionBaseSegmento + offsetAleatorio;
                    Vector2 velocidadParticula = Projectile.velocity * Main.rand.NextFloat(0.5f, 2.5f);

                    Dust d = Dust.NewDustPerfect(
                        posicionParticula,
                        tipoDust,
                        velocidadParticula,
                        100,
                        default,
                        Main.rand.NextFloat(1.2f, 2.2f)
                    );

                    d.noGravity = true;
                    d.velocity *= 0.1f;
                    d.scale -= Main.rand.NextFloat(0.03f, 0.12f);
                }
            }

            ultimaPosicion = Projectile.Center;

            float velocidadStat = player.GetModPlayer<StandStatsPlayer>().standSpeed;

            if (velocidadStat < 50f)
            {
                Projectile.localNPCHitCooldown = 14;
            }
            else if (velocidadStat < 100f)
            {
                Projectile.localNPCHitCooldown = 10;
            }
            else if (velocidadStat < 150f)
            {
                Projectile.localNPCHitCooldown = 7;
            }
            else
            {
                Projectile.localNPCHitCooldown = 4;
            }

            float velocidadDeAnimacion = 1f;
            if (velocidadStat >= 50f) velocidadDeAnimacion = 1.5f;
            if (velocidadStat >= 100f) velocidadDeAnimacion = 2.2f;
            if (velocidadStat >= 150f) velocidadDeAnimacion = 3.5f;

            Projectile.frameCounter += (int)velocidadDeAnimacion;

            if (velocidadDeAnimacion > 1f && Main.rand.NextFloat() < (velocidadDeAnimacion - (int)velocidadDeAnimacion))
            {
                Projectile.frameCounter++;
            }

            int ticksLimiteParaCambiarDeFrame = 2;

            if (Projectile.frameCounter >= ticksLimiteParaCambiarDeFrame)
            {
                Projectile.frameCounter = 0;
                currentFrame++;

                if (currentFrame >= TotalFrames)
                {
                    currentFrame = 0;
                }
            }

            if (isOwner)
            {
                Projectile.netUpdate = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player player = Main.player[Projectile.owner];

            string rutaTextura = Texture;
            if (StandSlotSystem.HasRedSkinFor(player))
            {
                rutaTextura = "Jojo/Content/Projectiles/Skins/Anubis/Red/KatanaSlash";
            }
            else if (StandSlotSystem.HasGreenSkinFor(player))
            {
                rutaTextura = "Jojo/Content/Projectiles/Skins/Anubis/Green/KatanaSlash";
            }
            else if (StandSlotSystem.HasBlueSkinFor(player))
            {
                rutaTextura = "Jojo/Content/Projectiles/Skins/Anubis/Blue/KatanaSlash";
            }

            Texture2D texture = ModContent.Request<Texture2D>(rutaTextura).Value;
            Rectangle sourceRectangle = new Rectangle(currentFrame * FrameWidth, 0, FrameWidth, FrameHeight);
            Vector2 drawOrigin = new Vector2(FrameWidth / 2f, FrameHeight / 2f);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            SpriteEffects effects = SpriteEffects.None;
            float rotacionFinal = Projectile.rotation;

            if (Projectile.velocity.X < 0)
            {
                effects = SpriteEffects.FlipHorizontally;
                rotacionFinal += MathHelper.Pi;
            }

            Main.EntitySpriteDraw(texture, drawPosition, sourceRectangle, Color.White, rotacionFinal, drawOrigin, Projectile.scale, effects, 0);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Player player = Main.player[Projectile.owner];
            float collisionPoint = 0f;

            float largoDelTajo = FrameWidth * Projectile.scale;
            float grosorLinea = FrameHeight * 0.7f * Projectile.scale;

            Vector2 startPoint = player.MountedCenter + Projectile.velocity * 5f;
            Vector2 endPoint = player.MountedCenter + Projectile.velocity * (largoDelTajo + 10f);

            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), startPoint, endPoint, grosorLinea, ref collisionPoint))
            {
                return true;
            }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.life <= 0)
            {
                Player player = Main.player[Projectile.owner];

                if (player.TryGetModPlayer(out AnubisSoulPlayer_Tier_1 anubisPlayer))
                {
                    anubisPlayer.CosecharEnemigo(target);
                }
            }
        }
    }
}