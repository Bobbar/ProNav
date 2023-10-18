using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

    }
}
