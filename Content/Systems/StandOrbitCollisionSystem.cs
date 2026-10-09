using Microsoft.Xna.Framework;
using Terraria;

namespace Jojo.Systems
{
    public static class StandOrbitCollisionSystem
    {
        // =========================================
        // LIMITE DE RADIO
        // =========================================
        public static Vector2 ClampToRadius(Vector2 offset, float maxRadius)
        {
            if (offset.LengthSquared() <= maxRadius * maxRadius)
                return offset;

            return Vector2.Normalize(offset) * maxRadius;
        }

        // =========================================
        // 🔥 NORMAL REAL DE IMPACTO EN BORDE CIRCULAR
        // =========================================
        public static Vector2 GetCollisionNormal(Vector2 offset, float maxRadius)
        {
            if (offset == Vector2.Zero)
                return Vector2.UnitX;

            if (offset.Length() < maxRadius)
                return Vector2.Zero;

            return Vector2.Normalize(offset);
        }

        // =========================================
        // ROTACIÓN REAL DE IMPACTO
        // =========================================
        public static float GetImpactRotation(Vector2 offset, float maxRadius)
        {
            Vector2 normal = GetCollisionNormal(offset, maxRadius);

            if (normal == Vector2.Zero)
                return 0f;

            return normal.ToRotation();
        }
    }
}