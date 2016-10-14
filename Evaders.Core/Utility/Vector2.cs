namespace Evaders.Core.Utility
{
    using System;

    public struct Vector2
    {
        public double X, Y;
        public double Length => Math.Sqrt(X*X + Y*Y);
        public double LengthSqr => X*X + Y*Y;
        public Vector2 Unit => this/Length;
        public bool IsUnitVector => LengthSqr - 1d <= double.Epsilon;

        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double Distance(Vector2 other, bool squared = false) => squared ? (this - other).LengthSqr : (this - other).Length;

        public Vector2 Extended(Vector2 targetPoint, double length) => (targetPoint - this).Unit*length + this;

        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator *(Vector2 a, Vector2 b) => new Vector2(a.X*b.X, a.Y*b.Y);
        public static Vector2 operator /(Vector2 a, Vector2 b) => new Vector2(a.X/b.X, a.Y/b.Y);
        public static Vector2 operator *(Vector2 a, double b) => new Vector2(a.X*b, a.Y*b);
        public static Vector2 operator /(Vector2 a, double b) => new Vector2(a.X/b, a.Y/b);


        private const double DegToRad = Math.PI/180;

        public Vector2 RotatedDegrees(double degrees)
        {
            return RotatedRadians(degrees*DegToRad);
        }

        public Vector2 RotatedRadians(double radians)
        {
            var ca = Math.Cos(radians);
            var sa = Math.Sin(radians);
            return new Vector2(ca*X - sa*Y, sa*X + ca*Y);
        }

        public override string ToString()
        {
            return $"{{{X}/{Y}}}";
        }
    }
}