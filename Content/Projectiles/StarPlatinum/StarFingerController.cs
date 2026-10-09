using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

// 1. Namespace actualizado a la carpeta raíz de Star Platinum
namespace Jojo.Content.Projectiles.StarPlatinum
{
    public class StarFingerController
    {
        public enum Phase : byte { Inactive, Growing, Holding, Retracting }

        // 2. "Puertos abiertos": Dejaron de ser 'const' para que cada Tier los modifique
        public int MaxSegments = 20;
        public float SegmentLength = 14f;
        public int TicksPerSegmentGrow = 1;
        public int TicksPerSegmentRetract = 1;
        public int HoldTimeTicks = 20;

        public Phase phase = Phase.Inactive;
        public int segmentCount = 0;
        public Vector2 origin;
        public Vector2 direction = Vector2.UnitX;

        int stepTimer;
        int holdTimer;

        public bool Active => phase != Phase.Inactive;

        public void Begin(Vector2 startOrigin, Vector2 startDirection)
        {
            phase = Phase.Growing;
            segmentCount = 0;
            stepTimer = 0;
            holdTimer = 0;
            origin = startOrigin;
            direction = startDirection == Vector2.Zero ? Vector2.UnitX : Vector2.Normalize(startDirection);
        }

        public void Reset()
        {
            phase = Phase.Inactive;
            segmentCount = 0;
            stepTimer = 0;
            holdTimer = 0;
        }

        public bool Update()
        {
            switch (phase)
            {
                case Phase.Inactive:
                    return false;

                case Phase.Growing:
                    if (++stepTimer >= TicksPerSegmentGrow)
                    {
                        stepTimer = 0;
                        segmentCount++;
                        if (segmentCount >= MaxSegments)
                        {
                            segmentCount = MaxSegments;
                            phase = Phase.Holding;
                            holdTimer = 0;
                        }
                    }
                    return true;

                case Phase.Holding:
                    if (++holdTimer >= HoldTimeTicks)
                    {
                        phase = Phase.Retracting;
                        stepTimer = 0;
                    }
                    return true;

                case Phase.Retracting:
                    if (++stepTimer >= TicksPerSegmentRetract)
                    {
                        stepTimer = 0;
                        segmentCount--;
                        if (segmentCount <= 0)
                        {
                            Reset();
                            return false;
                        }
                    }
                    return true;
            }

            return false;
        }

        public float CurrentLength => segmentCount * SegmentLength;

        public Vector2 GetTipPosition() => origin + direction * CurrentLength;

        public Vector2 GetSegmentPosition(int index) => origin + direction * (index * SegmentLength);

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)phase);
            writer.Write((byte)segmentCount);
            writer.Write(origin.X);
            writer.Write(origin.Y);
            writer.Write(direction.X);
            writer.Write(direction.Y);
        }

        public void Read(BinaryReader reader)
        {
            phase = (Phase)reader.ReadByte();
            segmentCount = reader.ReadByte();
            origin = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            direction = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        public void Draw(Texture2D segmentTex, Texture2D endTex, Color color, float scale)
        {
            if (segmentCount <= 0) return;

            Vector2 originSegment = new Vector2(0f, segmentTex.Height / 2f);
            Vector2 originEnd = new Vector2(0f, endTex.Height / 2f);
            float rot = direction.ToRotation();

            SpriteEffects effects = direction.X < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None;

            for (int i = 0; i < segmentCount; i++)
            {
                bool isTip = i == segmentCount - 1;
                Texture2D tex = isTip ? endTex : segmentTex;
                Vector2 texOrigin = isTip ? originEnd : originSegment;

                Vector2 drawPos = GetSegmentPosition(i) - Main.screenPosition;

                Main.EntitySpriteDraw(tex, drawPos, null, color, rot, texOrigin, scale, effects, 0f);
            }
        }

        public bool CheckHit(Rectangle targetHitbox, float lineWidth)
        {
            if (segmentCount <= 0) return false;

            Vector2 start = origin;
            Vector2 end = GetTipPosition();
            float collisionPoint = 0f;

            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, lineWidth, ref collisionPoint);
        }
    }
}