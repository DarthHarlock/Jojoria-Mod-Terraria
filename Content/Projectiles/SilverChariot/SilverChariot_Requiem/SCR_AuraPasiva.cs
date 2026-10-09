using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.SilverChariot.SilverChariot_Requiem
{
    // Aura PASIVA invisible. Misma lógica de detección que antes, pero
    // sin ningún efecto visual (ni dust ni luz): solo existe mientras
    // el Stand referenciado en ai[0] siga vivo.
    public class SCR_AuraPasiva : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        // Un poco más grande que el aura de la habilidad F (350f)
        const float AuraRadius = 800f;

        public override void SetDefaults()
        {
            Projectile.width = (int)(AuraRadius * 2);
            Projectile.height = (int)(AuraRadius * 2);
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.hide = true; // no se dibuja el sprite
        }

        // El aura no debe cortar césped, flores ni vides.
        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            // Único requisito de vida: que el Stand siga activo.
            Projectile stand = Main.projectile[(int)Projectile.ai[0]];
            if (!stand.active || stand.type != ModContent.ProjectileType<SILVERCHARIOTSTAND_Requiem>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = stand.Center;
            Projectile.timeLeft = 2; // se auto-renueva mientras el Stand exista

            // --- ESCANEO DE ENEMIGOS (sin ningún efecto visual) ---
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && !npc.dontTakeDamage)
                {
                    if (Vector2.Distance(npc.Center, Projectile.Center) <= AuraRadius)
                    {
                        npc.GetGlobalNPC<AuraPasivaGlobalNPC>().lastPassiveAuraHit = Main.GameUpdateCount;
                    }
                }
            }
        }
    }
}