using Terraria;
using Terraria.ModLoader;
using Jojo.Content.Buffs.MadeInHeaven_Buffs;

namespace Jojo.Content.Players
{
    public class MIHDashPlayer : ModPlayer
    {
        public bool isOmniDashing;
        public bool mihActive;
        public bool mihUltimate;

        public override void ResetEffects()
        {
            isOmniDashing = false;
            mihActive = false;
            mihUltimate = false;
        }

        public override void SetControls()
        {
            if (isOmniDashing)
            {
                Player.controlLeft = false;
                Player.controlRight = false;
                Player.controlUp = false;
                Player.controlDown = false;
                Player.controlJump = false;
                Player.controlUseItem = false;
                Player.controlUseTile = false;
                Player.controlHook = false;
                Player.controlMount = false;
            }
        }

        // Aquí es donde se debe modificar la velocidad para que el juego no la reinicie
        public override void PostUpdateRunSpeeds()
        {
            if (mihActive)
            {
                if (mihUltimate)
                {
                    Player.moveSpeed += 0.60f; // 60% más de velocidad de movimiento
                    Player.maxRunSpeed += 6f;  // Aumenta el límite de velocidad máxima
                    Player.accRunSpeed += 1f;  // Acelera más rápido
                }
                else
                {
                    Player.moveSpeed += 0.30f; // 30% más de velocidad
                    Player.maxRunSpeed += 3f;
                    Player.accRunSpeed += 0.5f;
                }
            }
        }
    }
}