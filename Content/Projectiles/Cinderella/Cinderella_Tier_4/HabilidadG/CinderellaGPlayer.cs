using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.UI;
using Jojo.Content.Systems;
using Jojo.Content.Buffs.Cinderalla_Buffs;
using Jojo.Content.Habilidades.Cinderella.Cinderella_Tier_4.ArmarioHabilida;
using Jojo.Content.Projectiles.Cinderella.Cinderella_Tier_4;

namespace Jojo.Content.Habilidades.Cinderella.Cinderella_Tier_4.HabilidadG
{
    // ====================================================================================
    // CONFIGURACIÓN DE LA HABILIDAD G
    // Cada tier crea su propio objeto con SUS valores y se lo pasa al cerebro.
    // Los valores por defecto son los del Tier 4.
    // ====================================================================================
    public class CinderellaGConfig
    {
        public int Duracion = 300;                 // Ticks que dura el cambio de apariencia (60 = 1s)
        public float RadioConfusion = 480f;        // Radio donde los enemigos quedan confundidos
        public int DuracionConfusion = 300;        // Ticks de confusión en NPCs normales
        public int DuracionDesorientadoJefe = 300; // Ticks de "Desorientado" en jefes
        public float RadioDesfigurado = 260f;      // Radio del área de partículas (donde se aplica Desfigurado)

        // Debuff "Desfigurado" aplicado a TODO enemigo dentro del área
        public int DesfiguradoDuracion = 300;                // 5 segundos
        public float DesfiguradoMultiplicadorDefensa = 0.75f; // Igual que el golpe del stand
        public float DesfiguradoReduccionVelocidad = 0.25f;   // Igual que el golpe del stand
    }

    public class CinderellaGPlayer : ModPlayer
    {
        // Prefijo común a todos los items de Cinderella (Tier_1, Tier_2, Tier_3, Tier_4...)
        private const string PrefijoItemStand = "CinderellaItem_Tier_";

        // Valores por defecto (los usa Tier 4 y cualquier llamada sin config)
        public const float RadioConfusion = 480f;
        public const int DuracionDesfiguradoG = 300;
        public const float RadioDesfigurado = 260f;

        // Duración larga del buff marcador (1 hora). Se renueva solo si baja de 1 minuto.
        private const int DuracionBuffArmadura = 60 * 60 * 60;
        private const int UmbralRenovarBuff = 60 * 60;

        public bool gActiva = false;
        public int gTimer = 0;

        // Valor "deseado" por el jugador local (lo cambia el botón del ojo)
        public bool ocultarArmadura = false;

        private int originalHair;
        private Color originalHairColor;
        private Color originalSkinColor;
        private Color originalEyeColor;
        private Color originalShirtColor;
        private Color originalUnderShirtColor;
        private Color originalPantsColor;
        private Color originalShoeColor;

        // Verdadero si hay CUALQUIER tier de Cinderella en la ranura de stand
        public static bool TieneCinderellaEnRanura(Player p)
        {
            if (p == null || !p.active) return false;

            StandSlotPlayer slot = p.GetModPlayer<StandSlotPlayer>();
            Item item = slot.standItem;

            return item != null
                && !item.IsAir
                && item.ModItem != null
                && item.ModItem.Name.StartsWith(PrefijoItemStand);
        }

        // Verdadero para CUALQUIER jugador (local o remoto) que tenga la armadura oculta
        public bool ArmaduraOculta =>
            ocultarArmadura || Player.HasBuff(ModContent.BuffType<CinderellaArmaduraOcultaBuff>());

        // Llamado por el botón del ojo. Aplica/quita el buff y lo envía a todos.
        public void SyncArmadura()
        {
            if (Player.whoAmI != Main.myPlayer) return;
            ActualizarBuffArmadura();
        }

        private void ActualizarBuffArmadura()
        {
            int tipo = ModContent.BuffType<CinderellaArmaduraOcultaBuff>();
            bool tiene = Player.HasBuff(tipo);
            bool cambio = false;

            if (ocultarArmadura)
            {
                if (!tiene)
                {
                    Player.AddBuff(tipo, DuracionBuffArmadura);
                    cambio = true;
                }
                else
                {
                    int idx = Player.FindBuffIndex(tipo);
                    if (idx >= 0 && Player.buffTime[idx] < UmbralRenovarBuff)
                    {
                        Player.buffTime[idx] = DuracionBuffArmadura;
                        cambio = true;
                    }
                }
            }
            else if (tiene)
            {
                Player.ClearBuff(tipo);
                cambio = true;
            }

            if (cambio && Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.PlayerBuffs, -1, -1, null, Player.whoAmI);
            }
        }

        public override void FrameEffects()
        {
            if (ArmaduraOculta)
            {
                Player.head = -1;
                Player.body = -1;
                Player.legs = -1;
            }
        }

        public override void ResetEffects()
        {
            if (Player.whoAmI == Main.myPlayer)
            {
                // Si desequipa a Cinderella, la armadura vuelve a ser visible
                if (ocultarArmadura && !TieneCinderellaEnRanura(Player))
                {
                    ocultarArmadura = false;
                }

                ActualizarBuffArmadura();
            }

            if (gActiva)
            {
                gTimer--;

                if (gTimer <= 0)
                {
                    RestaurarApariencia();
                }
            }
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            RestaurarApariencia();
        }

        private static void EnviarBuffAlServidor(NPC npc, int buffType, int ticks)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.AddNPCBuff, -1, -1, null, npc.whoAmI, buffType, ticks);
            }
        }

        private static void AplicarDesorientado(NPC npc, int buffDesorientado, int duracion)
        {
            NPC objetivo = DesorientadoGlobalNPC.Duenio(npc);
            objetivo.buffImmune[buffDesorientado] = false;
            objetivo.AddBuff(buffDesorientado, duracion);
            EnviarBuffAlServidor(objetivo, buffDesorientado, duracion);
        }

        // Partículas del área (las llama el proyectil sincronizado en todos los clientes)
        public static void GenerarAreaDesfigurado(Vector2 centro, float radio)
        {
            if (Main.dedServ) return;

            int puntos = (int)MathHelper.Clamp(radio / 6f, 24, 140);

            for (int i = 0; i < puntos; i++)
            {
                float angulo = Main.rand.NextFloat(MathHelper.TwoPi);
                float distancia = radio * (float)System.Math.Sqrt(Main.rand.NextFloat());
                Vector2 pos = centro + angulo.ToRotationVector2() * distancia;

                int d = Dust.NewDust(pos, 0, 0, DustID.PinkFairy, 0f, 0f, 60, default, Main.rand.NextFloat(0.8f, 1.3f));
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = Vector2.Zero;
                Main.dust[d].fadeIn = 0f;
            }
        }

        // Sobrecarga sin parámetros: valores por defecto (Tier 4 sigue funcionando igual)
        public void ActivarHabilidadG()
        {
            ActivarHabilidadG(new CinderellaGConfig());
        }

        // CEREBRO de la habilidad G. Cada tier le pasa su configuración.
        public void ActivarHabilidadG(CinderellaGConfig config)
        {
            if (config == null) config = new CinderellaGConfig();

            if (gActiva) RestaurarApariencia();

            originalHair = Player.hair;
            originalHairColor = Player.hairColor;
            originalSkinColor = Player.skinColor;
            originalEyeColor = Player.eyeColor;
            originalShirtColor = Player.shirtColor;
            originalUnderShirtColor = Player.underShirtColor;
            originalPantsColor = Player.pantsColor;
            originalShoeColor = Player.shoeColor;

            Player.hair = Main.rand.Next(0, HairID.Count);
            Player.hairColor = new Color(Main.rand.Next(256), Main.rand.Next(256), Main.rand.Next(256));
            Player.skinColor = new Color(Main.rand.Next(100, 256), Main.rand.Next(80, 240), Main.rand.Next(60, 220));
            Player.eyeColor = new Color(Main.rand.Next(256), Main.rand.Next(256), Main.rand.Next(256));
            Player.shirtColor = new Color(Main.rand.Next(256), Main.rand.Next(256), Main.rand.Next(256));
            Player.underShirtColor = new Color(Main.rand.Next(256), Main.rand.Next(256), Main.rand.Next(256));
            Player.pantsColor = new Color(Main.rand.Next(256), Main.rand.Next(256), Main.rand.Next(256));
            Player.shoeColor = new Color(Main.rand.Next(256), Main.rand.Next(256), Main.rand.Next(256));

            gActiva = true;
            gTimer = config.Duracion;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, Player.whoAmI);
            }

            SoundEngine.PlaySound(SoundID.Item4, Player.Center);

            // Proyectil de partículas: lleva la config en ai[] (se sincroniza solo)
            if (Player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Player.GetSource_Misc("CinderellaG"),
                    Player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<CinderellaGHabilidadProj>(),
                    0, 0f, Player.whoAmI,
                    config.Duracion,
                    config.RadioConfusion,
                    config.RadioDesfigurado);
            }

            int buffDesorientado = ModContent.BuffType<Desorientado>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.life <= 0) continue;

                bool esJefe = DesorientadoGlobalNPC.EsJefe(npc);

                if (!esJefe && npc.dontTakeDamage) continue;

                float distancia = Vector2.Distance(Player.Center, npc.Center);
                if (esJefe)
                    distancia -= System.Math.Max(npc.width, npc.height) * 0.5f;

                // ---- Confusión / Desorientado (radio de confusión) ----
                if (distancia <= config.RadioConfusion)
                {
                    if (esJefe)
                    {
                        AplicarDesorientado(npc, buffDesorientado, config.DuracionDesorientadoJefe);
                    }
                    else
                    {
                        npc.AddBuff(BuffID.Confused, config.DuracionConfusion);
                        EnviarBuffAlServidor(npc, BuffID.Confused, config.DuracionConfusion);
                    }
                }

                // ---- DESFIGURADO: todo enemigo dentro del área de partículas ----
                // Misma llamada e intensidad que cuando el stand golpea (OnHitNPC).
                if (distancia <= config.RadioDesfigurado)
                {
                    DesfiguradoIconGlobalNPC.Aplicar(
                        npc,
                        config.DesfiguradoDuracion,
                        config.DesfiguradoMultiplicadorDefensa,
                        config.DesfiguradoReduccionVelocidad
                    );
                }
            }
        }

        public void RestaurarApariencia()
        {
            if (!gActiva) return;

            Player.hair = originalHair;
            Player.hairColor = originalHairColor;
            Player.skinColor = originalSkinColor;
            Player.eyeColor = originalEyeColor;
            Player.shirtColor = originalShirtColor;
            Player.underShirtColor = originalUnderShirtColor;
            Player.pantsColor = originalPantsColor;
            Player.shoeColor = originalShoeColor;

            gActiva = false;
            gTimer = 0;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, Player.whoAmI);
            }

            SoundEngine.PlaySound(SoundID.Item29, Player.Center);
        }
    }


    // ====================================================================================
    // Proyectil de partículas de la habilidad G (visible para TODOS los jugadores)
    // ai[0] = duración en ticks, ai[1] = radio de confusión, ai[2] = radio del área
    // ====================================================================================
    public class CinderellaGHabilidadProj : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_14";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 300; // Se sobrescribe con ai[0] en el primer frame
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.hide = true;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = player.Center;

            float radioConfusion = Projectile.ai[1] > 0f ? Projectile.ai[1] : CinderellaGPlayer.RadioConfusion;
            float radioArea = Projectile.ai[2] > 0f ? Projectile.ai[2] : CinderellaGPlayer.RadioDesfigurado;

            // 1. Primer frame: ajusta duración y lanza la explosión inicial
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;

                if (Projectile.ai[0] > 0f)
                    Projectile.timeLeft = (int)Projectile.ai[0];

                for (int i = 0; i < 40; i++)
                {
                    Vector2 velocidad = Main.rand.NextVector2Circular(6f, 6f);
                    int d = Dust.NewDust(player.Center, 0, 0, DustID.PinkFairy, velocidad.X, velocidad.Y, 100, default, 1.8f);
                    Main.dust[d].noGravity = true;
                }

                CinderellaGPlayer.GenerarAreaDesfigurado(player.Center, radioArea);

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (!npc.active || npc.friendly || npc.life <= 0) continue;

                    bool esJefe = DesorientadoGlobalNPC.EsJefe(npc);
                    if (!esJefe && npc.dontTakeDamage) continue;

                    float distancia = Vector2.Distance(player.Center, npc.Center);
                    if (esJefe) distancia -= System.Math.Max(npc.width, npc.height) * 0.5f;

                    if (distancia <= radioConfusion || distancia <= radioArea)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            Dust.NewDust(npc.position, npc.width, npc.height, DustID.PinkFairy, 0f, -2f, 100, default, 1.2f);
                        }
                    }
                }
            }

            // 2. Partículas constantes mientras la habilidad dura
            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(player.position, player.width, player.height, DustID.PinkFairy, 0f, -1f, 150, default, 1.2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.5f;
            }
        }

        public override void Kill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            if (player.active)
            {
                for (int i = 0; i < 20; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                    int d = Dust.NewDust(player.Center, 0, 0, DustID.PinkFairy, vel.X, vel.Y, 120, default, 1.3f);
                    Main.dust[d].noGravity = true;
                }
            }
        }
    }
}