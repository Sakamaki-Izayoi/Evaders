namespace Evaders.Core.Utility
{
    using System;

    public static class Geometry
    {
        // Determines if the lines intersect
        public static bool LinesIntersect(Vector2 startA, Vector2 endA, Vector2 startB, Vector2 endB, out Vector2 intersectionPoint)
        {
            var CmP = new Vector2(startB.X - startA.X, startB.Y - startA.Y);
            var r = new Vector2(endA.X - startA.X, endA.Y - startA.Y);
            var s = new Vector2(endB.X - startB.X, endB.Y - startB.Y);

            var CmPxr = CmP.X*r.Y - CmP.Y*r.X;
            var CmPxs = CmP.X*s.Y - CmP.Y*s.X;
            var rxs = r.X*s.Y - r.Y*s.X;
            intersectionPoint = new Vector2(0, 0);
            if (Math.Abs(CmPxr) <= double.Epsilon)
                // Lines are collinear, and so intersect if they have any overlap

                return (startB.X - startA.X < 0f != startB.X - endA.X < 0f) || (startB.Y - startA.Y < 0f != startB.Y - endA.Y < 0f);

            if (Math.Abs(rxs) <= double.Epsilon)
                return false; // Lines are parallel.

            var rxsr = 1f/rxs;
            var t = CmPxs*rxsr;
            var u = CmPxr*rxsr;

            var colliding = (t >= 0f) && (t <= 1f) && (u >= 0f) && (u <= 1f);

            if (colliding)
                intersectionPoint = startA + (endA - startA)*t;

            return colliding;
        }
    }
}