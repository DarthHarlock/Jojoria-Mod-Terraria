using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using System.IO;
using System.Collections.Generic;
using Jojo.Content.Clases;
using Jojo.Content.Players;

namespace Jojo.Content.Projectiles.MadeInHeaven.MadeInHeaven_Tier_Final
{
    public class MIH_OmnidirectionalDash : ModProjectile
    {
        public override string Texture => "Jojo/Content/Projectiles/MadeInHeaven/MadeInHeaven_Tier_Final/DashStart";

        class DashVisual
        {
            public Vector2 Start;
            public Vector2 End;
            public int TimeLeft;
            public int MaxTime;
        }

        private List<DashVisual> dashes = new List<DashVisual>();
        private bool initialized = false;
        private Vector2 origin;

        private float radius = 450f;

        // --- SYNC del segmento visual del dash (arreglo multijugador) ---
        private Vector2 netDashStart;
        private Vector2 netDashEnd;

        public override void SetDefaults()
        {
            Projectile.width = 900;
            Projectile.height = 900;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;

            Projectile.alpha = 255;
            Projectile.timeLeft = 180;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(netDashStart.X);
            writer.Write(netDashStart.Y);
            writer.Write(netDashEnd.X);
            writer.Write(netDashEnd.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            netDashStart = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            netDashEnd = new Vector2(reader.ReadSingle(), reader.ReadSingle());

            // Los clientes ajenos reciben el segmento real y lo añaden aquí,
            // en vez de calcularlo con su propia posición desactualizada.
            dashes.Add(new DashVisual { Start = netDashStart, End = netDashEnd, TimeLeft = 15, MaxTime = 15 });
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];

            if (!p.active || p.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!initialized)
            {
                origin = new Vector2(Projectile.ai[0], Projectile.ai[1]);
                Projectile.Center = origin;
                initialized = true;
            }

            p.GetModPlayer<MIHDashPlayer>().isOmniDashing = true;

            p.immune = true;
            p.immuneTime = 5;
            p.immuneNoBlink = true;
            p.noKnockback = true;
            p.velocity = Vector2.Zero;

            Color ringColor = new Color(237, 150, 69);
            int dustsPerFrame = (int)(radius / 150f) + 2;

            for (int i = 0; i < dustsPerFrame; i++)
            {
                float randAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 dustPos = origin + randAngle.ToRotationVector2() * radius;

                Dust d = Dust.NewDustPerfect(dustPos, DustID.RainbowMk2, Vector2.Zero, 0, ringColor, 1.2f);
                d.noGravity = true;
                d.velocity = randAngle.ToRotationVector2() * 0.5f;
            }

            if (Projectile.timeLeft % 5 == 0)
            {
                if (Main.myPlayer == Projectile.owner)
                {
                    Vector2 dashStart = p.Center;
                    Vector2 dashEnd;

                    List<NPC> validTargets = new List<NPC>();
                    foreach (NPC n in Main.npc)
                    {
                        if (n.active && !n.friendly && n.life > 0 && Vector2.Distance(n.Center, origin) <= radius)
                        {
                            validTargets.Add(n);
                        }
                    }

                    if (validTargets.Count > 0)
                    {
                        NPC target = validTargets[Main.rand.Next(validTargets.Count)];
                        Vector2 dir = Vector2.Normalize(target.Center - dashStart);
                        dashEnd = target.Center + dir * 150f;
                    }
                    else
                    {
                        float randAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                        dashEnd = origin + randAngle.ToRotationVector2() * Main.rand.NextFloat(200f, radius);
                    }

                    if (Vector2.Distance(dashEnd, origin) > radius)
                    {
                        dashEnd = origin + Vector2.Normalize(dashEnd - origin) * radius;
                    }

                    p.Teleport(dashEnd - p.Size / 2f, 1, 0);
                    p.velocity = Vector2.Zero;
                    p.fallStart = (int)(p.position.Y / 16f);
                    p.direction = dashEnd.X > dashStart.X ? 1 : -1;

                    NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, p.whoAmI);

                    // Guardamos el segmento real y forzamos el envío a todos los clientes
                    netDashStart = dashStart;
                    netDashEnd = dashEnd;
                    Projectile.netUpdate = true;

                    SoundEngine.PlaySound(new SoundStyle("Jojo/Content/Sonidos/MihDash") with { MaxInstances = 10, Volume = 0.5f }, p.Center);
                    dashes.Add(new DashVisual { Start = dashStart, End = dashEnd, TimeLeft = 15, MaxTime = 15 });
                }
                else
                {
                    // El sonido igual se escucha localmente en cada cliente;
                    // el segmento visual llega vía ReceiveExtraAI, no se calcula aquí.
                    SoundEngine.PlaySound(new SoundStyle("Jojo/Content/Sonidos/MihDash") with { MaxInstances = 10, Volume = 0.5f }, p.Center);
                }
            }

            for (int i = dashes.Count - 1; i >= 0; i--)
            {
                dashes[i].TimeLeft--;
                if (dashes[i].TimeLeft <= 0) dashes.RemoveAt(i);
            }
        }

        public override void OnKill(int timeLeft)
        {
            Player p = Main.player[Projectile.owner];
            if (p.active && !p.dead && initialized)
            {
                if (Main.myPlayer == Projectile.owner)
                {
                    p.Teleport(origin - p.Size / 2f, 1, 0);
                    p.velocity = Vector2.Zero;
                    p.fallStart = (int)(p.position.Y / 16f);
                    NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, p.whoAmI);
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float sqrDist = Vector2.DistanceSquared(origin, targetHitbox.Center.ToVector2());
            if (sqrDist < radius * radius) return true;
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texStart = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/MadeInHeaven/MadeInHeaven_Tier_Final/DashStart").Value;
            Texture2D texMiddle = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/MadeInHeaven/MadeInHeaven_Tier_Final/DashMiddle").Value;
            Texture2D texEnd = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/MadeInHeaven/MadeInHeaven_Tier_Final/DashEnd").Value;

            foreach (var dash in dashes)
            {
                Vector2 dir = dash.End - dash.Start;
                float dist = dir.Length();
                float rot = dir.ToRotation();
                float alpha = (float)dash.TimeLeft / dash.MaxTime;
                Color color = Color.White * alpha;

                if (dist > 0)
                {
                    Main.EntitySpriteDraw(texStart, dash.Start - Main.screenPosition, null, color, rot, texStart.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

                    float currentDist = texStart.Width / 2f;
                    float segmentLength = texMiddle.Width;

                    if (segmentLength > 0 && currentDist < dist)
                    {
                        while (currentDist + segmentLength / 2f < dist)
                        {
                            Vector2 drawPos = dash.Start + Vector2.Normalize(dir) * currentDist;
                            Main.EntitySpriteDraw(texMiddle, drawPos - Main.screenPosition, null, color, rot, new Vector2(0, texMiddle.Height / 2), Projectile.scale, SpriteEffects.None, 0);
                            currentDist += segmentLength;
                        }
                    }

                    Main.EntitySpriteDraw(texEnd, dash.End - Main.screenPosition, null, color, rot, texEnd.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
                }
            }
            return false;
        }
    }
}