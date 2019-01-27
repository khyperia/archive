using System;

namespace StellarStack
{
    struct Vector2
    {
        public readonly float X, Y;

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 operator -(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X - right.X, left.Y - right.Y);
        }

        public static Vector2 operator -(Vector2 right)
        {
            return new Vector2(-right.X, -right.Y);
        }

        public static Vector2 operator +(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X + right.X, left.Y + right.Y);
        }

        public static Vector2 operator /(Vector2 left, float right)
        {
            return new Vector2(left.X / right, left.Y / right);
        }

        public float LengthSquared
        { get { return X * X + Y * Y; } }

        public float Length
        { get { return (float)Math.Sqrt(LengthSquared); } }

        public static Vector2 NaN
        {
            get { return new Vector2(float.NaN, float.NaN); }
        }

        public bool IsNaN
        {
            get { return float.IsNaN(X) || float.IsNaN(Y); }
        }
    }
}
