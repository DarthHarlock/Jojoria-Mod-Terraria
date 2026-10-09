using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using Jojo.Content.Clases;
using Jojo.Content.Buffs.MagiciansRed_Buffs;
using Jojo.Systems;
using Jojo.Content.Systems;

namespace Jojo.Content.Projectiles.MagiciansRed.MagiciansRed_Tier_4
{
    public class Fuego1_Tier_4 : ModProjectile
    {
        const string SkinBasePath = "Jojo/Content/Projectiles/Skins/MagiciansRed/";

        static string GetMagiciansRedSkinFolder(Player p)
        {
            if (UI.StandSlotSystem.HasMagiciansRedPinkSkinFor(p))
                return SkinBasePath + "Magicias_Pink/";

            if (UI.StandSlotSystem.HasMagiciansRedGreenSkinFor(p))
                return SkinBasePath + "Magicias_Green/";

            if (UI.StandSlotSystem.HasMagiciansRedBlueSkinFor(p))
                return SkinBasePath + "Magicias_Blue/";

            return null;
        }

        // --- MÉTODOS PARA OBTENER EL COLOR/TIPO DE PARTÍCULA SEGÚN LA SKIN ---
        private int GetTorchDust(Player p)
        {
            if (UI.StandSlotSystem.HasMagiciansRedBlueSkinFor(p)) return DustID.IceTorch; // Partícula de hielo (Azul)
            if (UI.StandSlotSystem.HasMagiciansRedPinkSkinFor(p)) return DustID.PinkTorch; // Partícula Rosa
            if (UI.StandSlotSystem.HasMagiciansRedGreenSkinFor(p)) return DustID.GreenTorch; // Partícula Verde
            return DustID.Torch; // Fuego normal
        }

        private int GetFlareDust(Player p)
        {
            if (UI.StandSlotSystem.HasMagiciansRedBlueSkinFor(p)) return DustID.Frost; // Efecto explosivo helado (Azul cyan brillante)
            if (UI.StandSlotSystem.HasMagiciansRedPinkSkinFor(p)) return DustID.Shadowflame; // Explosión oscura morada/rosa
            if (UI.StandSlotSystem.HasMagiciansRedGreenSkinFor(p)) return DustID.CursedTorch; // Explosión fuego maldito (Verde)
            return DustID.SolarFlare; // Explosión normal de fuego
        }

        private Vector3 GetLightColor(Player p)
        {
            if (UI.StandSlotSystem.HasMagiciansRedBlueSkinFor(p)) return new Vector3(0.1f, 0.4f, 0.9f); // Luz azul ambiental
            if (UI.StandSlotSystem.HasMagiciansRedPinkSkinFor(p)) return new Vector3(0.9f, 0.2f, 0.7f); // Luz rosa ambiental
            if (UI.StandSlotSystem.HasMagiciansRedGreenSkinFor(p)) return new Vector3(0.2f, 0.9f, 0.3f); // Luz verde ambiental
            return new Vector3(0.8f, 0.4f, 0.1f); // Luz naranja de fuego (Default)
        }
        // ---------------------------------------------------------------------

        public bool IsExploding
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.alpha = 0;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 0;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];

            // Obtenemos qué polvos y luz requiere esta skin antes de hacer la lógica
            int torchDust = GetTorchDust(p);
            int flareDust = GetFlareDust(p);
            Vector3 lightColor = GetLightColor(p);

            // --- LÓGICA DE EXPLOSIÓN (Sincronizada para TODOS los jugadores) ---
            if (IsExploding)
            {
                if (Projectile.localAI[1] == 0f)
                {
                    Projectile.localAI[1] = 1f;

                    Projectile.velocity = Vector2.Zero;
                    Projectile.tileCollide = false;
                    Projectile.alpha = 255;

                    Vector2 center = Projectile.Center;
                    Projectile.width = 120;
                    Projectile.height = 120;
                    Projectile.Center = center;

                    SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

                    if (Main.netMode != NetmodeID.Server)
                    {
                        // 1. Anillo Expansivo (Se adapta a la skin)
                        for (int i = 0; i < 70; i++)
                        {
                            Vector2 direction = Main.rand.NextVector2CircularEdge(Projectile.width / 2f, Projectile.height / 2f);
                            Dust dust = Dust.NewDustPerfect(Projectile.Center + (direction * 0.2f), torchDust, direction * 0.25f, 100, default, 2.8f);
                            dust.noGravity = true;
                        }

                        // 2. Explosión central (Se adapta a la skin)
                        for (int i = 0; i < 50; i++)
                        {
                            Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, flareDust, 0f, 0f, 100, default, 1.0f);
                            dust.velocity *= 3f;
                            dust.noGravity = true;
                        }

                        // 3. Humo (Lo dejamos igual, visualmente luce como vapor de hielo denso en la skin azul)
                        for (int i = 0; i < 30; i++)
                        {
                            Dust smoke = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 1.5f);
                            smoke.velocity *= 1.5f;
                        }
                    }
                }

                return;
            }

            // --- ARO DE FUEGO AL SPAWNEAR (Solo se ejecuta 1 vez por cliente) ---
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;

                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 20; i++) // 20 partículas formando un circulito
                    {
                        Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f);
                        // Adaptado al color
                        Dust ringDust = Dust.NewDustPerfect(Projectile.Center, torchDust, dir * 2.5f, 100, default, 1.6f);
                        ringDust.noGravity = true;
                    }
                }
            }
            // ---------------------------------------------------------

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= 4)
                {
                    Projectile.frame = 0;
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            // Modificamos la luz ambiental que emite para que cambie junto al color de la skin
            Lighting.AddLight(Projectile.Center, lightColor.X, lightColor.Y, lightColor.Z);

            if (Main.rand.NextBool(2) && Main.netMode != NetmodeID.Server)
            {
                // El rastro de partículas cuando vuela también adopta el color
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, torchDust, 0f, 0f, 100, default, 1.6f);
                dust.noGravity = true;
                dust.velocity = Projectile.velocity * -0.1f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<MagiciansFire_Tier_4>(), 720);

            if (!IsExploding) Explode();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!IsExploding) Explode();
            return false;
        }

        private void Explode()
        {
            if (IsExploding) return;

            IsExploding = true;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.timeLeft = 3;

            Vector2 center = Projectile.Center;
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.Center = center;

            Projectile.netUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            string skinFolder = GetMagiciansRedSkinFolder(owner);

            Texture2D texture = skinFolder != null
                ? ModContent.Request<Texture2D>(skinFolder + "Fuego1").Value
                : Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;

            int frameWidth = texture.Width / 4;
            int frameHeight = texture.Height;

            Rectangle sourceRectangle = new Rectangle(Projectile.frame * frameWidth, 0, frameWidth, frameHeight);
            Vector2 origin = new Vector2(frameWidth * 0.5f, frameHeight * 0.5f);

            SpriteEffects effects = SpriteEffects.None;

            if (Projectile.velocity.X < 0)
            {
                effects = SpriteEffects.FlipVertically;
            }

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                sourceRectangle,
                lightColor * ((255 - Projectile.alpha) / 255f),
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0
            );

            return false;
        }
    }
}