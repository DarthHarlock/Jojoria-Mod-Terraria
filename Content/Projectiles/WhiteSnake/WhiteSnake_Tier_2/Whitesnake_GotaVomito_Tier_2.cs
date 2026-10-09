using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Buffs;
using Jojo.Content.Clases;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_2;
// Los rastros (suelo y pared) y el sistema del ácido viven en Tier 4; Tier 2 los hereda:
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_4;
using Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_4.Vomito;

namespace Jojo.Content.Projectiles.WhiteSnake.WhiteSnake_Tier_2.Vomito
{
    public class Whitesnake_GotaVomito_Tier_2 : ModProjectile
    {
        public override string Texture => "Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_2/Vomito/Gota1";

        // ==========================================
        // 🔧 ZONA DE CONFIGURACIÓN 🔧
        // ==========================================

        // Cuántas gotas lanza cada vómito (1 = una sola)
        public const int CANTIDAD_DE_GOTAS = 1;

        // Daño de la gota y del rastro:      WHITESNAKESTAND_Tier_2.DANO_GOTA_Y_RASTRO
        // Daño/duración/slow del ÁCIDO:      WHITESNAKESTAND_Tier_2 (constantes ACIDO_*)

        // ==========================================

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.hostile = false;

            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.timeLeft = 80;
            Projectile.penetrate = 1;
            Projectile.alpha = 0;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.ai[0] = Main.rand.Next(3);

            // DAÑO PROPIO DE LA GOTA (separado del ácido y de los puñetazos).
            // Los rastros copian este daño, así que también dependen de DANO_GOTA_Y_RASTRO.
            Player dueno = Main.player[Projectile.owner];
            Projectile.damage = (int)dueno.GetTotalDamage(ModContent.GetInstance<ClaseStand>())
                                          .ApplyTo(WHITESNAKESTAND_Tier_2.DANO_GOTA_Y_RASTRO);

            if (Projectile.ai[1] == 0f && Main.myPlayer == Projectile.owner)
            {
                Projectile.ai[1] = 1f;

                for (int i = 0; i < CANTIDAD_DE_GOTAS - 1; i++)
                {
                    Vector2 velocidadAlterada = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(30));
                    velocidadAlterada *= 1f - Main.rand.NextFloat(0.5f);

                    Projectile.NewProjectile(
                        source,
                        Projectile.position,
                        velocidadAlterada,
                        Projectile.type,
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner,
                        Main.rand.Next(3),
                        1f
                    );
                }
            }
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return true;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                if (Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                {
                    Projectile.Kill();
                    return;
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            Projectile.velocity.X *= 0.96f;
            Projectile.velocity.Y += 0.35f;
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
            }

            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Web, 0f, 0f, 150, Color.White, 0.8f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = Projectile.velocity * 0.4f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int gotaNum = (int)Projectile.ai[0] + 1;
            string texPath = "Jojo/Content/Projectiles/WhiteSnake/WhiteSnake_Tier_2/Vomito/Gota" + gotaNum;

            Texture2D tex = ModContent.Request<Texture2D>(texPath).Value;
            Vector2 drawOrigin = new Vector2(tex.Width * 0.5f, tex.Height * 0.5f);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(
                tex, drawPos, null, lightColor, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0
            );

            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // ÁCIDO (el debuff): sus números salen de las constantes ACIDO_* del stand Tier 2.
            AcidoBlancoNPC_Tier_4.AplicarDesdeProyectil(target, Projectile);

            for (int i = 0; i < 15; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Web, 0f, 0f, 150, Color.White, 1.2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 2.5f;
            }

            if (Main.myPlayer == Projectile.owner)
            {
                // RASTRO HEREDADO DE TIER 4
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    new Vector2(Projectile.Center.X, Projectile.Bottom.Y - 5f),
                    new Vector2(0, 1f),
                    ModContent.ProjectileType<Whitesnake_RastroSuelo_Tier_4>(),
                    Projectile.damage, // daño de la gota (DANO_GOTA_Y_RASTRO de Tier 2)
                    0f,
                    Projectile.owner
                );
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                if (Projectile.velocity.X != oldVelocity.X)
                {
                    int dirChoque = Math.Sign(oldVelocity.X);
                    int tileX = (int)((Projectile.Center.X + (Projectile.width * 0.5f + 4f) * dirChoque) / 16f);
                    int tileY = (int)(Projectile.Center.Y / 16f);

                    Tile tileCentral = Framing.GetTileSafely(tileX, tileY);
                    Tile tileArriba = Framing.GetTileSafely(tileX, tileY - 1);
                    Tile tileAbajo = Framing.GetTileSafely(tileX, tileY + 1);

                    if (tileCentral.HasUnactuatedTile && Main.tileSolid[tileCentral.TileType] &&
                        tileArriba.HasUnactuatedTile && Main.tileSolid[tileArriba.TileType] &&
                        tileAbajo.HasUnactuatedTile && Main.tileSolid[tileAbajo.TileType])
                    {
                        float spawnX;
                        if (dirChoque == 1)
                        {
                            spawnX = tileX * 16f;
                        }
                        else
                        {
                            spawnX = tileX * 16f + 16f;
                        }

                        Vector2 posRastro = new Vector2(spawnX, Projectile.Center.Y);

                        // RASTRO HEREDADO DE TIER 4
                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            posRastro,
                            Vector2.Zero,
                            ModContent.ProjectileType<Whitesnake_RastroPared_Tier_4>(),
                            Projectile.damage,
                            0f,
                            Projectile.owner,
                            dirChoque
                        );
                    }
                }
                else if (Projectile.velocity.Y != oldVelocity.Y && oldVelocity.Y > 0)
                {
                    Vector2 posSuelo = new Vector2(Projectile.Center.X, Projectile.Bottom.Y - 5f);

                    // RASTRO HEREDADO DE TIER 4
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        posSuelo,
                        Vector2.Zero,
                        ModContent.ProjectileType<Whitesnake_RastroSuelo_Tier_4>(),
                        Projectile.damage,
                        0f,
                        Projectile.owner
                    );
                }
            }
            return true;
        }
    }
}