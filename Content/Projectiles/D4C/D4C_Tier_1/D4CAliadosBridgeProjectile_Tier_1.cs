using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.D4C.D4C_Tier_1
{
    // Puente de red para sincronizar la marca de objetivo (click derecho) al servidor.
    public class D4CMarcaBridgeProjectile_Tier_1 : ModProjectile
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

                D4CGlobalNPC_Tier_1 gOtro = otro.GetGlobalNPC<D4CGlobalNPC_Tier_1>();
                if (gOtro.d4cMarkTimer > 0 && gOtro.OwnerIndex == owner.whoAmI)
                {
                    gOtro.d4cMarkTimer = 0;
                    gOtro.OwnerIndex = -1;
                    otro.netUpdate = true;
                }
            }

            if (npcIndexMarcado < 0 || npcIndexMarcado >= Main.maxNPCs)
            {
                D4CGlobalNPC_Tier_1.ObjetivoMarcadoPorJugador[owner.whoAmI] = -1;
                return;
            }

            NPC npc = Main.npc[npcIndexMarcado];
            if (!npc.active || npc.friendly || npc.life <= 0 || npc.dontTakeDamage) return;

            D4CGlobalNPC_Tier_1 g = npc.GetGlobalNPC<D4CGlobalNPC_Tier_1>();
            g.d4cMarkTimer = 240;
            g.OwnerIndex = owner.whoAmI;
            npc.netUpdate = true;

            D4CGlobalNPC_Tier_1.ObjetivoMarcadoPorJugador[owner.whoAmI] = npcIndexMarcado;
        }
    }

    // Puente de red para invocar el grupo completo de aliados (melee + ranged) de forma
    // sincronizada en multijugador. Es la ÚNICA habilidad activa que le queda al Tier 1.
    // ai[0] = cantidad de aliados melee, ai[1] = cantidad de aliados ranged
    public class D4CAliadosBridgeProjectile_Tier_1 : ModProjectile
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

        public static void InvocarGrupoCompleto(Player owner, Vector2 posicion, int cantidadMelee, int cantidadRanged)
        {
            Projectile.NewProjectile(
                owner.GetSource_FromThis("D4C_GrupoAliados"),
                posicion,
                Vector2.Zero,
                ModContent.ProjectileType<D4CAliadosBridgeProjectile_Tier_1>(),
                0, 0f, owner.whoAmI, cantidadMelee, cantidadRanged
            );
        }

        static void InvocarGrupoServidor(Player owner, Vector2 posicion, int cantidadMelee, int cantidadRanged)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            AliadoMeleeNPC_Tier_1.EliminarAliadosDelJugadorServidor(owner);
            AliadoRangedNPC_Tier_1.EliminarAliadosDelJugadorServidor(owner);

            for (int i = 0; i < cantidadMelee; i++)
            {
                float angulo = MathHelper.TwoPi * i / cantidadMelee;
                AliadoMeleeNPC_Tier_1.InvocarUnAliadoServidor(owner, posicion, angulo);
            }

            for (int i = 0; i < cantidadRanged; i++)
            {
                float angulo = MathHelper.TwoPi * i / cantidadRanged;
                AliadoRangedNPC_Tier_1.InvocarUnAliadoServidor(owner, posicion, angulo);
            }
        }
    }

    // Puente de limpieza total al desaparecer el Stand (multijugador). Sin clones ni
    // fantasma en el Tier 1, aquí solo se eliminan los aliados melee/ranged.
    public class D4CRedBridgeProjectile_Tier_1 : ModProjectile
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

            if (Projectile.ai[0] == 3f)
            {
                AliadoMeleeNPC_Tier_1.EliminarAliadosDelJugadorServidor(owner);
                AliadoRangedNPC_Tier_1.EliminarAliadosDelJugadorServidor(owner);
            }
        }
    }
}