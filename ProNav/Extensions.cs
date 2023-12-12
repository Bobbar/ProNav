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

        public static D2DPoint Subtract(this D2DPoint point, float value)
        {
            return new D2DPoint(point.X - value, point.Y - value);
        }

        public static float NextFloat(this Random rnd, float min, float max)
        {
            return (float)rnd.NextDouble() * (max - min) + min;
        }

        public static D2DPoint Normalized(this D2DPoint point)
        {
            return D2DPoint.Normalize(point);
        }

        public static float Angle(this D2DPoint vector, bool clamp = false)
        {
            var angle = (float)Math.Atan2(vector.Y, vector.X) * (180f / (float)Math.PI);

            if (clamp)
                angle = Helpers.ClampAngle(angle);

            return angle;
        }

        public static float AngleRads(this D2DPoint vector, bool clamp = false)
        {
            var angle = (float)Math.Atan2(vector.Y, vector.X);

            if (clamp)
                angle = Helpers.ClampAngle(angle);

            return angle;
        }

        public static double AngleD(this D2DPoint vector, bool clamp = false)
        {
            var angle = Math.Atan2(vector.Y, vector.X) * (180d / Math.PI);

            if (clamp)
                angle = Helpers.ClampAngleD(angle);

            return angle;
        }

        public static float Cross(this D2DPoint vector, D2DPoint other)
        {
            return Helpers.Cross(vector, other);
        }

        public static float AngleBetween(this D2DPoint vector, D2DPoint other, bool clamp = false)
        {
            return Helpers.AngleBetween(vector, other, clamp);
        }
    }
}
