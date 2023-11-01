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
        public int NumPolyPoints { get; set; } = 8;
        public int PolyRadius { get; set; } = 80;

        protected readonly float MIN_MAX_VELO = 150f;
        protected readonly float MIN_MAX_ROT = 40f;

        public Target() { }

        public Target(D2DPoint pos) : base(pos) { }

        public Target(D2DPoint pos, int numPolyPoints, int polyRadius) : base(pos)
        {
            this.NumPolyPoints = numPolyPoints;
            this.PolyRadius = polyRadius;
        }
    }

    public class StaticTarget : Target
    {
        public StaticTarget(D2DPoint pos) : base(pos) 
        {
            this.Polygon = new RenderPoly(GameObjectPoly.RandomPoly(this.NumPolyPoints, this.PolyRadius));
        }

        public override void Render(D2DGraphics gfx)
        {
            gfx.DrawPolygon(this.Polygon.Poly, D2DColor.White, 1f, D2DDashStyle.Solid, D2DColor.DeepPink);
            //gfx.FillEllipse(new D2DEllipse(this.Position, new D2DSize(3, 3)), D2DColor.LightGray);

        }
    }

    public class LinearMovingTarget : Target
    {
        public LinearMovingTarget() { }

        public LinearMovingTarget(D2DPoint pos) : base(pos)
        {
            this.Polygon = new RenderPoly(GameObjectPoly.RandomPoly(this.NumPolyPoints, this.PolyRadius));
            this.Velocity = new D2DPoint(_rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO), _rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO));
        }

        public LinearMovingTarget(D2DPoint pos, int numPolyPoints, int polyRadius) : base(pos, numPolyPoints, polyRadius)
        {
            this.Polygon = new RenderPoly(GameObjectPoly.RandomPoly(this.NumPolyPoints, this.PolyRadius));
            this.Velocity = new D2DPoint(_rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO), _rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO));
        }

        public override void Render(D2DGraphics gfx)
        {
            gfx.DrawPolygon(this.Polygon.Poly, D2DColor.White, 1f, D2DDashStyle.Solid, D2DColor.White);
            //gfx.FillEllipse(new D2DEllipse(this.Position, new D2DSize(3, 3)), D2DColor.LightGray);

        }
    }


    public class RotatingMovingTarget : Target
    {
        public RotatingMovingTarget() { }
        public RotatingMovingTarget(D2DPoint pos) : base(pos)
        {
            this.Polygon = new RenderPoly(GameObjectPoly.RandomPoly(this.NumPolyPoints, this.PolyRadius));

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
            gfx.DrawPolygon(this.Polygon.Poly, D2DColor.White, 1f, D2DDashStyle.Solid, D2DColor.BlueViolet);
            //gfx.FillEllipse(new D2DEllipse(this.Position, new D2DSize(3, 3)), D2DColor.LightGray);
        }

    }


    public class ErraticMovingTarget : Target
    {
        private int _nextRotMod = 100;
        private int _nextVeloMod = 100;
        private float _targRot = 0;
        private D2DPoint _targVelo = D2DPoint.Zero;

        private SmoothFloat _rotSmooth = new SmoothFloat(300);
        private SmoothPos _veloSmooth = new SmoothPos(300);

        public ErraticMovingTarget() { }
        public ErraticMovingTarget(D2DPoint pos) : base(pos)
        {
            this.Polygon = new RenderPoly(GameObjectPoly.RandomPoly(this.NumPolyPoints, this.PolyRadius));

            this.Velocity = new D2DPoint(_rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO), _rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO));
            this.RotationSpeed = _rnd.NextFloat(-MIN_MAX_ROT, MIN_MAX_ROT);

            _targRot = this.RotationSpeed;
            _targVelo = this.Velocity;
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            base.Update(dt, viewport, renderScale);

            if (currentFrame % _nextRotMod == 0)
            {
                _targRot = _rnd.NextFloat(-MIN_MAX_ROT, MIN_MAX_ROT);
                _nextRotMod = _rnd.Next(500, 1000);
            }

            this.RotationSpeed = _rotSmooth.Add(_targRot);


            if (currentFrame % _nextVeloMod == 0)
            {
                _targVelo = new D2DPoint(_rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO), _rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO));
                _nextVeloMod = _rnd.Next(500, 1000);
            }

            this.Velocity = _veloSmooth.Add(_targVelo);

            var vec = AngleToVector(-this.Rotation);
            this.Velocity = vec * this.Velocity.Length();
        }

        public override void Render(D2DGraphics gfx)
        {
            gfx.DrawPolygon(this.Polygon.Poly, D2DColor.White, 1f, D2DDashStyle.Solid, D2DColor.OrangeRed);
        }

    }
}
