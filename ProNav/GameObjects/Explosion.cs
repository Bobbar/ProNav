using unvell.D2DLib;

namespace ProNav.GameObjects
{
    public class Explosion : GameObjectPoly
    {
        public float MaxRadius { get; set; } = 100f;
        public float Duration { get; set; } = 1f;

        private float _currentRadius = 0f;
        private float _age = 0f;
        private D2DColor _color = new D2DColor(0.2f, D2DColor.Orange);

        public Explosion(D2DPoint pos, float maxRadius, float duration) : base(pos) 
        { 
            this.MaxRadius = maxRadius;
            this.Duration = duration;

            _color.r = _rnd.NextFloat(0.8f, 1f);
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            base.Update(dt, viewport, renderScale);

            //_currentRadius = MaxRadius * (_age / Duration);
            //_currentRadius = MaxRadius * EaseQuinticOut(_age / Duration);
            //_currentRadius = MaxRadius * EaseQuinticIn(_age / Duration);
            //_currentRadius = MaxRadius * EaseOutElastic(_age / Duration);
            _currentRadius = MaxRadius * EaseOutBack(_age / Duration);

            _age += dt;

            if (_age >= Duration)
                this.IsExpired = true;
        }

        private float EaseQuinticOut(float k)
        {
            return 1f + ((k -= 1f) * (float)Math.Pow(k, 4));
        }
        private float EaseQuinticIn(float k)
        {
            return k * k * k * k * k;
        }

        private float EaseOutElastic(float k)
        {
            const float c4 = (2f * (float)Math.PI) / 3f;

            return k == 0f ? 0f : k == 1f ? 1f : (float)Math.Pow(2f, -10f * k) * (float)Math.Sin((k * 10f - 0.75f) * c4) + 1f;
        }

        private float EaseOutBack(float k)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;

            return (float)(1f + c3 * Math.Pow(k - 1f, 3f) + c1 * Math.Pow(k - 1f, 2f));
        }

        public override void Render(D2DGraphics gfx)
        {
            gfx.FillEllipse(new D2DEllipse(this.Position, new D2DSize(_currentRadius, _currentRadius)), _color);
        }

        public override bool Contains(D2DPoint pnt)
        {
            var dist = D2DPoint.Distance(pnt, this.Position);

            return dist < _currentRadius;
        }
    }
}
