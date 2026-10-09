using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using System.IO;
using Jojo.Content.Buffs;
using Jojo.Content.Buffs.ScaryMonsters_Buffs;
using Jojo.Content.Habilidades;
using Jojo.Systems;
using Jojo.Content.Players;
using Jojo.Content.Clases;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4.Transformacion.ModoDinoFull;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4.Transformacion;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4.MinionDinosaurios;
using Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4.Marca;

namespace Jojo.Content.Projectiles.ScaryMonsters.ScaryMonsters_Tier_4
{
    public class MONSTERSTAND_Tier_4 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Item_0";

        // =========================================================================
        //  CONFIGURACIÓN CENTRALIZADA DEL STAND (CONTROL TOTAL DE DAÑOS Y TIEMPOS)
        // =========================================================================
        public static float DanoBase = 50f;          // Daño en forma normal
        public static float DanoDino = 55f;          // Daño transformado en dinosaurio
        public static float DanoPisoton = 200f;      // Daño del pisotón

        public static float GarraVelocidad = 8f;     // Velocidad/alcance del disparo del tajo
        public static float GarraEscalaNormal = 1.00f;// Escala del tajo en forma normal
        public static float GarraEscalaDino = 1.3f;   // Escala del tajo en modo dino

        public static float VelocidadDino = 14f;     // Velocidad de carrera
        public static int AlturaSalto = 14;          // Altura base de salto inicial
        public static float FuerzaSalto = 7.5f;      // Impulso de salto inicial
        public static float FuerzaSaltoExtra = 11f;  // Impulso de saltos dobles
        public static int MaxSaltosExtra = 4;        // Cantidad de saltos extra (2 = Triple Salto)

        // ----------------------- MINI DINOS (INVOCADOS CON LA 'G') -----------------------
        public static int MiniDinoDano = 35;                         // Daño base del dino invocado por la G
        public static int MiniDinosPorG = 4;                         // Invocados por pulsación (forma normal)
        public static int MiniDinosPorGDino = 5;                     // Invocados por pulsación (TRANSFORMADO en dino con la F)
        public static int MiniDinosMax = 50;                         // Máximo simultáneo
        public static float MiniDinoVelocidad = 7f;                  // Velocidad
        public static float MiniDinoSalto = 8.5f;                    // Fuerza de salto
        public static float MiniDinoRangoDeteccion = 400f;    // Rango de detección
        public static float MiniDinoDistanciaVuelo = 500f;    // Distancia para volver volando


        // ----------------------- DINO VIRUS & ENEMIGOS TRANSFORMADOS -----------------------
        public static float DinoVirusDuracionEfecto = 8f;
        public static float DinoVirusTiempoTransformar = 7f;
        public static float DinoVirusDuracionDino = 15f;       // Segundos que dura un mini dino temporal
        public static int TransformedDinoBonusVida = 210;      // Vida extra que recibe el dino infectado (+200)
        public static int TransformedDinoDano = 20;             // Daño base del dinosaurio infectado transformado
        public static float TransformedDinoVelocidad = 8f;      // Velocidad de movimiento
        public static float TransformedDinoSalto = 11f;            // Fuerza de salto

        // ----------------------- MEJORAS DE ESTADÍSTICAS DEL JUGADOR AL EQUIPAR/INVOCAR -----------------------
        public static float PlayerBonusDamage = 0.10f;          // +10% de daño con armas de Stand
        public static float PlayerBonusMoveSpeed = 0.25f;       // +25% de velocidad de movimiento
        public static int PlayerBonusDefense = 10;              // +10 de armadura
        public static float PlayerBonusCrit = 10f;              // +10% de probabilidad de crítico
        // =========================================================================

        bool init, dying, fDash, auto;
        int dashT, cd;
        int attackTimer;

        public int baseCritChance = 1;
        public int attackSpeed = 1;

        static readonly SoundStyle SpawnSound = new("Jojo/Content/Sonidos/Stand_Spawn");
        static readonly SoundStyle DespawnSound = new("Jojo/Content/Sonidos/Stand_Despawn");
        static readonly SoundStyle SwingSoundBase = new("Terraria/Sounds/Item_1");

        float syncOffX, syncOffY;
        int netTimer = 0;

        public bool isAttacking = false;

        // NPC marcado con click derecho (whoAmI, -1 = ninguno). Se sincroniza y se publica en ScaryMarca.
        public int marcadoNPC = -1;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(fDash);
            writer.Write(auto);
            writer.Write(syncOffX);
            writer.Write(syncOffY);
            writer.Write(isAttacking);
            writer.Write(marcadoNPC);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            fDash = reader.ReadBoolean();
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
            Projectile.localNPCHitCooldown = 6;
            Projectile.ArmorPenetration = 1000;
        }

        public void StartDying()
        {
            if (dying) return;
            dying = true;
            Projectile.timeLeft = 25;
            SoundEngine.PlaySound(DespawnSound, Projectile.Center);

            Player owner = Main.player[Projectile.owner];
            if (owner.active && owner.mount.Active && owner.mount.Type == ModContent.MountType<FromaDino>())
            {
                owner.mount.Dismount(owner);
                owner.ClearBuff(ModContent.BuffType<Fosilizacion>());
            }
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
                case 1: return 12;
                case 2: return 9;
                case 3: return 7;
                case 4: return 4;
                default: return 12;
            }
        }

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            UpdateAttackSpeed(p);

            if (dying) { Projectile.Kill(); return; }
            if (!p.active || p.dead) { StartDying(); return; }

            // Aplicar las estadísticas pasivas al jugador mientras el stand esté invocado
            p.GetDamage(ModContent.GetInstance<ClaseStand>()) += PlayerBonusDamage;
            p.moveSpeed += PlayerBonusMoveSpeed;
            p.statDefense += PlayerBonusDefense;
            p.GetCritChance(ModContent.GetInstance<ClaseStand>()) += PlayerBonusCrit;

            Projectile.timeLeft = 2;

            DinoPlayer dinoPl = p.GetModPlayer<DinoPlayer>();
            bool esDino = dinoPl.IsDino;

            if (!esDino)
            {
                // Mantiene vivos los accesorios visuales del Scary Monsters
                p.GetModPlayer<ScaryMonstersVisualPlayer>().KeepVisualsAlive();
            }

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
                HandleSkills(p);
                HandleToggle(p);
                HandleMarca(p);
            }

            UpdateSkills(p);

            float danoActual = esDino ? DanoDino : DanoBase;

            if (isOwner)
            {
                int direccionMiradaReal = p.direction;

                bool puedeUsarGarras = esDino || !auto;
                bool quiereAtacar = Main.mouseLeft && !p.CCed && !p.mouseInterface && puedeUsarGarras;

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

                    int tipoTajo = ModContent.ProjectileType<GarraSlash_Tier_4>();
                    if (attackTimer <= 0 && p.ownedProjectileCounts[tipoTajo] < 1)
                    {
                        Vector2 direccionAtaque = Main.MouseWorld - p.MountedCenter;
                        if (direccionAtaque != Vector2.Zero)
                        {
                            direccionAtaque.Normalize();
                        }
                        else
                        {
                            direccionAtaque = new Vector2(p.direction, 0);
                        }

                        int dañoFinal = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(danoActual);

                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            p.MountedCenter,
                            direccionAtaque * GarraVelocidad,
                            tipoTajo,
                            dañoFinal,
                            esDino ? 7f : 5f,
                            p.whoAmI,
                            esDino ? 1f : 0f
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

            // Publica la marca para que la lean los dinos (en cualquier cliente o servidor)
            ScaryMarca.ObjetivoPorJugador[Projectile.owner] = marcadoNPC;

            Projectile.CritChance = StandCritSystem.GetFinalCritChance(p, baseCritChance);
            Projectile.damage = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(danoActual);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            DinoVirusGlobalNPC.Infectar(target, Projectile.owner);
        }

        public override void OnKill(int timeLeft)
        {
            // Al desaparecer el Stand se limpia la marca
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
                {
                    Main.NewText(auto ? "AutoStand-OFF" : "AutoStand-ON", 255, 255, 0);
                }
                Projectile.netUpdate = true;
            }
        }

        // =========================================================================
        //  MARCA (CLICK DERECHO SOBRE UN NPC)
        // =========================================================================
        void HandleMarca(Player p)
        {
            // Limpiar si el marcado murió / desapareció
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
                    // Click sobre el mismo = desmarcar
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

        void UpdateSkills(Player p)
        {
            if (fDash && ++dashT > 6)
            {
                fDash = false;
                dashT = 0;
                if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
            }
        }

        bool InvocarMiniDinos(Player p)
        {
            int tipo = ModContent.ProjectileType<MiniDino>();

            int actuales = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr.active && pr.type == tipo && pr.owner == p.whoAmI && pr.ai[0] == 0f)
                    actuales++;
            }

            // Transformado en dino (montura F) invoca más dinos por pulsación
            bool esDino = p.GetModPlayer<DinoPlayer>().IsDino;
            int porPulsacion = esDino ? MiniDinosPorGDino : MiniDinosPorG;

            int aInvocar = Math.Min(porPulsacion, MiniDinosMax - actuales);
            if (aInvocar <= 0)
            {
                if (Main.netMode != NetmodeID.Server)
                    Main.NewText("Límite de mini dinos alcanzado", 255, 255, 0);
                return false;
            }

            for (int i = 0; i < 30; i++)
            {
                Dust d = Dust.NewDustDirect(p.position, p.width, p.height, DustID.UltraBrightTorch, 0f, 0f, 100, default, 1.6f);
                d.noGravity = true;
                d.velocity *= 3f;
            }
            SoundEngine.PlaySound(SoundID.Item103, p.Center);

            int dano = (int)p.GetTotalDamage(ModContent.GetInstance<ClaseStand>()).ApplyTo(MiniDinoDano);

            for (int k = 0; k < aInvocar; k++)
            {
                Vector2 pos = p.Center + new Vector2(Main.rand.Next(-20, 21), -10f);
                Vector2 vel = new Vector2((k % 2 == 0 ? 1f : -1f) * Main.rand.NextFloat(2f, 4f), -4f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    pos,
                    vel,
                    tipo,
                    dano,
                    3f,
                    p.whoAmI,
                    0f
                );
            }
            return true;
        }

        void HandleSkills(Player p)
        {
            if (p.whoAmI != Main.myPlayer) return;

            if (JojoKeybinds.SkillF.JustPressed)
            {
                int mountType = ModContent.MountType<FromaDino>();

                if (p.mount.Active && p.mount.Type == mountType)
                {
                    p.mount.Dismount(p);
                    p.ClearBuff(ModContent.BuffType<Fosilizacion>());
                }
                else if (!p.mount.Active)
                {
                    p.mount.SetMount(mountType, p);
                }

                fDash = false;
                Projectile.netUpdate = true;
            }

            if (p.HasBuff(ModContent.BuffType<CD>())) return;

            if (JojoKeybinds.SkillG.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown2>()))
            {
                if (InvocarMiniDinos(p))
                {
                    p.AddBuff(ModContent.BuffType<Cooldown2>(), 3600);
                    p.AddBuff(ModContent.BuffType<CD>(), 0);
                    Projectile.netUpdate = true;
                }
            }

            if (JojoKeybinds.SkillH.JustPressed && !p.HasBuff(ModContent.BuffType<Cooldown3>()))
            {
                p.AddBuff(ModContent.BuffType<Cooldown3>(), 0);
                p.AddBuff(ModContent.BuffType<CD>(), 0);
                Projectile.netUpdate = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}