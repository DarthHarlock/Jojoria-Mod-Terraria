using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.D4C.D4C_Tier_4
{
    // Puente de red para sincronizar la marca de objetivo (click derecho) al servidor.
    // ai[0] = whoAmI del NPC marcado (o -1 para limpiar)
    public class D4CMarcaBridgeProjectile_Tier_4 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_1";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.timeLeft = 2;
            Projectile.alpha = 255;
            Projectile.hide = true;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (Projectile.localAI[0] != 0) return;
            Projectile.localAI[0] = 1;

            Player owner = Main.player[Projectile.owner];
            int npcMarcado = (int)Projectile.ai[0];

            AplicarMarcaServidor(owner, npcMarcado);
        }

        public static void AplicarMarcaServidor(Player owner, int npcIndexMarcado)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC otro = Main.npc[i];
                if (!otro.active) continue;

                D4CGlobalNPC_Tier_4 gOtro = otro.GetGlobalNPC<D4CGlobalNPC_Tier_4>();
                if (gOtro.d4cMarkTimer > 0 && gOtro.OwnerIndex == owner.whoAmI)
                {
                    gOtro.d4cMarkTimer = 0;
                    gOtro.OwnerIndex = -1;
                    otro.netUpdate = true;
                }
            }

            if (npcIndexMarcado < 0 || npcIndexMarcado >= Main.maxNPCs)
            {
                ClonJugadorNPC_Tier_4.ObjetivoMarcadoPorJugador[owner.whoAmI] = -1;
                return;
            }

            NPC npc = Main.npc[npcIndexMarcado];
            if (!npc.active || npc.friendly || npc.life <= 0 || npc.dontTakeDamage) return;

            D4CGlobalNPC_Tier_4 g = npc.GetGlobalNPC<D4CGlobalNPC_Tier_4>();
            g.d4cMarkTimer = 240;
            g.OwnerIndex = owner.whoAmI;
            npc.netUpdate = true;

            ClonJugadorNPC_Tier_4.ObjetivoMarcadoPorJugador[owner.whoAmI] = npcIndexMarcado;
        }
    }

    // Puente de red para invocar el grupo completo de aliados de la Skill G (melee + ranged)
    // de forma sincronizada en multijugador. Se spawnea en el cliente dueño (por lo que se
    // sincroniza automáticamente al servidor, al ser un proyectil propio del jugador local) y
    // la invocación real de los NPCs solo se ejecuta en el lado autoritativo (server/singleplayer).
    // ai[0] = cantidad de aliados melee a invocar
    // ai[1] = cantidad de aliados ranged a invocar
    public class D4CAliadosBridgeProjectile_Tier_4 : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_1";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.timeLeft = 2;
            Projectile.alpha = 255;
            Projectile.hide = true;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (Projectile.localAI[0] != 0) return;
            Projectile.localAI[0] = 1;

            Player owner = Main.player[Projectile.owner];
            if (!owner.active) return;

            int cantidadMelee = (int)Projectile.ai[0];
            int cantidadRanged = (int)Projectile.ai[1];

            InvocarGrupoServidor(owner, Projectile.Center, cantidadMelee, cantidadRanged);
        }

        // Llamado desde el cliente dueño (D4CSTAND_Tier_4.UpdateSkills)
        public static void InvocarGrupoCompleto(Player owner, Vector2 posicion, int cantidadMelee, int cantidadRanged)
        {
            Projectile.NewProjectile(
                owner.GetSource_FromThis("D4C_GrupoAliados"),
                posicion,
                Vector2.Zero,
                ModContent.ProjectileType<D4CAliadosBridgeProjectile_Tier_4>(),
                0, 0f, owner.whoAmI, cantidadMelee, cantidadRanged
            );
        }

        static void InvocarGrupoServidor(Player owner, Vector2 posicion, int cantidadMelee, int cantidadRanged)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            AliadoMeleeNPC_Tier_4.EliminarAliadosDelJugadorServidor(owner);
            AliadoRangedNPC_Tier_4.EliminarAliadosDelJugadorServidor(owner);

            for (int i = 0; i < cantidadMelee; i++)
            {
                float angulo = MathHelper.TwoPi * i / cantidadMelee;
                AliadoMeleeNPC_Tier_4.InvocarUnAliadoServidor(owner, posicion, angulo);
            }

            for (int i = 0; i < cantidadRanged; i++)
            {
                float angulo = MathHelper.TwoPi * i / cantidadRanged;
                AliadoRangedNPC_Tier_4.InvocarUnAliadoServidor(owner, posicion, angulo);
            }
        }
    }
}