using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using System.IO;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.MadeInHeaven.MadeInHeaven_Tier_Final
{
    public class MIHDashSkill : ModProjectile
    {
        public override string Texture => "Jojo/Content/Projectiles/MadeInHeaven/MadeInHeaven_Tier_Final/DashStart";

        public override void SetDefaults()
        {
            Projectile.width = 128;
            Projectile.height = 128;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 3;

            Projectile.alpha = 0;
            Projectile.timeLeft = 600;
        }

        private Vector2 startPos;
        private bool initialized = false;

        // --- SYNC del punto de inicio real (arreglo del flicker de la estela) ---
        private Vector2 netStartPos;
        private bool startReady = false;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(netStartPos.X);
            writer.Write(netStartPos.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            netStartPos = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            startReady = true;
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            bool isOwner = p.whoAmI == Main.myPlayer;

            if (!p.active || p.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!initialized)
            {
                initialized = true;
                startPos = p.Center;
                Projectile.Center = p.Center;

                if (isOwner)
                {
                    // El ajuste de colisión con paredes SOLO lo calcula el dueño,
                    // para que todos los clientes usen exactamente el mismo objetivo final.
                    Vector2 rawTarget = new Vector2(Projectile.ai[1], Projectile.ai[2]);
                    Vector2 checkPos = rawTarget;
                    Vector2 checkDir = checkPos - startPos;
                    float checkDist = checkDir.Length();

                    if (checkDist > 0)
                    {
                        checkDir.Normalize();
                        while (checkDist > 0)
                        {
                            Point tilePoint = checkPos.ToTileCoordinates();
                            if (WorldGen.InWorld(tilePoint.X, tilePoint.Y))
                            {
                                Tile tile = Main.tile[tilePoint.X, tilePoint.Y];
                                if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                                {
                                    checkPos -= checkDir * 8f;
                                    checkDist -= 8f;
                                }
                                else break;
                            }
                            else break;
                        }
                    }

                    Projectile.ai[1] = checkPos.X;
                    Projectile.ai[2] = checkPos.Y;

                    // Guardamos y forzamos el envío del punto de inicio real a todos los clientes
                    netStartPos = startPos;
                    startReady = true;
                    Projectile.netUpdate = true;

                    SoundEngine.PlaySound(new SoundStyle("Jojo/Content/Sonidos/MihDash"), p.Center);
                }
            }

            // Mientras los clientes ajenos no hayan recibido el startPos real, no movemos
            // ni dibujamos nada para evitar el frame con la dirección incorrecta.
            if (!startReady)
                return;

            if (Projectile.ai[0] == 0)
            {
                Vector2 targetPos = new Vector2(Projectile.ai[1], Projectile.ai[2]);
                Vector2 dir = targetPos - Projectile.Center;
                float speed = 95f;

                if (isOwner)
                {
                    p.immune = true;
                    p.immuneTime = 60;
                    p.immuneNoBlink = true;
                    p.noKnockback = true;
                    p.velocity = Vector2.Zero;

                    if (dir.X != 0) p.direction = dir.X > 0 ? 1 : -1;
                }

                if (dir.Length() <= speed)
                {
                    Projectile.Center = targetPos;
                    Projectile.velocity = Vector2.Zero;

                    // SINCRO MULTIJUGADOR: Solo el dueño se teletransporta y avisa a los demás.
                    // ARREGLO: usamos MessageID.PlayerControls (igual que la Skill G, que no
                    // tiene este bug) en vez de MessageID.SyncPlayer. SyncPlayer es un paquete
                    // pesado de reestado completo que competía en la red con el propio paquete
                    // de Teleport, provocando que el cliente 2 recibiera brevemente una posición
                    // vieja (el "clon") antes de que el siguiente sync normal lo corrigiera.
                    if (isOwner)
                    {
                        Vector2 teleportPos = targetPos - new Vector2(p.width / 2, p.height / 2);
                        p.Teleport(teleportPos, 1, 0);
                        p.velocity = Vector2.Zero;
                        p.fallStart = (int)(p.position.Y / 16f);

                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, p.whoAmI);
                        }
                    }

                    Projectile.ai[0] = 1;
                    Projectile.friendly = false;

                    if (isOwner) Projectile.netUpdate = true;
                }
                else
                {
                    Projectile.velocity = Vector2.Normalize(dir) * speed;

                    // Pequeño resync periódico para mantener a todos los clientes alineados
                    // durante el trayecto (evita drift acumulado en partidas con más lag).
                    if (isOwner && Projectile.timeLeft % 10 == 0)
                        Projectile.netUpdate = true;
                }
            }
            else if (Projectile.ai[0] == 1)
            {
                Projectile.velocity = Vector2.Zero;
                if (isOwner) p.velocity = Vector2.Zero;
                Projectile.alpha += 25;

                if (Projectile.alpha >= 255) Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!startReady) return false;

            Texture2D texStart = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/MadeInHeaven/MadeInHeaven_Tier_Final/DashStart").Value;
            Texture2D texMiddle = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/MadeInHeaven/MadeInHeaven_Tier_Final/DashMiddle").Value;
            Texture2D texEnd = ModContent.Request<Texture2D>("Jojo/Content/Projectiles/MadeInHeaven/MadeInHeaven_Tier_Final/DashEnd").Value;

            Vector2 realStart = netStartPos;
            Vector2 endPos = Projectile.Center;
            Vector2 dir = endPos - realStart;
            float dist = dir.Length();
            float rot = dir.ToRotation();
            Color color = Color.White * (1f - Projectile.alpha / 255f);

            if (dist > 0)
            {
                Main.EntitySpriteDraw(texStart, realStart - Main.screenPosition, null, color, rot, texStart.Size() / 2, Projectile.scale, SpriteEffects.None, 0);

                float currentDist = texStart.Width / 2f;
                float segmentLength = texMiddle.Width;

                if (segmentLength > 0 && currentDist < dist)
                {
                    while (currentDist + segmentLength / 2f < dist)
                    {
                        Vector2 drawPos = realStart + Vector2.Normalize(dir) * currentDist;
                        Main.EntitySpriteDraw(texMiddle, drawPos - Main.screenPosition, null, color, rot, new Vector2(0, texMiddle.Height / 2), Projectile.scale, SpriteEffects.None, 0);
                        currentDist += segmentLength;
                    }
                }

                if (Projectile.ai[0] == 1)
                {
                    Main.EntitySpriteDraw(texEnd, endPos - Main.screenPosition, null, color, rot, texEnd.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
                }
            }
            return false;
        }
    }
}