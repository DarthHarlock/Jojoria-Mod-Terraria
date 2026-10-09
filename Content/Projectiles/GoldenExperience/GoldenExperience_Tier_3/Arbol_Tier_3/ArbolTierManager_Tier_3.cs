using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Tier_3.Arbol_Tier_3
{
    public static class ArbolTierManager_Tier_3
    {
        static readonly SoundStyle ArbolSummonSound = SoundID.Item29 with { Volume = 0.9f, Pitch = 0.1f };

        // =========================================================================
        // FIX DEFINITIVO DEL BUG "SE BORRAN TROZOS DE ÁRBOLES VIEJOS AL INVOCAR OTRO":
        //
        // El árbol es un PROYECTIL (Arbol_Torre_Controller), no un NPC. Antes este
        // método se llamaba SOLO desde la copia con autoridad de red (servidor
        // dedicado o host), es decir, desde una máquina DISTINTA a la del jugador
        // dueño cuando ese jugador es un cliente normal conectado a un servidor.
        //
        // El problema: el servidor elegía el slot libre del tronco según SU PROPIO
        // array de proyectiles, completamente independiente del array del cliente
        // dueño. Las PIEZAS del árbol, en cambio, siempre se crean en el cliente
        // dueño (Arbol_Torre_Controller.ColocarPieza solo corre si
        // Projectile.owner == Main.myPlayer), usando el array local de ESE mismo
        // cliente.
        //
        // Resultado: el servidor podía asignarle al tronco nuevo un slot que, en el
        // array del cliente dueño, ya estaba ocupado por una pieza de un árbol
        // anterior (todavía viva). El paquete de red que sincroniza el tronco nuevo
        // llegaba al cliente dueño y sobreescribía ese slot sin más, borrando la
        // pieza vieja sin animación de muerte.
        //
        // LA SOLUCIÓN: el tronco se crea ahora en la MISMA máquina y con el MISMO
        // criterio que sus piezas (el cliente dueño), así ambos comparten el mismo
        // array de proyectiles y nunca pueden pisarse entre sí. Quien invoca esta
        // función ahora es GOLDENSTAND_Tier_3 comprobando "isOwner" (ver ese
        // archivo), no la autoridad de red del servidor.
        // =========================================================================
        public static void SpawnArbol(Player p, Vector2? customPos = null)
        {
            Vector2 spawnPos = customPos ?? p.Bottom;

            SoundEngine.PlaySound(ArbolSummonSound, spawnPos);

            int index = Projectile.NewProjectile(
                p.GetSource_FromThis(),
                spawnPos,
                Vector2.Zero,
                ModContent.ProjectileType<Arbol_Torre_Controller_Tier_3>(),
                0,
                0f,
                p.whoAmI
            );

            // Igual que en Arbol_Torre_Controller.ColocarPieza: si esto corre en un
            // cliente normal, Projectile.NewProjectile YA envía el paquete de sync
            // automáticamente (porque owner == Main.myPlayer). Si esto corre en el
            // host (Main.netMode == Server pero jugando), ese auto-envío no se
            // dispara, así que hace falta este envío manual para que los demás
            // clientes vean el árbol. En singleplayer no hace falta nada.
            if (index >= 0 && index < Main.maxProjectiles && Main.netMode != NetmodeID.SinglePlayer)
            {
                NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, index);
            }
        }
    }
}