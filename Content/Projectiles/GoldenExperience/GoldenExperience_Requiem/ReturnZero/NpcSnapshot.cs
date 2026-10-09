using Microsoft.Xna.Framework;

namespace Jojo.Content.Projectiles.GoldenExperience.GoldenExperience_Requiem.ReturnZero
{
    public struct NpcSnapshot
    {
        public Vector2 Position;
        public Vector2 Velocity; // ¡Guardamos la velocidad real!
        public float Rotation;
        public int SpriteDirection;
        public Rectangle Frame;

        // ¡Guardamos la memoria del boss para que no se congele!
        public float AI0, AI1, AI2, AI3;
        public float LocalAI0, LocalAI1, LocalAI2, LocalAI3;
        public int Target;

        public NpcSnapshot(
            Vector2 position, Vector2 velocity, float rotation, int spriteDirection, Rectangle frame,
            float ai0, float ai1, float ai2, float ai3,
            float localAI0, float localAI1, float localAI2, float localAI3,
            int target)
        {
            Position = position;
            Velocity = velocity;
            Rotation = rotation;
            SpriteDirection = spriteDirection;
            Frame = frame;
            AI0 = ai0; AI1 = ai1; AI2 = ai2; AI3 = ai3;
            LocalAI0 = localAI0; LocalAI1 = localAI1; LocalAI2 = localAI2; LocalAI3 = localAI3;
            Target = target;
        }

        public NpcSnapshot ConDireccion(int nuevaDireccion)
        {
            // Al ser un struct (tipo de valor), esto crea una copia automáticamente
            var copy = this;
            copy.SpriteDirection = nuevaDireccion;
            return copy;
        }
    }
}