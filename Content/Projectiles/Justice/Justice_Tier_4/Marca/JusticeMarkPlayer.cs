using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Jojo.Content.Projectiles.Justice.Justice_Tier_4.Marca
{
    public class JusticeMarkPlayer : ModPlayer
    {
        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer) return;

            // Funciona con el stand de cualquier tier de Justice
            if (JUSTICESTAND_Tier_4.ObtenerStand(Player.whoAmI) == null) return;

            if (Main.mouseRight && Main.mouseRightRelease)
            {
                Vector2 mousePos = Main.MouseWorld;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && !npc.friendly && !npc.dontTakeDamage && npc.lifeMax > 5)
                    {
                        JusticeGlobalNPC jNpc = npc.GetGlobalNPC<JusticeGlobalNPC>();

                        if (!jNpc.bajoControlMental && npc.Hitbox.Contains(mousePos.ToPoint()))
                        {
                            Projectile.NewProjectile(
                                Player.GetSource_FromThis(),
                                npc.Center,
                                Vector2.Zero,
                                ModContent.ProjectileType<JusticeMarcaSenal>(),
                                0,
                                0f,
                                Player.whoAmI,
                                i
                            );
                            break;
                        }
                    }
                }
            }
        }

        // Cada jugador tiene SU marca.
        // La duración la define su stand con DuracionMarca (en ticks, 60 = 1 segundo).
        // Volver a marcar al mismo enemigo reinicia el tiempo.
        public static void AplicarMarca(int npcIndex, int jugador)
        {
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs) return;

            NPC objetivo = Main.npc[npcIndex];
            if (!objetivo.active) return;

            JusticeGlobalNPC jObjetivo = objetivo.GetGlobalNPC<JusticeGlobalNPC>();
            if (jObjetivo.bajoControlMental) return;

            for (int j = 0; j < Main.maxNPCs; j++)
            {
                NPC otro = Main.npc[j];
                if (!otro.active) continue;

                JusticeGlobalNPC g = otro.GetGlobalNPC<JusticeGlobalNPC>();
                if (g.markOwner == jugador)
                {
                    g.justiceMarkTimer = 0;
                    g.markOwner = -1;
                }
            }

            jObjetivo.justiceMarkTimer = JUSTICESTAND_Tier_4.Config(jugador).DuracionMarca;
            jObjetivo.markOwner = jugador;
            jObjetivo.markFrame = 0;
            jObjetivo.markFrameCounter = 0;
        }
    }

    // Proyectil invisible que transporta la marca por la red.
    // ai[0] = índice del NPC marcado. El dueño del proyectil = el jugador que marca.
    public class JusticeMarcaSenal : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        private bool aplicado = false;

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 5;
            Projectile.hide = true;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            if (aplicado) return;
            aplicado = true;

            JusticeMarkPlayer.AplicarMarca((int)Projectile.ai[0], Projectile.owner);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}