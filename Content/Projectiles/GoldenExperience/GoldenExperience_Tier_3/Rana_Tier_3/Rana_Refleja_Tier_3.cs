using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_3.Rana_Tier_3
{
    public class Rana_Refleja_Tier_3 : ModNPC
    {
        public override string Texture => "Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Tier_3/Rana_Tier_3/RanaRefleja_Tier_3";

        const int FrameWidth = 20;
        const int FrameHeight = 20;

        bool spawnParticulas = true;
        int frameActual = 0;
        int waitTimer = 0;
        int animTimer = 0;

        // =========================================================================
        // FIX MULTIJUGADOR (partículas de muerte invisibles fuera de singleplayer):
        // SacrificarRana() generaba su dust directamente y solo se ejecutaba de
        // forma fiable en el servidor. El dust NUNCA viaja solo por red en Terraria,
        // así que en multijugador el servidor lo creaba en su copia local y ningún
        // cliente se enteraba -> la rana "desaparecía en seco".
        //
        // Mismo fix que en Mariposa_Refleja_Tier_3: un flag sincronizado por
        // SendExtraAI/ReceiveExtraAI que, al detectar la transición false -> true,
        // dispara el efecto DENTRO de ReceiveExtraAI (eso sí corre en cada cliente
        // al procesar el paquete). El aviso se manda en un paquete separado, con la
        // rana TODAVÍA ACTIVA, antes de desactivarla de verdad — si fuera en el
        // mismo paquete que el active = false, el sync no llevaría los datos
        // completos y el flag nunca llegaría a los clientes.
        // =========================================================================
        bool muriendo;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 6;
            NPCID.Sets.ActsLikeTownNPC[NPC.type] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = FrameWidth;
            NPC.height = FrameHeight;
            NPC.lifeMax = 100;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.knockBackResist = 0.5f;
            NPC.friendly = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.aiStyle = -1;
            NPC.noGravity = false;

            // Reseteamos el flag por si el slot de NPC se reutiliza para un spawn nuevo.
            muriendo = false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(muriendo);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            bool muriendoPrevio = muriendo;
            muriendo = reader.ReadBoolean();

            // Transición false -> true: esta es la señal de "la rana acaba de morir"
            // llegando a un cliente. Reproducimos aquí el efecto porque este código
            // corre en CADA cliente al procesar el paquete de sync.
            if (muriendo && !muriendoPrevio)
            {
                ReproducirEfectosMuerte();
            }
        }

        public override void AI()
        {
            // --- SOLO EL SERVIDOR / SINGLEPLAYER DECIDE SI LA RANA VIVE O MUERE ---
            // Antes esta comprobación no tenía guard de netMode, así que un cliente
            // podía llegar a evaluarla también por su cuenta con datos que pueden ir
            // con retraso (ownedProjectileCounts, sync del Stand, etc.). Igual que se
            // corrigió en la mariposa, dejamos que solo la simulación autoritativa
            // decida esto.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int ownerIndex = (int)NPC.ai[3];
                if (ownerIndex >= 0 && ownerIndex < Main.maxPlayers)
                {
                    Player owner = Main.player[ownerIndex];
                    int standType = ModContent.ProjectileType<GOLDENSTAND_Tier_3>();

                    if (!owner.active || owner.dead || owner.ownedProjectileCounts[standType] <= 0)
                    {
                        SacrificarRana();
                        return;
                    }
                }
            }

            if (spawnParticulas)
            {
                for (int i = 0; i < 20; i++)
                {
                    int polvo = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.YellowStarDust, 0f, 0f, 100, default, 1.5f);
                    Main.dust[polvo].noGravity = true;
                    Main.dust[polvo].velocity *= 1.2f;
                }
                spawnParticulas = false;
            }

            if (NPC.velocity.Y > 10f) NPC.velocity.Y = 10f;

            if (frameActual == 0)
            {
                waitTimer++;
                if (waitTimer > 75)
                {
                    waitTimer = 0;
                    frameActual = 1;
                }
            }
            else
            {
                animTimer++;
                if (animTimer > 6)
                {
                    animTimer = 0;
                    frameActual++;
                    if (frameActual >= 6) frameActual = 1;
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                ChequearColisiones();
            }
        }

        void ChequearColisiones()
        {
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.damage > 0 && npc.Hitbox.Intersects(NPC.Hitbox) && npc.whoAmI != NPC.whoAmI)
                {
                    AplicarReflejo(npc, npc.damage);
                    return;
                }
            }

            foreach (Projectile proj in Main.projectile)
            {
                if (proj.active && proj.hostile && proj.damage > 0 && proj.Hitbox.Intersects(NPC.Hitbox))
                {
                    int identity = proj.identity;
                    int owner = proj.owner;
                    proj.Kill();

                    // FIX: Sincronizar la muerte del proyectil enemigo a los clientes
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, identity, owner);

                    SacrificarRana();
                    return;
                }
            }
        }

        void AplicarReflejo(NPC atacante, int dañoRecibido)
        {
            float baseDamage = 65f; // Daño base 
            int ownerIndex = (int)NPC.ai[3];

            if (ownerIndex >= 0 && ownerIndex < Main.maxPlayers)
            {
                Player owner = Main.player[ownerIndex];
                if (owner.active && !owner.dead)
                {
                    baseDamage = owner.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);
                }
            }

            // MODIFICADO: Ahora multiplica dañoRecibido por 0.5f para tomar solo el 50%
            int dañoReflejado = (int)baseDamage + (int)(dañoRecibido * 0.7f);

            for (int i = 0; i < 15; i++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-3f, -1f));
            }

            // FIX MULTIJUGADOR: Generamos el impacto mediante el proyectil invisible (como en la mariposa)
            if (atacante != null && atacante.active && Main.netMode != NetmodeID.MultiplayerClient && ownerIndex >= 0 && ownerIndex < Main.maxPlayers)
            {
                int proj = Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    atacante.position,
                    Vector2.Zero,
                    ModContent.ProjectileType<RanaGolpeProjectile_Tier_3>(),
                    dañoReflejado,
                    0f,
                    ownerIndex
                );

                if (proj >= 0 && proj < Main.maxProjectiles)
                {
                    Projectile p = Main.projectile[proj];
                    p.width = atacante.width + 8;
                    p.height = atacante.height + 8;
                    p.Center = atacante.Center;
                    p.netUpdate = true;

                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, proj);
                }
            }

            SacrificarRana();
        }

        public override bool CheckDead()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                SacrificarRana();
            return false;
        }

        void ReproducirEfectosMuerte()
        {
            for (int i = 0; i < 30; i++)
            {
                int polvo = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.YellowStarDust, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 100, default, 1.8f);
                Main.dust[polvo].noGravity = true;
                Main.dust[polvo].velocity *= 1.5f;
            }

            SoundEngine.PlaySound(NPC.DeathSound, NPC.Center);
        }

        void SacrificarRana()
        {
            if (!NPC.active) return;

            // --- Efecto local inmediato en Servidor/Singleplayer ---
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                ReproducirEfectosMuerte();
            }

            if (Main.netMode == NetmodeID.Server)
            {
                muriendo = true;
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }

            // PASO 2: desactivamos y sincronizamos
            NPC.active = false;

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle sourceRect = new Rectangle(0, frameActual * FrameHeight, FrameWidth, FrameHeight);
            Vector2 origin = new Vector2(FrameWidth / 2f, FrameHeight / 2f);
            SpriteEffects effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            spriteBatch.Draw(tex, NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY), sourceRect, drawColor, NPC.rotation, origin, NPC.scale, effects, 0f);

            return false;
        }
    }

    // =========================================================================
    // Proyectil invisible para registrar el golpe de forma segura en Multijugador.
    // =========================================================================
    public class RanaGolpeProjectile_Tier_3 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_1";

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 3;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.ArmorPenetration = 1000;
            Projectile.knockBack = 0f;
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}