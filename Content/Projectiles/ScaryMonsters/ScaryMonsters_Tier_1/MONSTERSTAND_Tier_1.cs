using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using System.IO;
using Jojo.Content.Habilidades;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;

// Reutilizamos sistemas de Tier 4 (marca, visuales)
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4.Transformacion;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4.Marca;

namespace Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_1
{
    public class MONSTERSTAND_Tier_1 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Item_0";

        // =========================================================================
        // CONFIGURACIÓN CENTRALIZADA DEL STAND - TIER 1 (SOLO GARRAS + TRANSFORMACIÓN)
        // =========================================================================
        public static float DanoBase = 7f;

        public static float GarraVelocidad = 5f;
        public static float GarraEscalaNormal = 0.60f;

        // ----------------------- BUFFS PASIVOS DEL JUGADOR (por estar transformado) -----------------------
        public static float BonusDanoStand = 0.05f;       // +5% daño de Stand
        public static float BonusMoveSpeed = 0.10f;       // +10% velocidad de movimiento
        public static float MultMaxRunSpeed = 1.07f;      // x1.07 velocidad máxima de carrera
        public static float MultRunAcceleration = 1.07f;  // x1.07 aceleración al corre r

        // ----------------------- MINI DINOS (NO SE USAN EN TIER 1, el perfil los pide) -----------------------
        public static int MiniDinoDano = 2;
        public static float MiniDinoVelocidad = 4f;
        public static float MiniDinoSalto = 6f;
        public static float MiniDinoRangoDeteccion = 400f;
        public static float MiniDinoDistanciaVuelo = 500f;

        // ----------------------- DINO VIRUS (PASIVA DE TRANSFORMACIÓN) -----------------------
        // IMPORTANTE: DuracionEfecto debe ser MAYOR que TiempoTransformar
        public static bool InfectaConVirus = true;
        public static float DinoVirusDuracionEfecto = 11f;
        public static float DinoVirusTiempoTransformar = 10f;
        public static float DinoVirusDuracionDino = 6f;
        public static int TransformedDinoBonusVida = 60;
        public static int TransformedDinoDano = 5;
        public static float TransformedDinoVelocidad = 4.5f;
        public static float TransformedDinoSalto = 8f;
        // =========================================================================

        bool init, dying, auto;
        int cd;
        int attackTimer;

        public int baseCritChance = 1;
        public int attackSpeed = 1;

        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");
        static readonly SoundStyle SwingSoundBase = new("Terraria/Sounds/Item_1");

        float syncOffX, syncOffY;
        int netTimer = 0;

        public bool isAttacking = false;

        // NPC marcado con click derecho (whoAmI, -1 = ninguno)
        public int marcadoNPC = -1;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(auto);
            writer.Write(syncOffX);
            writer.Write(syncOffY);
            writer.Write(isAttacking);
            writer.Write(marcadoNPC);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            auto = reader.ReadBoolean();
            syncOffX = reader.ReadSingle();
            syncOffY = reader.ReadSingle();
            isAttacking = reader.ReadBoolean();
            marcadoNPC = reader.ReadInt32();
        }

        public override void SetDefaults()
        {
            Projectile.width = 88;
            Projectile.height = 80;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = ModContent.GetInstance<ClaseStand>();

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 9;
            Projectile.ArmorPenetration = 100;
        }

        public void StartDying()
        {
            if (dying) return;
            dying = true;
            Projectile.timeLeft = 25;
            SoundEngine.PlaySound(DespawnSound, Projectile.Center);
        }

        void UpdateAttackSpeed(Player p)
        {
            float s = p.GetModPlayer<StandStatsPlayer>().standSpeed;
            if (s < 50f) attackSpeed = 1;
            else if (s < 100f) attackSpeed = 2;
            else if (s < 150f) attackSpeed = 3;
            else attackSpeed = 4;
        }

        int GetAttackDelay()
        {
            switch (attackSpeed)
            {
                case 1: return 18;
                case 2: return 15;
                case 3: return 12;
                case 4: return 8;
                default: return 18;
            }
        }

        /// <summary>Si hay varios stands Tier 1 del mismo jugador, solo sobrevive el de menor índice.</summary>
        bool EsDuplicado()
        {
            for (int i = 0; i < Projectile.whoAmI; i++)
            {
                Projectile o = Main.projectile[i];
                if (o.active && o.type == Projectile.type && o.owner == Projectile.owner)
                    return true;
            }
            return false;
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            UpdateAttackSpeed(p);

            if (dying) { Projectile.Kill(); return; }
            if (!p.active || p.dead) { StartDying(); return; }

            if (EsDuplicado()) { Projectile.Kill(); return; }

            Projectile.timeLeft = 2;

            // Mantiene vivos la cola y las garras visuales (transformación)
            p.GetModPlayer<ScaryMonstersVisualPlayer>().KeepVisualsAlive();

            if (!init)
            {
                init = true;
                Projectile.alpha = 255;
                SoundEngine.PlaySound(SpawnSound, Projectile.Center);
            }

            bool isOwner = Projectile.owner == Main.myPlayer;
            auto = Projectile.ai[0] == 1f;

            if (isOwner)
            {
                HandleToggle(p);
                HandleMarca(p);

                int direccionMiradaReal = p.direction;

                bool quiereAtacar = Main.mouseLeft && !p.CCed && !p.mouseInterface && !auto;

                if (quiereAtacar)
                {
                    direccionMiradaReal = (Main.MouseWorld.X - p.MountedCenter.X >= 0) ? 1 : -1;
                    p.ChangeDir(direccionMiradaReal);
                }

                Vector2 off = new Vector2(-40 * direccionMiradaReal, -10);
                syncOffX = off.X;
                syncOffY = off.Y;

                Projectile.rotation = 0f;
                Projectile.Center = p.Center + off;

                isAttacking = quiereAtacar;

                if (quiereAtacar)
                {
                    if (attackTimer > 0) attackTimer--;

                    int tipoTajo = ModContent.ProjectileType<GarraSlash_Tier_1>();
                    if (attackTimer <= 0 && p.ownedProjectileCounts[tipoTajo] < 1)
                    {
                        Vector2 direccionAtaque = Main.MouseWorld - p.MountedCenter;
                        if (direccionAtaque != Vector2.Zero)
                            direccionAtaque.Normalize();
                        else
                            direccionAtaque = new Vector2(p.direction, 0);

                        int dañoFinal = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(DanoBase);

                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            p.MountedCenter,
                            direccionAtaque * GarraVelocidad,
                            tipoTajo,
                            dañoFinal,
                            2f,
                            p.whoAmI,
                            0f
                        );

                        float pitch = attackSpeed >= 3 ? 0.05f : attackSpeed >= 2 ? 0.08f : 0f;
                        SoundEngine.PlaySound(SwingSoundBase with { Pitch = pitch }, p.Center);

                        attackTimer = GetAttackDelay();
                    }
                }
                else
                {
                    if (attackTimer > 0) attackTimer--;
                }

                if (++netTimer >= 5)
                {
                    netTimer = 0;
                    Projectile.netUpdate = true;
                }
            }
            else
            {
                Vector2 off = new Vector2(syncOffX, syncOffY);
                Projectile.rotation = 0f;
                Projectile.Center = p.Center + off;
            }

            ScaryMarca.ObjetivoPorJugador[Projectile.owner] = marcadoNPC;
            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(DanoBase);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (InfectaConVirus)
                DinoVirusGlobalNPC.Infectar(target, Projectile.owner);
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
                ScaryMarca.ObjetivoPorJugador[Projectile.owner] = -1;
        }

        void HandleToggle(Player p)
        {
            if (cd-- > 0 || p.whoAmI != Main.myPlayer) return;

            if (JojoKeybinds.ToggleAuto.JustPressed)
            {
                auto = !auto;
                Projectile.ai[0] = auto ? 1f : 0f;
                cd = 30;

                if (Main.netMode != NetmodeID.Server)
                    Main.NewText(auto ? "AutoStand-OFF" : "AutoStand-ON", 255, 255, 0);

                Projectile.netUpdate = true;
            }
        }

        void HandleMarca(Player p)
        {
            if (marcadoNPC >= 0)
            {
                bool invalido = marcadoNPC >= Main.maxNPCs;
                if (!invalido)
                {
                    NPC m = Main.npc[marcadoNPC];
                    invalido = !m.active || m.life <= 0;
                }
                if (invalido)
                {
                    marcadoNPC = -1;
                    Projectile.netUpdate = true;
                }
            }

            if (Main.mouseRight && Main.mouseRightRelease && !p.mouseInterface && !Main.mapFullscreen && !p.CCed)
            {
                NPC objetivo = ScaryMarca.BuscarBajoCursor();
                if (objetivo != null)
                {
                    marcadoNPC = (marcadoNPC == objetivo.whoAmI) ? -1 : objetivo.whoAmI;
                    Projectile.netUpdate = true;
                    SoundEngine.PlaySound(SoundID.MenuTick, p.Center);
                    for (int i = 0; i < 10; i++)
                    {
                        Dust d = Dust.NewDustDirect(objetivo.position, objetivo.width, objetivo.height, DustID.GreenTorch, 0f, 0f, 100, default, 1.2f);
                        d.noGravity = true;
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    // =========================================================================
    // BUFFS PASIVOS DE TIER 1 (daño de Stand + velocidad por estar transformado)
    // Viven en este mismo archivo. Solo actúan si el tier activo es EXACTAMENTE el 1.
    // =========================================================================
    public class Tier1BuffsPlayer : ModPlayer
    {
        bool Activo => ScaryTiers.GetActiveTier(Player) == 1;

        public override void PostUpdateEquips()
        {
            if (!Activo) return;

            Player.GetDamage(ModContent.GetInstance<ClaseStand>()) += MONSTERSTAND_Tier_1.BonusDanoStand;
            Player.moveSpeed += MONSTERSTAND_Tier_1.BonusMoveSpeed;
        }

        public override void PostUpdateRunSpeeds()
        {
            if (!Activo) return;

            Player.maxRunSpeed *= MONSTERSTAND_Tier_1.MultMaxRunSpeed;
            Player.runAcceleration *= MONSTERSTAND_Tier_1.MultRunAcceleration;
        }
    }
}