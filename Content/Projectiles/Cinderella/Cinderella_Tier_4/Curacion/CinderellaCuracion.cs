using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Clases;
using Jojo.Content.Projectiles.Cinderella.Cinderella_Tier_4;
using BuffCuracion = Jojo.Content.Buffs.GoldenExperience_Buffs.Curacion;

namespace Jojo.Content.Habilidades.Cinderella.Cinderella_Tier_4.CuracionHabilidad
{
    public class CinderellaCuracionConfig
    {
        public int Vida = 80;          // Vida que cura
        public int Mana = 80;          // Maná que recarga
        public int Cooldown = 1800;    // Ticks de cooldown (30s)
        public float Radio = 190f;     // Radio del área en píxeles
    }

    public static class CinderellaCuracionManager
    {
        public static bool Aplicar(Player player, CinderellaCuracionConfig config, Projectile standProjectile = null)
        {
            if (player == null || !player.active || player.dead) return false;
            if (player.whoAmI != Main.myPlayer) return false;
            if (config == null) config = new CinderellaCuracionConfig();

            int buffCooldown = ModContent.BuffType<BuffCuracion>();

            if (player.HasBuff(buffCooldown)) return false;

            // --- El lanzador se cura al instante ---
            AplicarEfectoA(player, config.Vida, config.Mana);

            // --- Cooldown ---
            player.AddBuff(buffCooldown, config.Cooldown);

            SoundEngine.PlaySound(SoundID.Item4, player.Center);

            Vector2 posInicial = (standProjectile != null && standProjectile.active) ? standProjectile.Center : player.Center;

            int projIndex = Projectile.NewProjectile(
                player.GetSource_Misc("CinderellaCuracion"),
                posInicial,
                Vector2.Zero,
                ModContent.ProjectileType<CinderellaCuracionAreaProj>(),
                0,
                config.Radio,
                player.whoAmI,
                config.Vida,
                config.Mana);

            // Guardamos la referencia directa del Stand del jugador en localAI[1]
            if (projIndex >= 0 && projIndex < Main.maxProjectiles)
            {
                Main.projectile[projIndex].localAI[1] = (standProjectile != null && standProjectile.active) ? standProjectile.whoAmI : -1;
            }

            return true;
        }

        public static void AplicarEfectoA(Player target, int vida, int mana)
        {
            if (vida > 0)
            {
                target.Heal(vida);
                if (Main.netMode == NetmodeID.MultiplayerClient && target.whoAmI == Main.myPlayer)
                {
                    NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, target.whoAmI);
                }
            }

            if (mana > 0)
            {
                target.statMana += mana;
                if (target.statMana > target.statManaMax2)
                    target.statMana = target.statManaMax2;

                target.ManaEffect(mana);

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    NetMessage.SendData(MessageID.PlayerMana, -1, -1, null, target.whoAmI);
                }
            }
        }

        public static bool EsAliado(Player lanzador, Player otro)
        {
            if (lanzador.whoAmI == otro.whoAmI) return true;
            if (!lanzador.hostile || !otro.hostile) return true;
            return lanzador.team != 0 && lanzador.team == otro.team;
        }
    }

    public class CinderellaCuracionAreaProj : ModProjectile
    {
        private const int DuracionArea = 35;

        public override string Texture => "Terraria/Images/Projectile_14";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = DuracionArea;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.hide = true;
        }

        public override void AI()
        {
            float radio = Projectile.knockBack > 0f ? Projectile.knockBack : 190f;
            int vida = (int)Projectile.ai[0];
            int mana = (int)Projectile.ai[1];

            // ---------- SEGUIMIENTO ESPECÍFICO AL STAND DEL JUGADOR ----------
            Projectile stand = null;
            int targetStandIndex = (int)Projectile.localAI[1];

            // 1. Intentar usar la referencia directa que pasamos al instanciar
            if (targetStandIndex >= 0 && targetStandIndex < Main.maxProjectiles)
            {
                Projectile cand = Main.projectile[targetStandIndex];
                if (cand.active && cand.owner == Projectile.owner && !cand.npcProj)
                {
                    stand = cand;
                }
            }

            // 2. Si no se encuentra, hacer búsqueda estricta ignorando PROYECTILES DE NPC (!proj.npcProj)
            if (stand == null)
            {
                int playerCinderellaType = ModContent.ProjectileType<CINDERELLASTAND_Tier_4>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active && proj.owner == Projectile.owner && !proj.npcProj && proj.type == playerCinderellaType)
                    {
                        stand = proj;
                        break;
                    }
                }
            }

            if (stand != null && stand.active)
            {
                Projectile.Center = stand.Center;
            }
            else if (Main.player[Projectile.owner].active)
            {
                Projectile.Center = Main.player[Projectile.owner].Center;
            }

            // ---------- Primer frame: curar aliados dentro del área ----------
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;

                if (!Main.dedServ && Main.myPlayer != Projectile.owner)
                {
                    Player local = Main.LocalPlayer;
                    Player duenio = Main.player[Projectile.owner];

                    if (local.active && !local.dead && duenio.active
                        && CinderellaCuracionManager.EsAliado(duenio, local)
                        && Vector2.Distance(local.Center, Projectile.Center) <= radio)
                    {
                        CinderellaCuracionManager.AplicarEfectoA(local, vida, mana);
                    }
                }

                if (!Main.dedServ)
                {
                    for (int i = 0; i < 18; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                        int d = Dust.NewDust(Projectile.Center, 0, 0, DustID.GreenFairy, vel.X, vel.Y, 100, default, 1.3f);
                        Main.dust[d].noGravity = true;
                    }
                }
            }

            if (Main.dedServ) return;

            // ---------- Partículas del borde ----------
            for (int i = 0; i < 2; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + ang.ToRotationVector2() * radio;
                int d = Dust.NewDust(pos, 0, 0, DustID.GreenFairy, 0f, 0f, 80, default, 1.1f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = Vector2.Zero;
            }

            // ---------- Partículas de relleno ----------
            if (Main.rand.NextBool(2))
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                float dist = radio * (float)System.Math.Sqrt(Main.rand.NextFloat());
                Vector2 pos = Projectile.Center + ang.ToRotationVector2() * dist;
                int d = Dust.NewDust(pos, 0, 0, DustID.GreenFairy, 0f, -1f, 120, default, Main.rand.NextFloat(0.8f, 1.1f));
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = new Vector2(0f, Main.rand.NextFloat(-1.2f, -0.4f));
            }
        }
    }
}