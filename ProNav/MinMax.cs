namespace ProNav
{
    public class MinMax
    {
        public float MinX;
        public float MinY;
        public float MaxX;
        public float MaxY;

        public float Width
        {
            get
            {
                return Math.Abs(MinX - MaxX);
            }
        }

        public float Height
        {
            get
            {
                return Math.Abs(MinY - MaxY);
            }
        }

        public MinMax()
        {
            MinX = float.MaxValue;
            MinY = float.MaxValue;
            MaxX = float.MinValue;
            MaxY = float.MinValue;
        }

        public MinMax(float minX, float minY, float maxX, float maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public void Update(float x, float y)
        {
            MinX = Math.Min(MinX, x);
            MinY = Math.Min(MinY, y);
            MaxX = Math.Max(MaxX, x);
            MaxY = Math.Max(MaxY, y);
        }

        public void Update(MinMax minMax)
        {
            MinX = Math.Min(MinX, minMax.MinX);
            MinY = Math.Min(MinY, minMax.MinY);
            MaxX = Math.Max(MaxX, minMax.MaxX);
            MaxY = Math.Max(MaxY, minMax.MaxY);
        }

        public void Reset()
        {
            MinX = float.MaxValue;
            MinY = float.MaxValue;
            MaxX = float.MinValue;
            MaxY = float.MinValue;
        }
    }
}
