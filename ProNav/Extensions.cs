using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ProNav
{
    public static class Extensions
    {
        public static D2DPoint Add(this D2DPoint point, D2DPoint other)
        {
            return new D2DPoint(point.X + other.X, point.Y + other.Y);
        }

        public static D2DPoint Add(this D2DPoint point, float value)
        {
            return new D2DPoint(point.X + value, point.Y + value);
        }

        public static D2DPoint Subtract(this D2DPoint point, D2DPoint other)
        {
            return new D2DPoint(point.X - other.X, point.Y - other.Y);
        }

        public static float NextFloat(this Random rnd, float min, float max)
        {
            return (float)rnd.NextDouble() * (max - min) + min;
        }

        public static D2DPoint Normalized(this D2DPoint point)
        {
            return D2DPoint.Normalize(point);
        }

        public static float Angle(this D2DPoint vector)
        {
            var angle = Math.Atan2(vector.Y, vector.X) * (180f / (float)Math.PI);

            angle = angle % 360f;

            if (angle < 0f)
                angle += 360f;

            return (float)angle;
        }

        public static double AngleD(this D2DPoint vector)
        {
            var angle = Math.Atan2(vector.Y, vector.X) * (180d / Math.PI);

            angle = angle % 360d;

            if (angle < 0d)
                angle += 360d;

            return angle;
        }

        public static float Cross(this D2DPoint vector, D2DPoint other)
        {
            return Helpers.Cross(vector, other);
        }

        public static float AngleBetween(this D2DPoint vector, D2DPoint other)
        {
            return Helpers.AngleBetween(vector, other);
        }
    }
}
