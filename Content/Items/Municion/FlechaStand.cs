using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.NPCs.StandsNpc;

namespace Jojo.Content.Items.Municion
{
    public class FlechaStand : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 1;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 14;
            Item.height = 32;
            Item.maxStack = 999;
            Item.consumable = true;
            Item.knockBack = 2f;
            Item.value = Item.sellPrice(silver: 5);
            Item.rare = ItemRarityID.LightRed;
            Item.shoot = ModContent.ProjectileType<FlechaStandProj>();
            Item.shootSpeed = 4.5f;
            Item.ammo = AmmoID.Arrow;
            Item.material = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe(5)
                .AddIngredient(ModContent.ItemType<LingoteMeteoritoJojo>(), 1)
                .AddIngredient(ItemID.WoodenArrow, 5)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }

    public class FlechaStandProj : ModProjectile
    {
        public override string Texture => "Jojo/Content/Items/Municion/FlechaStand";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.aiStyle = ProjAIStyleID.Arrow;

            AIType = ProjectileID.WoodenArrowFriendly;

            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
            Projectile.penetrate = 1;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                null,
                lightColor,
                Projectile.rotation + MathHelper.Pi,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (target.townNPC)
                return true;

            return null;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Solo procesamos la colisión desde el cliente dueño de la flecha
            if (Projectile.owner != Main.myPlayer)
                return;

            if (!GlobalStandSpawner.CanHaveStand(target))
            {
                SoltarFlecha();
                return;
            }

            GlobalStandSpawner infoNPC = target.GetGlobalNPC<GlobalStandSpawner>();
            NPC miembroConStand = GlobalStandSpawner.FindLivingGroupMemberWithStand(target);

            if (infoNPC.hasStand || miembroConStand != null)
            {
                SoltarFlecha();
                return;
            }

            // MULTIJUGADOR: El cliente le pide al Servidor que aplique el Stand
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write(global::Jojo.Jojo.PacketType_GiveStandToNPC);
                packet.Write(target.whoAmI);
                packet.Send();
            }
            else // SINGLEPLAYER: El juego local aplica el Stand directamente
            {
                infoNPC.initializedStand = true;
                infoNPC.hasStand = true;
                // Vuelve a ser completamente aleatorio basándose en la lista AllStandTypes
                infoNPC.standType = GlobalStandSpawner.GetRandomStandType();
                infoNPC.standRespawnCooldown = 0;
                infoNPC.forceSpawn = true;
                target.netUpdate = true;
            }

            // Efecto de partículas doradas
            for (int i = 0; i < 30; i++)
            {
                Vector2 velocity = new Vector2(0, -5f).RotatedBy(MathHelper.ToRadians(360f / 30 * i));
                int dust = Dust.NewDust(target.Center, 0, 0, DustID.GoldCoin, velocity.X, velocity.Y, 0, default, 1.5f);
                Main.dust[dust].noGravity = true;
            }

            SoltarFlecha();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoltarFlecha();
            return true;
        }

        private void SoltarFlecha()
        {
            // Solo se genera la flecha física en Singleplayer o en el dueño del proyectil para evitar duplicados
            if (Main.myPlayer == Projectile.owner)
            {
                int item = Item.NewItem(
                    Projectile.GetSource_DropAsItem(),
                    Projectile.Hitbox,
                    ModContent.ItemType<FlechaStand>()
                );

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, item, 1f);
                }
            }
        }
    }
}