using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.NPCs.StandsNpc;
using Jojo.Content.Items;

namespace Jojo.Content.Items.Municion
{
    public class BalasStand : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 11;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 8;
            Item.height = 8;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.knockBack = 2f;
            Item.value = Item.sellPrice(copper: 50);
            Item.rare = ItemRarityID.LightRed;
            Item.shoot = ModContent.ProjectileType<BalasStand_Trayectoria>();
            Item.shootSpeed = 10f;
            Item.ammo = AmmoID.Bullet;
            Item.material = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe(50)
                .AddIngredient(ModContent.ItemType<LingoteMeteoritoJojo>(), 1)
                .AddIngredient(ItemID.MusketBall, 50)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }

    public class BalasStand_Trayectoria : ModProjectile
    {
        public override string Texture => "Jojo/Content/Items/Municion/BalasStand_Trayectoria";

        // Probabilidad de dar un Stand al impactar: 0.005f = 0.5%
        private const float ProbabilidadStand = 0.005f;

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.aiStyle = 0;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= 50;
                if (Projectile.alpha < 0)
                    Projectile.alpha = 0;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.YellowTorch,
                    0f, 0f,
                    100,
                    default,
                    1.2f
                );
                dust.noGravity = true;
                dust.velocity *= 0.2f;
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (target.townNPC)
                return true;

            return null;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Solo procesamos desde el cliente dueño del proyectil
            if (Main.myPlayer != Projectile.owner)
                return;

            if (target.type == NPCID.TargetDummy)
                return;

            // Tirada de probabilidad del 0.5%
            if (Main.rand.NextFloat() >= ProbabilidadStand)
                return;

            // Comprobaciones de elegibilidad del objetivo
            if (!GlobalStandSpawner.CanHaveStand(target))
                return;

            GlobalStandSpawner infoNPC = target.GetGlobalNPC<GlobalStandSpawner>();
            NPC miembroConStand = GlobalStandSpawner.FindLivingGroupMemberWithStand(target);

            if (infoNPC.hasStand || miembroConStand != null)
                return;

            // MULTIJUGADOR: Envía el paquete al servidor para sincronizar
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write(global::Jojo.Jojo.PacketType_GiveStandToNPC);
                packet.Write(target.whoAmI);
                packet.Send();
            }
            else // SINGLEPLAYER: Aplica el Stand directamente
            {
                infoNPC.initializedStand = true;
                infoNPC.hasStand = true;
                infoNPC.standType = GlobalStandSpawner.GetRandomStandType();
                infoNPC.standRespawnCooldown = 0;
                infoNPC.forceSpawn = true;
                target.netUpdate = true;
            }

            // --- ONDA EXPANSIVA DORADA ---
            for (int i = 0; i < 30; i++)
            {
                Vector2 velocity = new Vector2(0, -5f).RotatedBy(MathHelper.ToRadians(360f / 30 * i));
                int dust = Dust.NewDust(target.Center, 0, 0, DustID.GoldCoin, velocity.X, velocity.Y, 0, default, 1.5f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}