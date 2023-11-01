using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unvell.D2DLib;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ProNav.GameObjects
{
    public class Explosion : GameObjectPoly
    {
        public float MaxRadius { get; set; } = 100f;
        public float Duration { get; set; } = 1f;

        private float _currentRadius = 0f;
        private float _age = 0f;
        private D2DColor _color = new D2DColor(0.4f, D2DColor.Orange);

        public Explosion(D2DPoint pos, float maxRadius, float duration) : base(pos) 
        { 
            this.MaxRadius = maxRadius;
            this.Duration = duration;
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            base.Update(dt, viewport, renderScale);

            _currentRadius = MaxRadius * (_age / Duration);

            _age += dt;

            if (_age >= Duration)
                this.IsExpired = true;
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
