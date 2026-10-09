using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using System;
using System.IO;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.SilverChariot_Buffs;
using Jojo.Content.Habilidades;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Systems;
using Jojo.Content.Items;
using Jojo.Content.UI;

namespace Jojo.Content.Projectiles.SilverChariot.SilverChariot_Tier_3
{
    public class ChariotClon_Tier_3 : ModProjectile
    {
        // =========================================================================
        // ¡CAMBIA ESTE NÚMERO PARA MODIFICAR EL DAÑO BASE DE LOS CLONES!
        // =========================================================================
        public float DañoBaseClones = 40f;
        // =========================================================================

        enum EstadoClon { Desplegando, Orbitando, Atacando, Regresando }
        EstadoClon estadoActual = EstadoClon.Desplegando;

        private Vector2 origenStand;
        private float progresoDespliegue = 0f;
        private float radioActual = 0f;
        private float radioMaximo = 95f;

        private Vector2 posicionObjetivoAtaque;
        private Vector2 posicionInicioRetorno;
        private float progresoAtaque = 0f;
        private int timerRetraso = 0;

        const string PathNormal = "Jojo/Content/Projectiles/SilverChariot/SilverChariot_Tier_3/";
        const string PathGolden = "Jojo/Content/Projectiles/Skins/SilverChariot/Golden/";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 88;
            Projectile.height = 98;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.alpha = 40;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)estadoActual);
            writer.Write(posicionObjetivoAtaque.X);
            writer.Write(posicionObjetivoAtaque.Y);
            writer.Write(posicionInicioRetorno.X);
            writer.Write(posicionInicioRetorno.Y);
            writer.Write(progresoAtaque);
            writer.Write(timerRetraso);
            writer.Write(progresoDespliegue);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            estadoActual = (EstadoClon)reader.ReadByte();
            posicionObjetivoAtaque = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            posicionInicioRetorno = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            progresoAtaque = reader.ReadSingle();
            timerRetraso = reader.ReadInt32();
            progresoDespliegue = reader.ReadSingle();
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            // --- ARREGLO MULTIJUGADOR: PARTICULAS Y SONIDO ---
            // Al usar localAI[0] == 0 aseguramos que esto ocurra exactamente 1 vez por cada cliente que vea el proyectil, solucionando el lag de red.
            if (Projectile.localAI[0] == 0f)
            {
                origenStand = new Vector2(Projectile.ai[1], Projectile.ai[2]);
                Projectile.Center = origenStand;

                if (Projectile.ai[0] == 0f && Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("Jojo/Content/Sonidos/SilverChariotArmorOFF"), Projectile.Center);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34, Projectile.Center);

                    Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                    var source = Projectile.GetSource_FromThis();

                    Gore.NewGore(source, Projectile.Center, velocity, ModContent.Find<ModGore>("Jojo/SilverChariot1").Type);
                    Gore.NewGore(source, Projectile.Center, velocity, ModContent.Find<ModGore>("Jojo/SilverChariot2").Type);
                    Gore.NewGore(source, Projectile.Center, velocity, ModContent.Find<ModGore>("Jojo/SilverChariot3").Type);
                    Gore.NewGore(source, Projectile.Center, velocity, ModContent.Find<ModGore>("Jojo/SilverChariot4").Type);
                    Gore.NewGore(source, Projectile.Center, velocity, ModContent.Find<ModGore>("Jojo/SilverChariot5").Type);

                    for (int i = 0; i < 40; i++)
                    {
                        Dust humo = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, Main.rand.NextVector2Circular(7f, 7f), 100, Color.Gray, Main.rand.NextFloat(1.5f, 3.0f));
                        humo.noGravity = true;
                    }
                    for (int i = 0; i < 15; i++)
                    {
                        Gore.NewGore(source, Projectile.Center, Main.rand.NextVector2Circular(4f, 4f), Main.rand.Next(61, 64));
                    }
                    for (int i = 0; i < 35; i++)
                    {
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.SilverCoin, Main.rand.NextVector2Circular(6f, 6f), 100, Color.White, Main.rand.NextFloat(1.2f, 2.0f));
                        d.noGravity = true;
                    }
                }
            }

            Projectile.localAI[0]++;

            bool isOwner = Projectile.owner == Main.myPlayer;

            if (isOwner)
            {
                if (Projectile.localAI[0] > 15 && !player.HasBuff(ModContent.BuffType<SilverClones>()))
                {
                    for (int i = 0; i < 5; i++)
                        Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Silver, 0f, 0f, 150, default, 1f);

                    Projectile.Kill();
                    return;
                }
            }

            float velocidadStand = 0f;
            if (player.TryGetModPlayer<StandStatsPlayer>(out var pStats))
            {
                velocidadStand = pStats.standSpeed;
            }

            if (velocidadStand < 50f) Projectile.localNPCHitCooldown = 6;
            else if (velocidadStand < 100f) Projectile.localNPCHitCooldown = 5;
            else if (velocidadStand < 150f) Projectile.localNPCHitCooldown = 4;
            else Projectile.localNPCHitCooldown = 3;

            int indiceClon = (int)Projectile.ai[0];

            if (radioActual < radioMaximo && (estadoActual == EstadoClon.Orbitando || estadoActual == EstadoClon.Desplegando))
            {
                radioActual += 7f;
                if (radioActual > radioMaximo) radioActual = radioMaximo;
            }

            Projectile.localAI[1] += (0.025f + (velocidadStand * 0.00015f));
            float anguloBase = Projectile.localAI[1];
            float anguloClon = anguloBase + (indiceClon * (MathHelper.TwoPi / 7f));
            Vector2 posicionOrbitaIdeal = player.Center + new Vector2((float)Math.Cos(anguloClon), (float)Math.Sin(anguloClon)) * radioMaximo;

            if (origenStand == Vector2.Zero)
            {
                origenStand = new Vector2(Projectile.ai[1], Projectile.ai[2]);
            }

            if (isOwner)
            {
                bool standEnModoAuto = false;
                NPC objetivoAuto = null;

                int standProjType = ModContent.ProjectileType<SLIVERCHARIOTSTAND_Tier_3>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active && proj.owner == Projectile.owner && proj.type == standProjType)
                    {
                        if (proj.ai[0] == 1f)
                        {
                            standEnModoAuto = true;
                            objetivoAuto = EncontrarEnemigoHabilidad(player);
                        }
                        break;
                    }
                }

                bool quiereAtacar = false;
                Vector2 vectorParaAtacar = Vector2.Zero;

                if (standEnModoAuto)
                {
                    if (objetivoAuto != null)
                    {
                        quiereAtacar = true;
                        vectorParaAtacar = objetivoAuto.Center - player.Center;
                    }
                }
                else
                {
                    if (Main.mouseLeft)
                    {
                        quiereAtacar = true;
                        vectorParaAtacar = Main.MouseWorld - player.Center;
                    }
                }

                if (quiereAtacar)
                {
                    if (estadoActual == EstadoClon.Orbitando)
                    {
                        timerRetraso++;
                        int framesPorClon = (velocidadStand >= 150f) ? 4 : (velocidadStand >= 50f) ? 5 : 6;
                        int delayRequerido = indiceClon * framesPorClon;

                        if (timerRetraso >= delayRequerido)
                        {
                            estadoActual = EstadoClon.Atacando;
                            progresoAtaque = 0f;
                            timerRetraso = 0;

                            Vector2 vectorObjetivo = vectorParaAtacar;
                            float rangoMaximoStand = 280f;

                            if (vectorObjetivo.Length() > rangoMaximoStand)
                                vectorObjetivo = Vector2.Normalize(vectorObjetivo) * rangoMaximoStand;

                            posicionObjetivoAtaque = player.Center + vectorObjetivo;
                            posicionInicioRetorno = Projectile.Center;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1, Projectile.Center);

                            Projectile.netUpdate = true;
                        }
                    }
                }
                else
                {
                    if (estadoActual == EstadoClon.Orbitando) timerRetraso = 0;
                }
            }

            int direccionMiradaVisual = player.direction;
            if (estadoActual == EstadoClon.Atacando) direccionMiradaVisual = (posicionObjetivoAtaque.X < player.Center.X) ? -1 : 1;

            float factorVelocidadDesplazamiento = Math.Min(0.22f, 0.15f + (velocidadStand * 0.0004f));
            float factorVelocidadRetorno = Math.Min(0.16f, 0.10f + (velocidadStand * 0.0003f));

            switch (estadoActual)
            {
                case EstadoClon.Desplegando:
                    progresoDespliegue += 0.05f;
                    Projectile.Center = Vector2.Lerp(origenStand, posicionOrbitaIdeal, progresoDespliegue);
                    Projectile.spriteDirection = player.direction;
                    Projectile.rotation = 0f;
                    if (progresoDespliegue >= 1f)
                    {
                        estadoActual = EstadoClon.Orbitando;
                        if (isOwner) Projectile.netUpdate = true;
                    }
                    break;

                case EstadoClon.Orbitando:
                    Projectile.Center = posicionOrbitaIdeal;
                    Projectile.spriteDirection = player.direction;
                    Projectile.rotation = 0f;
                    break;

                case EstadoClon.Atacando:
                    progresoAtaque += factorVelocidadDesplazamiento;
                    if (progresoAtaque >= 1f)
                    {
                        progresoAtaque = 1f;
                        estadoActual = EstadoClon.Regresando;
                        posicionInicioRetorno = Projectile.Center;
                        if (isOwner) Projectile.netUpdate = true;
                    }
                    Projectile.Center = Vector2.Lerp(posicionInicioRetorno, posicionObjetivoAtaque, progresoAtaque);
                    Projectile.spriteDirection = (posicionObjetivoAtaque.X < player.Center.X) ? -1 : 1;

                    Vector2 dirAtaque = posicionObjetivoAtaque - Projectile.Center;
                    if (dirAtaque != Vector2.Zero)
                    {
                        dirAtaque.Normalize();
                        float rot = dirAtaque.ToRotation();
                        if (Projectile.spriteDirection == -1) rot += MathHelper.Pi;
                        Projectile.rotation = rot;
                    }

                    if (isOwner && progresoAtaque < 1f)
                    {
                        Vector2 dirEst = posicionObjetivoAtaque - Projectile.Center;
                        if (dirEst == Vector2.Zero) dirEst = new Vector2(player.direction, 0f);
                        dirEst.Normalize();
                        int cantEst = (velocidadStand >= 150f) ? 4 : (velocidadStand >= 100f) ? 3 : (velocidadStand >= 50f) ? 2 : 1;
                        for (int i = 0; i < cantEst; i++)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, dirEst * 42f, ModContent.ProjectileType<Estocada_Tier_3>(), Projectile.damage, 0f, player.whoAmI, velocidadStand, Projectile.identity);
                        }
                    }
                    break;

                case EstadoClon.Regresando:
                    progresoAtaque -= factorVelocidadRetorno;
                    if (progresoAtaque <= 0f)
                    {
                        progresoAtaque = 0f;
                        estadoActual = EstadoClon.Orbitando;
                        if (isOwner) Projectile.netUpdate = true;
                    }
                    Projectile.Center = Vector2.Lerp(posicionOrbitaIdeal, posicionInicioRetorno, progresoAtaque);
                    Projectile.spriteDirection = player.direction;
                    Projectile.rotation = 0f;
                    break;
            }

            // =========================================================================
            // AQUÍ ESTÁ EL CAMBIO DONDE SE USA LA VARIABLE QUE CREAMOS AL PRINCIPIO
            Projectile.damage = (int)player.GetTotalDamage<Clases.ClaseStand>().ApplyTo(DañoBaseClones);
            // =========================================================================

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 9) { Projectile.frameCounter = 0; Projectile.frame = (Projectile.frame + 1) % 4; }
        }

        private NPC EncontrarEnemigoHabilidad(Player p)
        {
            NPC best = null;
            float max = 280f;

            foreach (NPC n in Main.npc)
            {
                if (n.active && !n.friendly && n.life > 0)
                {
                    float dist = Vector2.Distance(p.Center, n.Center);
                    if (dist < max) { best = n; max = dist; }
                }
            }
            return best;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];

            bool goldenSkin = StandSlotSystem.HasGoldenSkinFor(owner);
            string pathBase = goldenSkin ? PathGolden : PathNormal;

            Texture2D tex = ModContent.Request<Texture2D>(goldenSkin ? pathBase + "ChariotClon_Golden" : PathNormal + "ChariotClon_Tier_3").Value;
            Rectangle r = new Rectangle(Projectile.frame * 88, 0, 88, 98);
            Vector2 o = new Vector2(44, 49);

            Color colorBaseClon = lightColor * ((255 - Projectile.alpha) / 255f);
            SpriteEffects efectos = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            for (int k = 0; k < Projectile.oldPos.Length; k++)
            {
                Vector2 drawPos = Projectile.oldPos[k] + new Vector2(Projectile.width / 2f, Projectile.height / 2f) - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
                Color colorSombra = colorBaseClon * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);
                colorSombra *= 0.45f;
                Main.EntitySpriteDraw(tex, drawPos, r, colorSombra, Projectile.rotation, o, Projectile.scale, efectos, 0f);
            }

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, r, colorBaseClon, Projectile.rotation, o, Projectile.scale, efectos, 0f);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (estadoActual != EstadoClon.Atacando) return false;
            if (projHitbox.Intersects(targetHitbox)) return true;

            Vector2 direccionAtaque = posicionObjetivoAtaque - Projectile.Center;
            if (direccionAtaque == Vector2.Zero) direccionAtaque = new Vector2(Projectile.spriteDirection, 0f);
            direccionAtaque.Normalize();

            Vector2 origenEstocada = Projectile.Center + (direccionAtaque * -59f);
            float longitudDeGolpe = 200f;
            int grosorDeLinea = 90;

            Vector2 destinoEstocada = origenEstocada + (direccionAtaque * longitudDeGolpe);
            float point = 0f;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), origenEstocada, destinoEstocada, grosorDeLinea, ref point))
            {
                return true;
            }

            return false;
        }
    }
}