using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProNav
{
    public static class Helpers
    {

        public static Random Rnd = new Random();


        public static float Lerp(float value1, float value2, float amount)
        {
            return value1 + (value2 - value1) * amount;
        }

        public static float LerpAngle(float value1, float value2, float amount)
        {
            float delta = Repeat((value2 - value1), 360);
            if (delta > 180)
                delta -= 360;

            var ret = value1 + delta * Clamp01(amount);

            ret = ClampAngle(ret);

            return ret;
        }

        public static float Repeat(float t, float length)
        {
            return Clamp(t - (float)Math.Floor(t / length) * length, 0.0f, length);
        }

        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
                value = min;
            else if (value > max)
                value = max;
            return value;
        }

        public static float Clamp01(float value)
        {
            if (value < 0F)
                return 0F;
            else if (value > 1F)
                return 1F;
            else
                return value;
        }

        public static D2DPoint LerpPoints(D2DPoint a, D2DPoint b, float amount)
        {
            var lerpX = Lerp(a.X, b.X, amount);
            var lerpY = Lerp(a.Y, b.Y, amount);

            return new D2DPoint(lerpX, lerpY);
        }


        public static float Factor(float value1, float value2)
        {
            return Math.Clamp(value1 / value2, 0f, 1f);
        }

        public static float RadsToDegrees(float rads)
        {
            return rads * (180f / (float)Math.PI);
        }

        public static float DegreesToRads(float degrees)
        {
            return degrees * ((float)Math.PI / 180f);
        }

        public static D2DPoint AngleToVector(float angle)
        {
            var rads = angle * ((float)Math.PI / 180f);
            var vec = new D2DPoint((float)Math.Cos(rads), (float)Math.Sin(rads));
            return vec;
        }

        public static D2DPoint AngleToVectorD(double angle)
        {
            var rads = angle * (Math.PI / 180d);
            var vec = new D2DPoint((float)Math.Cos(rads), (float)Math.Sin(rads));
            return vec;
        }

        public static float AngleBetween(D2DPoint vector, D2DPoint other)
        {
            var angA = vector.Angle();
            var angB = other.Angle();

            var angle = AngleDiff(angA, angB);

            return angle;
        }

        public static float AngleDiff(float a, float b)
        {
            var normDeg = ModSign((a - b), 360f);

            var absDiffDeg = Math.Min(360f - normDeg, normDeg);

            return absDiffDeg;
        }

        public static double AngleDiffD(double a, double b)
        {
            var normDeg = ModSignD((a - b), 360d);

            var absDiffDeg = Math.Min(360d - normDeg, normDeg);

            return absDiffDeg;
        }

        public static float ModSign(float a, float n)
        {
            return a - (float)Math.Floor(a / n) * n;
        }

        public static double ModSignD(double a, double n)
        {
            return a - Math.Floor(a / n) * n;
        }

        public static float FMod(float a, float n)
        {
            return a % n;
        }

        public static float ClampAngle(float angle)
        {
            var ret = angle % 360f;

            if (ret < 0f)
                ret += 360f;

            return ret;
        }

        public static float Cross(D2DPoint vector1, D2DPoint vector2)
        {
            return (vector1.X * vector2.Y) - (vector1.Y * vector2.X);
        }

    }
}
