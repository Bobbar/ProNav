using unvell.D2DLib;

namespace ProNav
{
    public class Graph
    {
        private List<float[]> _data;
        private Color[] _colors;
        private MinMax[] _valBounds;
        private int _numVals = 0;
        private SizeF _drawSize;
        private int _dataIdx = 0;
        private float _xPosition = 0;
        private float _xPadding = 2;
        private float _xMax = 0f;
        private float _pointSize = 1f;

        public Graph(SizeF size, params float[] vals)
        {
            _drawSize = size;
            _xMax = size.Width / _xPadding;

            Init(vals);
        }

        public Graph(SizeF size, Color[] colors, params float[] vals)
        {
            _drawSize = size;
            _xMax = size.Width / _xPadding;
            _colors = colors;

            Init(vals);
        }

        public Graph(SizeF size, Color[] colors, int numVals)
        {
            _drawSize = size;
            _xMax = size.Width / _xPadding;
            _colors = colors;

            Init(new float[numVals]);
        }

        private void Init(float[] vals)
        {
            _data = new List<float[]>();
            _valBounds = new MinMax[vals.Length];
            _numVals = vals.Length;

            for (int i = 0; i < _numVals; i++)
            {
                _valBounds[i] = new MinMax();
                _valBounds[i].Update(0f, vals[i]);
            }
        }

        public void Update(params float[] vals)
        {
            if (vals.Length != _numVals)
                Init(vals);

            if (_data.Count < _xMax)
                _data.Add(vals);
            else
                _data[_dataIdx] = vals;

            UpdateBounds(vals);

            _dataIdx = (_dataIdx + 1) % _data.Count;
            _xPosition = (_xPosition + _xPadding) % _drawSize.Width;
        }

        private void UpdateBounds(float[] vals)
        {
            for (int i = 0; i < vals.Length; i++)
            {
                _valBounds[i].Update(0f, vals[i]);
            }
        }

        public void Render(D2DGraphics gfx, D2DPoint pos, float scale = 1f)
        {
            gfx.PushTransform();
            gfx.ScaleTransform(scale, scale);

            var tmax = pos.Y + (_drawSize.Height * 0.5f);
            var tmin = pos.Y - (_drawSize.Height * 0.5f);

            for (int i = 0; i < _data.Count; i++)
            {
                var d = _data[i];

                for (int j = 0; j < d.Length; j++)
                {
                    var v = d[j];
                    var vScaled = ScaleValue(v, pos, _valBounds[j]);
                    vScaled = tmax - vScaled + tmin; // Flip Y direction.

                    var pnt = new D2DPoint((i * _xPadding) + pos.X - (_drawSize.Width * 0.5f), vScaled);

                    gfx.FillEllipse(new D2DEllipse(pnt, new D2DSize(_pointSize, _pointSize)), D2DColor.FromGDIColor(_colors[j % _colors.Length]));
                    //gfx.DrawEllipse(new D2DEllipse(pnt, new D2DSize(5f, 5f)), D2DColor.FromGDIColor(_colors[j % _colors.Length]));
                }
            }

            gfx.DrawLine(new D2DPoint(pos.X + _xPosition - _drawSize.Width * 0.5f, pos.Y + _drawSize.Height * 0.5f), new D2DPoint(pos.X + _xPosition - _drawSize.Width * 0.5f, pos.Y - _drawSize.Height * 0.5f), D2DColor.LightGray);
            gfx.DrawRectangle(new D2DRect(pos, new D2DSize(_drawSize.Width, _drawSize.Height)), D2DColor.White);

            gfx.PopTransform();
        }

        private float ScaleValue(float value, D2DPoint pos, MinMax range)
        {
            var tmax = pos.Y + (_drawSize.Height * 0.5f);
            var tmin = pos.Y - (_drawSize.Height * 0.5f);

            var scaled = ((value - range.MinY) / (range.MaxY - range.MinY)) * (tmax - tmin) + tmin;
            return scaled;
        }

    }
}
