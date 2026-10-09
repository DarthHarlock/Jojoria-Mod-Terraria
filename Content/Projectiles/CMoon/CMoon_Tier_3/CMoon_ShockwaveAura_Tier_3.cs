using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.IO;
using Jojo.Content.Clases;
using Jojo.Content.Systems;

namespace Jojo.Content.Projectiles.CMoon.CMoon_Tier_3
{
    // Habilidad G: onda expansiva que sale del centro del stand (tu pecho),
    // crece muy rápido, repele (incluyendo jefes) y daña, y luego frena y desaparece.
    public class CMoon_ShockwaveAura_Tier_3 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        // Radio máximo que alcanza el anillo.
        const float MaxRadius = 340f;

        // Ticks que tarda en llegar al radio máximo (la parte "rápida").
        public const int ExpandTicks = 14;

        // Ticks extra que se queda frenada/desvaneciéndose antes de desaparecer.
        public const int LingerTicks = 6;

        // Duración total (usada por CMOONSTAND para saber cuánto debe durar la skin de aura).
        public const int TotalLifeTicks = ExpandTicks + LingerTicks;

        // Empuje directo aplicado a lo que golpea, ignorando la resistencia a knockback normal
        // (así también empuja con fuerza a los jefes).
        const float PushForce = 19f;

        int elapsed = 0;
        float currentRadius = 0f;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalLifeTicks;
            Projectile.hide = true; // no dibuja sprite, solo partículas, igual que CMoon_GravityAura

            // Un solo golpe por enemigo durante toda la vida de la onda.
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = TotalLifeTicks + 5;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            if (!p.active || p.dead) { Projectile.Kill(); return; }

            elapsed++;

            // Progreso de expansión (0 a 1) con "ease-out": crece muy rápido al inicio y frena hacia el final.
            float t = MathHelper.Clamp(elapsed / (float)ExpandTicks, 0f, 1f);
            float eased = 1f - (1f - t) * (1f - t);
            currentRadius = eased * MaxRadius;

            // Mismo sistema de daño normal y corriente que el resto del stand.
            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, 5);
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(90f); // daño que generas

            // Una vez terminada la expansión, la onda "frena" y deja de dañar mientras se desvanece.
            Projectile.friendly = elapsed <= ExpandTicks;

            // Partículas (mismas que la habilidad F: ChlorophyteWeapon + GreenFairy).
            for (int i = 0; i < 10; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(currentRadius, currentRadius);
                Dust ringDust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.ChlorophyteWeapon, Vector2.Zero, 100, Color.LimeGreen, 1.5f);
                ringDust.noGravity = true;
            }

            for (int i = 0; i < 4; i++)
            {
                float innerR = MathHelper.Max(0f, currentRadius - 25f);
                Vector2 offset = Main.rand.NextVector2CircularEdge(innerR, innerR);
                Dust innerDust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.GreenFairy, Vector2.Zero, 100, Color.LimeGreen, 1.3f);
                innerDust.noGravity = true;
            }
        }

        // El "anillo" se trata como un círculo sólido: cualquier enemigo dentro del radio actual
        // puede ser golpeado (una sola vez, gracias a usesLocalNPCImmunity).
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float dist = Vector2.Distance(Projectile.Center, targetHitbox.Center.ToVector2());
            return dist <= currentRadius;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 dir = target.Center - Projectile.Center;
            if (dir == Vector2.Zero) dir = -Vector2.UnitY;
            dir.Normalize();

            // Enemigos inmóviles (maniquís y similares) NO deben ser empujados: forzarles la velocidad
            // les saca la hitbox de su sitio y los buguea.
            bool esInmovil = target.type == NPCID.TargetDummy || (!target.boss && target.knockBackResist <= 0f);

            if (!esInmovil)
            {
                Vector2 fuerzaCalculada = dir * PushForce;

                // Empuje visual instantáneo en el cliente
                target.velocity = fuerzaCalculada;

                // Sincronización robusta mediante el paquete registrado en Jojo.cs
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    ModPacket packet = Mod.GetPacket();
                    packet.Write(Jojo.PacketType_ShockwavePush);
                    packet.Write((int)target.whoAmI);
                    packet.Write((float)fuerzaCalculada.X);
                    packet.Write((float)fuerzaCalculada.Y);
                    packet.Send();
                }
            }

            // Partículas de impacto
            for (int i = 0; i < 6; i++)
            {
                Dust.NewDust(target.position, target.width, target.height, DustID.ChlorophyteWeapon, dir.X * 3f, dir.Y * 3f, 0, Color.LimeGreen, 1.2f);
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 18; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(currentRadius, currentRadius);
                Dust d = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Smoke, Vector2.Zero, 100, default, 1.1f);
                d.noGravity = true;
            }
        }
    }
}