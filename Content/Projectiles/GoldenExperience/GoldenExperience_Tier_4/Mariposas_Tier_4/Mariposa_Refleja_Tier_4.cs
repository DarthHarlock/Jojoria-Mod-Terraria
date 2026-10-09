using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Clases;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_4;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_4.Mariposas_Tier_4
{
    public class Mariposa_Refleja_Tier_4 : ModNPC
    {
        public override string Texture => "Terraria/Images/NPC_" + NPCID.Butterfly;

        const float DañoBase = 90f;
        const float PorcentajeReflejo = 0.4f;
        const int TiempoVidaMaximo = 40 * 60; // 40 segundos: la mariposa se autodestruye sola pasado este tiempo
        const int GraciaSpawnTicks = 20; // margen para que el Stand se registre en red antes de validar el vínculo

        int tiempoVida;

        // --- OwnerIndex ya NO vive en NPC.ai[] (para no chocar con la IA vanilla de Butterfly) ---
        public int OwnerIndex { get; set; } = -1;

        // =========================================================================
        // FIX MULTIJUGADOR (partículas de muerte invisibles fuera de singleplayer):
        // SacrificarMariposa() solo corre en Servidor/Singleplayer. El Dust.NewDust
        // que crea ahí NUNCA viaja a los clientes por sí solo (el dust no se
        // networkea automáticamente en Terraria), así que en singleplayer lo veías
        // porque tu propia máquina ejecutaba ese código, pero en multijugador el
        // servidor lo generaba solo en su copia local y ningún cliente se enteraba.
        //
        // La solución es la misma técnica que ya usa GOLDENSTAND_Tier_4 para
        // curacionRedActiva/mariposaRedActiva: un flag sincronizado por
        // SendExtraAI/ReceiveExtraAI que, al detectar la transición false -> true,
        // dispara el efecto DENTRO de ReceiveExtraAI. Ese método sí se ejecuta en
        // la máquina de cada cliente al procesar el paquete, así que el efecto se
        // reproduce ahí donde antes no llegaba nada.
        //
        // Importante: el aviso "muriendo" se manda en un paquete SEPARADO, con la
        // mariposa TODAVÍA ACTIVA (ver SacrificarMariposa). Si se mandara en el
        // mismo paquete en el que ya se pone NPC.active = false, el sync de una
        // NPC inactiva no lleva sus datos completos y ReceiveExtraAI nunca
        // recibiría el flag en los clientes.
        // =========================================================================
        bool muriendo;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.Butterfly];
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.Butterfly);
            AIType = NPCID.Butterfly;
            AnimationType = NPCID.Butterfly;

            NPC.lifeMax = 5;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.friendly = true;
            NPC.knockBackResist = 1f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;

            NPC.dontTakeDamage = true;

            // Reseteamos el flag por si el slot de NPC se reutiliza para un spawn nuevo.
            muriendo = false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(OwnerIndex);
            writer.Write(muriendo);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            OwnerIndex = reader.ReadInt32();

            bool muriendoPrevio = muriendo;
            muriendo = reader.ReadBoolean();

            // Transición false -> true: esta es la señal de "la mariposa acaba de
            // morir" llegando a un cliente. Reproducimos aquí el efecto porque este
            // código corre en CADA cliente al procesar el paquete de sync.
            if (muriendo && !muriendoPrevio)
            {
                ReproducirEfectosMuerte();
            }
        }

        public override bool? CanBeHitByItem(Player player, Item item) => false;

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if (!projectile.hostile) return false;
            return null;
        }

        public override void PostAI()
        {
            tiempoVida++;

            // --- A PARTIR DE AQUÍ: SOLO EL SERVIDOR / SINGLEPLAYER DECIDE SI LA MARIPOSA VIVE O MUERE ---
            // Un cliente NUNCA debe matarla por su cuenta en base a datos locales que pueden ir con retraso
            // (ownedProjectileCounts, sync del Stand, etc.). Eso era lo que causaba el "aparece y desaparece".
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // --- LÍMITE DE VIDA MÁXIMO ---
            if (tiempoVida >= TiempoVidaMaximo)
            {
                SacrificarMariposa();
                return;
            }

            // --- VERIFICACIÓN DE VÍNCULO CON EL STAND (con periodo de gracia tras el spawn) ---
            if (tiempoVida > GraciaSpawnTicks)
            {
                int ownerIndex = OwnerIndex;
                if (ownerIndex >= 0 && ownerIndex < Main.maxPlayers)
                {
                    Player owner = Main.player[ownerIndex];
                    int standType = ModContent.ProjectileType<GOLDENSTAND_Tier_4>();

                    if (!owner.active || owner.dead || owner.ownedProjectileCounts[standType] <= 0)
                    {
                        SacrificarMariposa();
                        return;
                    }
                }
            }

            // --- COMPROBACIÓN DE COLISIONES Y DAÑO (Solo Servidor / Singleplayer) ---
            ChequearColisiones();
        }

        void ChequearColisiones()
        {
            // 1. Enemigo hostil toca a la mariposa
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.damage > 0 && npc.whoAmI != NPC.whoAmI
                    && npc.Hitbox.Intersects(NPC.Hitbox))
                {
                    ReflejarDaño(npc, npc.damage);
                    return;
                }
            }

            // 2. Proyectil hostil impacta a la mariposa
            foreach (Projectile proj in Main.projectile)
            {
                if (proj.active && proj.hostile && proj.Hitbox.Intersects(NPC.Hitbox))
                {
                    NPC dueñoProyectil = BuscarNPCCercano(proj.Center);
                    int dañoProyectil = proj.damage;

                    // --- FIX MP: matar el proyectil server-side SIN avisar a los clientes lo
                    // dejaba "vivo" visualmente en las demás pantallas hasta que expirase solo.
                    // Hay que mandar KillProjectile explícitamente, igual que SacrificarMariposa
                    // ya hace con SyncNPC para sí misma. ---
                    int identity = proj.identity;
                    int owner = proj.owner;
                    proj.Kill();
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, identity, owner);

                    if (dueñoProyectil != null) ReflejarDaño(dueñoProyectil, dañoProyectil);
                    else SacrificarMariposa();

                    return;
                }
            }
        }

        NPC BuscarNPCCercano(Vector2 posicion)
        {
            NPC mejor = null;
            float mejorDist = 96f;
            foreach (NPC npc in Main.npc)
            {
                if (!npc.active || npc.friendly) continue;
                float d = Vector2.Distance(npc.Center, posicion);
                if (d < mejorDist) { mejorDist = d; mejor = npc; }
            }
            return mejor;
        }

        void ReflejarDaño(NPC atacante, int dañoRecibido)
        {
            if (atacante == null || !atacante.active) { SacrificarMariposa(); return; }

            float baseDamage = DañoBase;
            int ownerIndex = OwnerIndex;

            if (ownerIndex >= 0 && ownerIndex < Main.maxPlayers)
            {
                Player owner = Main.player[ownerIndex];
                if (owner.active && !owner.dead)
                {
                    baseDamage = owner.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(baseDamage);
                }
            }

            int dañoReflejado = (int)baseDamage + (int)(dañoRecibido * PorcentajeReflejo);

            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood,
                    Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-3f, -1f));
            }

            // =========================================================================
            // FIX MULTIJUGADOR: en vez de llamar a atacante.StrikeNPC(...) directamente
            // (lo cual solo modifica al NPC en la copia del servidor y NUNCA se sincroniza
            // a los clientes -> el enemigo "moría" de forma invisible, sin dust, sin sonido,
            // sin animación, y en la pantalla del cliente simplemente desaparecía sin razón
            // aparente en el siguiente sync genérico), usamos el MISMO enfoque que el clon
            // D4C Melee: un Projectile friendly real, propiedad del jugador dueño del Stand.
            // Ese pipeline (colisión de proyectil -> StrikeNPC -> paquete de red -> server
            // -> broadcast a todos) ya está resuelto por el motor del juego y sincroniza
            // vida, muerte, dust y sonido correctamente en todos los clientes.
            // =========================================================================
            if (Main.netMode != NetmodeID.MultiplayerClient && ownerIndex >= 0 && ownerIndex < Main.maxPlayers)
            {
                int proj = Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    atacante.position,
                    Vector2.Zero,
                    ModContent.ProjectileType<MariposaGolpeProjectile_Tier_4>(),
                    dañoReflejado,
                    0f,
                    ownerIndex
                );

                if (proj >= 0 && proj < Main.maxProjectiles)
                {
                    Projectile p = Main.projectile[proj];
                    // Igualamos el hitbox al del atacante y lo centramos exactamente encima
                    // suyo para garantizar el impacto en el primer tick, sin depender de
                    // velocidad ni de que el enemigo se quede quieto.
                    p.width = atacante.width + 8;
                    p.height = atacante.height + 8;
                    p.Center = atacante.Center;
                    p.netUpdate = true;

                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, proj);
                }
            }

            SacrificarMariposa();
        }

        public override bool CheckDead()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                SacrificarMariposa();
            return false;
        }

        // Punto de entrada público para forzar la muerte de esta mariposa desde
        // fuera (lo usa MariposaTierManager_Tier_4 para "refrescar" el enjambre
        // de un jugador cuando vuelve a usar la habilidad H, en vez de dejar
        // que se acumulen mariposas de casteos anteriores).
        public void Sacrificar() => SacrificarMariposa();

        void ReproducirEfectosMuerte()
        {
            for (int i = 0; i < 12; i++)
            {
                int polvo = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.YellowStarDust,
                    Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f), 100, default, 1.3f);
                Main.dust[polvo].noGravity = true;
            }

            SoundEngine.PlaySound(NPC.DeathSound, NPC.Center);
        }

        void SacrificarMariposa()
        {
            if (!NPC.active) return;

            // --- Efecto local inmediato en Servidor/Singleplayer ---
            // En singleplayer esta es la ÚNICA vez que se reproduce el efecto, porque
            // ahí SendExtraAI/ReceiveExtraAI nunca viajan por red (no hay "otros clientes").
            // En un servidor dedicado esta llamada no tiene efecto visual (el server no
            // dibuja nada), pero es inofensiva.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                ReproducirEfectosMuerte();
            }

            if (Main.netMode == NetmodeID.Server)
            {
                // PASO 1: avisamos "muriendo = true" con la mariposa TODAVÍA ACTIVA.
                // Esto es lo que hace que los clientes reciban el flag completo en
                // ReceiveExtraAI y disparen ahí el dust dorado + sonido. Si esto se
                // mandara junto al active = false de abajo, el paquete de una NPC
                // inactiva no llevaría los datos completos y el aviso jamás llegaría.
                muriendo = true;
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }

            // PASO 2: ahora sí desactivamos y sincronizamos la desaparición real,
            // exactamente igual que antes.
            NPC.active = false;

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }
        }
    }

    // =========================================================================
    // Proyectil de "golpe" para el reflejo de daño de la Mariposa. Existe solo
    // para canalizar el daño a través del sistema de colisión/red normal de
    // Terraria (el mismo mecanismo que usa el golpe del clon D4C Melee), en
    // vez de forzar el daño con StrikeNPC directo desde código, que no se
    // sincroniza a los clientes.
    // =========================================================================
    public class MariposaGolpeProjectile_Tier_4 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_1";

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;       // solo puede golpear a un único enemigo
            Projectile.timeLeft = 3;        // vive lo justo para registrar el impacto
            Projectile.hide = true;         // es un "hitbox invisible", no se dibuja
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();
            Projectile.ArmorPenetration = 1000;
            Projectile.knockBack = 0f;
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}