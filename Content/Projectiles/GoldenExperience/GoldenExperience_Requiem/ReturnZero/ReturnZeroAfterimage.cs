// Jojo/Content/Projectiles/GoldenExperience/GoldenExperience_Requiem/ReturnZero/ReturnZeroAfterimage.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Jojo.Content.Clases;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem.ReturnZero
{
    public class ReturnZeroAfterimage : ModProjectile
    {
        public int npcType;
        public Rectangle frame;
        public int direccion = 1;
        public float escalaNpc = 1f;
        public float rotacion = 0f;

        public int npcWhoAmI = -1;

        // Si es false, este clon es puramente visual (segmentos "no representantes" de un
        // grupo de vida compartida, como los gusanos) y NO debe infligir daño al tocar al dueño.
        public bool puedeDañar = true;

        // Opacidad del sprite del clon (0 = invisible, 1 = opaco total).
        // Antes se dibujaba con Color.White a alpha completo pese a llamarse "sombra"; ahora
        // se multiplica el color por este valor para que realmente sea semitransparente.
        public float opacidad = 0.45f;

        const int RETRASO_ANTES_DE_CHEQUEAR = 6;

        public override string Texture => "Terraria/Images/Projectile_1";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60 * 20; // despawn de seguridad a los 20s si nunca lo toca el dueño
            Projectile.alpha = 0;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Main.instance.LoadNPC(npcType);
                Projectile.localAI[0] = 1f;
            }

            Projectile.localAI[1]++;

            if (Projectile.localAI[1] < RETRASO_ANTES_DE_CHEQUEAR) return;
            if (npcWhoAmI < 0 || npcWhoAmI >= Main.maxNPCs) return;

            NPC dueño = Main.npc[npcWhoAmI];
            if (!dueño.active) return;

            float radioToque = System.Math.Max(dueño.width, dueño.height) * 0.55f + 12f;

            if (Vector2.Distance(dueño.Center, Projectile.Center) <= radioToque)
            {
                if (puedeDañar)
                    GolpearAlPasar(dueño);

                Projectile.Kill();
            }
        }

        void GolpearAlPasar(NPC npc)
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead) return;

            // Daño del 2% de la vida máxima del enemigo. Ignora los aumentos de daño del jugador.
            int dmg = (int)(npc.lifeMax * 0.02f);
            if (dmg < 1) dmg = 1; // Para enemigos con menos de 50 de vida

            int direccionGolpe = npc.Center.X >= owner.Center.X ? 1 : -1;

            npc.SimpleStrikeNPC(dmg, direccionGolpe, false, 0f, ModContent.GetInstance<ClaseStand>(), false);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 8; i++)
            {
                Dust.NewDust(
                    Projectile.position,
                    Projectile.width + 20,
                    Projectile.height + 20,
                    DustID.GemEmerald,
                    0f, 0f, 100, default, 1.3f
                );
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Npc[npcType].Value;

            Rectangle marco = frame;
            if (marco.Width <= 0 || marco.Height <= 0)
                marco = tex.Bounds;

            // Color.White * opacidad reduce tanto RGB como alpha de forma premultiplicada,
            // que es como Terraria espera el color para que el sprite salga translúcido de
            // verdad (con Color.White a secas siempre se dibuja opaco al 100%).
            Color colorSombra = Color.White * opacidad;

            Main.EntitySpriteDraw(
                tex,
                Projectile.Center - Main.screenPosition,
                marco,
                colorSombra,
                rotacion,
                new Vector2(marco.Width / 2f, marco.Height / 2f),
                escalaNpc,
                direccion < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                0f
            );

            return false;
        }
    }
}