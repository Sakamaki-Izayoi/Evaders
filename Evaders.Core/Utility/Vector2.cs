namespace Evaders.Core.Utility
{
    using System;

    public struct Vector2
    {
        public float X, Y;
        public float Length => (float) Math.Sqrt(X*X + Y*Y);
        public float LengthSqr => X*X + Y*Y;
        public Vector2 Unit => this/Length;

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float Distance(Vector2 other, bool squared = false) => squared ? (this - other).LengthSqr : (this - other).Length;

        public Vector2 Extended(Vector2 direction, float length) => (direction - this).Unit*length + this;

        public void Extend(Vector2 direction, float length)
        {
            var ex = (direction - this).Unit*length;
            X += ex.X;
            Y += ex.Y;
        }

        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator *(Vector2 a, Vector2 b) => new Vector2(a.X*b.X, a.Y*b.Y);
        public static Vector2 operator /(Vector2 a, Vector2 b) => new Vector2(a.X/b.X, a.Y/b.Y);
        public static Vector2 operator *(Vector2 a, float b) => new Vector2(a.X*b, a.Y*b);
        public static Vector2 operator /(Vector2 a, float b) => new Vector2(a.X/b, a.Y/b);


        private const double DegToRad = Math.PI/180;

        public Vector2 RotatedDegrees(double degrees)
        {
            return RotatedRadians(degrees*DegToRad);
        }

        public Vector2 RotatedRadians(double radians)
        {
            var ca = Math.Cos(radians);
            var sa = Math.Sin(radians);
            return new Vector2((float) (ca*X - sa*Y), (float) (sa*X + ca*Y));
        }
    }
}