using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace Jojo.Content.Projectiles.KillerQueen.KillerQueen_Tier_4.BitesTheDust
{
    public class BitesTheDustNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool markedForBitesTheDust = false;

        public Vector2 savedPosition;
        public Rectangle savedFrame;
        public int savedSpriteDirection;
    }
}