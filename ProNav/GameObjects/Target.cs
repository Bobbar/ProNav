using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unvell.D2DLib;
using ProNav;

namespace ProNav.GameObjects
{
    public abstract class Target : GameObjectPoly
    {
        public bool IsDestroyed = false;

        public Target() { }
        public Target(D2DPoint pos) : base(pos) { }
    }

    public class LinearMovingTarget : Target
    {
        private const float MIN_MAX_VELO = 220f;


        public LinearMovingTarget() { }
        public LinearMovingTarget(D2DPoint pos) : base(pos) 
        {
            this.Polygon = new RenderPoly(GameObjectPoly.RandomPoly(8, 50));

            //this.srcPoly = base.RandomPoly(8, 12);
            this.Velocity = new D2DPoint(_rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO), _rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO));
        }


        public override void Render(D2DGraphics gfx)
        {
            //gfx.DrawPolygon(this.Polygon, D2DColor.White, 1f, D2DDashStyle.Solid, D2DColor.White);

            gfx.DrawPolygon(this.Polygon.Poly, D2DColor.White, 1f, D2DDashStyle.Solid, D2DColor.White);
        }

    }


    public class RotatingMovingTarget : Target
    {
        private const float MIN_MAX_VELO = 220f;
        private const float MIN_MAX_ROT = 90f;

        public RotatingMovingTarget() { }
        public RotatingMovingTarget(D2DPoint pos) : base(pos)
        {
            this.Polygon = new RenderPoly(GameObjectPoly.RandomPoly(8, 50));

            this.Velocity = new D2DPoint(_rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO), _rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO));
            this.RotationSpeed = _rnd.NextFloat(-MIN_MAX_ROT, MIN_MAX_ROT);
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            base.Update(dt, viewport, renderScale);

            var rads = -this.Rotation * ((float)Math.PI / 180f);
            var vec = new D2DPoint((float)Math.Cos(rads), (float)Math.Sin(rads));
            this.Velocity = vec * this.Velocity.Length();
        }

        public override void Render(D2DGraphics gfx)
        {
            //gfx.DrawPolygon(this.Polygon, D2DColor.White, 1f, D2DDashStyle.Solid, D2DColor.White);

            gfx.DrawPolygon(this.Polygon.Poly, D2DColor.White, 1f, D2DDashStyle.Solid, D2DColor.White);
        }

    }


    public class ErraticMovingTarget : Target
    {
        private const float MIN_MAX_VELO = 220f;
        private const float MIN_MAX_ROT = 80f;
        private long _frame = 0;
        private int _nextRotMod = 100;
        private int _nextVeloMod = 100;
        private float _targRot = 0;
        private D2DPoint _targVelo = D2DPoint.Zero;

        private SmoothFloat _rotSmooth = new SmoothFloat(300);
        private SmoothPos _veloSmooth = new SmoothPos(300);

        public ErraticMovingTarget() { }
        public ErraticMovingTarget(D2DPoint pos) : base(pos)
        {
            this.Polygon = new RenderPoly(GameObjectPoly.RandomPoly(8, 22));

            this.Velocity = new D2DPoint(_rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO), _rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO));
            this.RotationSpeed = _rnd.NextFloat(-MIN_MAX_ROT, MIN_MAX_ROT);
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            base.Update(dt, viewport, renderScale);

            if (_frame % _nextRotMod == 0)
            {
                _targRot = _rnd.NextFloat(-MIN_MAX_ROT, MIN_MAX_ROT);

                _targRot = -this.RotationSpeed;
                _nextRotMod = _rnd.Next(500, 1000);
            }

            this.RotationSpeed = _rotSmooth.Add(_targRot);


            if (_frame % _nextVeloMod == 0)
            {
                _targVelo = new D2DPoint(_rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO), _rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO));
                _nextVeloMod = _rnd.Next(500, 1000);
            }

            this.Velocity = _veloSmooth.Add(_targVelo);


            var rads = -this.Rotation * ((float)Math.PI / 180f);
            var vec = new D2DPoint((float)Math.Cos(rads), (float)Math.Sin(rads));
            this.Velocity = vec * this.Velocity.Length();

            _frame++;
        }

        public override void Render(D2DGraphics gfx)
        {
            //gfx.DrawPolygon(this.Polygon, D2DColor.White, 1f, D2DDashStyle.Solid, D2DColor.White);

            gfx.DrawPolygon(this.Polygon.Poly, D2DColor.White, 1f, D2DDashStyle.Solid, D2DColor.White);
        }

    }
}
