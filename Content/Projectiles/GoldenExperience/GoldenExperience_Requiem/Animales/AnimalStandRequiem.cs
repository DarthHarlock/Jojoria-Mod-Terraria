using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Clases;
using Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem.Animales
{
    // =========================================================================
    // Base común para los "animales" invocados por GOLDENSTAND_Requiem
    // (Pájaro, Rana, Mariposa). Aplica el MISMO patrón que ya usa
    // Mariposa_Refleja_Tier_4 en Golden Experience Tier 4:
    //
    //   - Son ModNPC reales (no ModProjectile) que clonan al NPC vanilla
    //     correspondiente (AIType/AnimationType). Así conservan su
    //     movimiento errático/natural EXACTO (vuelo, salto, aleteo) en vez
    //     de comportarse como un misil que va en línea recta al objetivo.
    //     El único "extra" es un empujoncito suave hacia la presa más
    //     cercana en PostAI, que se SUMA a ese movimiento natural sin
    //     reemplazarlo.
    //   - Tienen vida real (NPC.lifeMax), no solo un contador de "penetrate"
    //     como un proyectil. Se mantienen inmunes a daño normal
    //     (dontTakeDamage) para que no las masacre un AoE random, igual que
    //     la mariposa de Tier 4.
    //   - Vínculo con el dueño sincronizado por SendExtraAI/ReceiveExtraAI
    //     (NO por NPC.ai[], para no chocar con la IA vanilla clonada).
    //   - El daño al enemigo NO se aplica directo desde código (eso nunca
    //     viaja a los clientes); se canaliza a través de un Projectile real
    //     (AnimalGolpeProjectile_Requiem) que colisiona, igual que hace el
    //     clon D4C Melee y la Mariposa de Tier 4.
    // =========================================================================
    public abstract class AnimalStandRequiem : ModNPC
    {
        // --- Configuración por especie (cada subclase define lo suyo) ---
        protected abstract int NPCVanillaBase { get; }
        protected abstract float DañoBase { get; }
        protected abstract float RadioDeteccion { get; }
        protected abstract int TiempoVidaMaximo { get; }

        protected virtual int GolpesMaximos => 3;          // como el "penetrate = 3" original
        protected virtual int CooldownGolpeTicks => 40;     // ~0.66s entre golpes
        protected virtual int GraciaSpawnTicks => 20;       // margen para que el Stand se registre en red
        protected virtual float ImpulsoHaciaObjetivo => 0.05f;

        int tiempoVida;
        int golpesRealizados;
        int cooldownGolpeTimer;

        // OwnerIndex NO vive en NPC.ai[] para no chocar con la IA vanilla clonada.
        public int OwnerIndex { get; set; } = -1;

        bool muriendo;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCVanillaBase];
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCVanillaBase);
            AIType = NPCVanillaBase;
            AnimationType = NPCVanillaBase;

            NPC.lifeMax = 5;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.friendly = true;
            NPC.knockBackResist = 1f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;

            NPC.dontTakeDamage = true;

            muriendo = false;
            golpesRealizados = 0;
            cooldownGolpeTimer = 0;
            tiempoVida = 0;
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

            // Transición false -> true: la señal de "acaba de morir" llegando
            // a un cliente. Se reproduce aquí porque este código corre en
            // CADA cliente al procesar el paquete de sync (igual que en
            // Mariposa_Refleja_Tier_4).
            if (muriendo && !muriendoPrevio)
            {
                ReproducirEfectosMuerte();
            }
        }

        public override bool? CanBeHitByItem(Player player, Item item) => false;
        public override bool? CanBeHitByProjectile(Projectile projectile) => false;

        public override void PostAI()
        {
            tiempoVida++;
            if (cooldownGolpeTimer > 0) cooldownGolpeTimer--;

            // El movimiento base (vuelo/salto/aleteo) ya lo puso la IA vanilla
            // clonada (AIType), que corre ANTES de este PostAI. Aquí solo
            // sumamos un empujón suave hacia la presa, sin reemplazar esa
            // trayectoria natural. Corre en todos los peers para que la
            // predicción visual sea consistente (igual que la IA vanilla).
            OrientarHaciaObjetivo();

            // --- A PARTIR DE AQUÍ: solo Servidor/Singleplayer decide vida o muerte ---
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (tiempoVida >= TiempoVidaMaximo)
            {
                Desaparecer();
                return;
            }

            if (tiempoVida > GraciaSpawnTicks)
            {
                int ownerIndex = OwnerIndex;
                if (ownerIndex >= 0 && ownerIndex < Main.maxPlayers)
                {
                    Player owner = Main.player[ownerIndex];
                    int standType = ModContent.ProjectileType<GOLDENSTAND_Requiem>();

                    if (!owner.active || owner.dead || owner.ownedProjectileCounts[standType] <= 0)
                    {
                        Desaparecer();
                        return;
                    }
                }
            }

            if (cooldownGolpeTimer <= 0)
            {
                ChequearColisiones();
            }
        }

        protected NPC BuscarEnemigoCercano()
        {
            NPC mejor = null;
            float mejorDist = RadioDeteccion;
            foreach (NPC npc in Main.npc)
            {
                if (!npc.active || npc.friendly || npc.life <= 0) continue;
                float d = Vector2.Distance(npc.Center, NPC.Center);
                if (d < mejorDist) { mejorDist = d; mejor = npc; }
            }
            return mejor;
        }

        // Cada especie puede sobreescribir esto si necesita un tipo de
        // "arrastre" distinto (p.ej. la rana solo quiere empuje horizontal
        // para no romper el arco de su salto).
        protected virtual void OrientarHaciaObjetivo()
        {
            NPC objetivo = BuscarEnemigoCercano();
            if (objetivo == null) return;

            Vector2 dir = objetivo.Center - NPC.Center;
            if (dir == Vector2.Zero) return;
            dir.Normalize();

            NPC.velocity += dir * ImpulsoHaciaObjetivo;
        }

        void ChequearColisiones()
        {
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.life > 0 && npc.Hitbox.Intersects(NPC.Hitbox))
                {
                    GolpearEnemigo(npc);
                    return;
                }
            }
        }

        void GolpearEnemigo(NPC atacado)
        {
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

            int daño = (int)baseDamage;

            // Mismo enfoque que MariposaGolpeProjectile_Tier_4: un Projectile
            // friendly real, propiedad del dueño, para que el pipeline de
            // colisión -> StrikeNPC -> red -> broadcast lo maneje el motor.
            if (Main.netMode != NetmodeID.MultiplayerClient && ownerIndex >= 0 && ownerIndex < Main.maxPlayers)
            {
                int proj = Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    atacado.position,
                    Vector2.Zero,
                    ModContent.ProjectileType<AnimalGolpeProjectile_Requiem>(),
                    daño,
                    0f,
                    ownerIndex
                );

                if (proj >= 0 && proj < Main.maxProjectiles)
                {
                    Projectile p = Main.projectile[proj];
                    p.width = atacado.width + 8;
                    p.height = atacado.height + 8;
                    p.Center = atacado.Center;
                    p.netUpdate = true;

                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, proj);
                }
            }

            golpesRealizados++;
            cooldownGolpeTimer = CooldownGolpeTicks;

            if (golpesRealizados >= GolpesMaximos)
            {
                Desaparecer();
            }
        }

        protected virtual void ReproducirEfectosMuerte()
        {
            for (int i = 0; i < 8; i++)
            {
                int polvo = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.YellowStarDust,
                    Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f), 100, default, 1.1f);
                Main.dust[polvo].noGravity = true;
            }

            SoundEngine.PlaySound(NPC.DeathSound, NPC.Center);
        }

        // Punto de entrada público para forzar la muerte desde fuera (por si
        // en el futuro se quiere "refrescar" el enjambre como hace
        // MariposaTierManager_Tier_4 con su mariposa).
        public void Sacrificar() => Desaparecer();

        void Desaparecer()
        {
            if (!NPC.active) return;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                ReproducirEfectosMuerte();
            }

            if (Main.netMode == NetmodeID.Server)
            {
                muriendo = true;
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }

            NPC.active = false;

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }
        }

        public override bool CheckDead()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Desaparecer();
            return false;
        }
    }
}