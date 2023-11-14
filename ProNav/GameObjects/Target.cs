using unvell.D2DLib;

namespace ProNav.GameObjects
{
    public abstract class Target : GameObjectPoly
    {
        public int NumPolyPoints { get; set; } = 8;
        public int PolyRadius { get; set; } = 80;

        protected readonly float MIN_MAX_VELO = 250f;//150f;
        protected readonly float MIN_MAX_ROT = 50f; //40f;

        public Target() { }

        public Target(D2DPoint pos) : base(pos) { }

        public Target(D2DPoint pos, int numPolyPoints, int polyRadius) : base(pos)
        {
            this.NumPolyPoints = numPolyPoints;
            this.PolyRadius = polyRadius;
        }

        protected virtual D2DPoint RandOPoint(float minMax)
        {
            return new D2DPoint(RandOFloat(minMax), RandOFloat(minMax));
        }

        protected virtual float RandOFloat(float minMax)
        {
            return RandOFloat(-minMax, minMax);
        }

        protected virtual float RandOFloat(float min, float max)
        {
            return _rnd.NextFloat(min, max);
        }
    }

    public class StaticTarget : Target
    {
        public StaticTarget(D2DPoint pos) : base(pos)
        {
            this.Polygon = new RenderPoly(GameObjectPoly.RandomPoly(this.NumPolyPoints, this.PolyRadius));
            this.RotationSpeed = RandOFloat(MIN_MAX_ROT);
        }

        public override void Render(D2DGraphics gfx)
        {
            gfx.DrawPolygon(this.Polygon.Poly, D2DColor.White, 1f, D2DDashStyle.Solid, D2DColor.DeepPink);
            //gfx.FillEllipse(new D2DEllipse(this.Position, new D2DSize(3, 3)), D2DColor.LightGray);

        }
    }

    public class LinearMovingTarget : Target
    {
        private readonly float _veloFactor = 0.5f;
        public LinearMovingTarget() { }

        public LinearMovingTarget(D2DPoint pos) : base(pos)
        {
            this.Polygon = new RenderPoly(GameObjectPoly.RandomPoly(this.NumPolyPoints, this.PolyRadius));
            this.Velocity = RandOPoint(MIN_MAX_VELO);
        }

        protected override float RandOFloat(float minMax)
        {
            return base.RandOFloat(minMax) * _veloFactor;
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

            this.Velocity = RandOPoint(MIN_MAX_VELO);
            this.RotationSpeed = RandOFloat(MIN_MAX_ROT);
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            base.Update(dt, viewport, renderScale);

            var vec = AngleToVector(this.Rotation);
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

        private float _nextRotTime = 0f;
        private float _nextVeloTime = 0f;

        private float _curRotTime = 0f;
        private float _curVeloTime = 0f;

        private float _prevRotTarg = 0f;
        private D2DPoint _prevVeloTarg = D2DPoint.Zero;

        private float _targRot = 0;
        private D2DPoint _targVelo = D2DPoint.Zero;

        private float _minTime = 5f;
        private float _maxTime = 20f;

        private SmoothFloat _rotSmooth = new SmoothFloat(300);
        private SmoothPos _veloSmooth = new SmoothPos(300);

        public ErraticMovingTarget() { }
        public ErraticMovingTarget(D2DPoint pos) : base(pos)
        {
            this.Polygon = new RenderPoly(GameObjectPoly.RandomPoly(this.NumPolyPoints, this.PolyRadius));
            this.Velocity = RandOPoint(MIN_MAX_VELO);
            this.RotationSpeed = RandOFloat(MIN_MAX_ROT);

            _targRot = this.RotationSpeed;
            _targVelo = this.Velocity;

            _prevRotTarg = this.RotationSpeed;
            _prevVeloTarg = this.Velocity;
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            base.Update(dt, viewport, renderScale);

            if (_curRotTime >= _nextRotTime)
            {
                _targRot = RandOFloat(MIN_MAX_ROT);
                _nextRotTime = RandOFloat(_minTime, _maxTime);
                _curRotTime = 0f;
                _prevRotTarg = this.RotationSpeed;
            }

            this.RotationSpeed = Helpers.Lerp(_prevRotTarg, _targRot, Helpers.Factor(_curRotTime, _nextRotTime));
            _curRotTime += dt;


            if (_curVeloTime >= _nextVeloTime)
            {
                _targVelo = RandOPoint(MIN_MAX_VELO);
                _nextVeloTime = RandOFloat(_minTime, _maxTime);
                _curVeloTime = 0f;
                _prevVeloTarg = this.Velocity;
            }

            this.Velocity = Helpers.LerpPoints(_prevVeloTarg, _targVelo, Helpers.Factor(_curVeloTime, _nextVeloTime));
            _curVeloTime += dt;



            //if (currentFrame % _nextRotMod == 0)
            //{
            //    _targRot = _rnd.NextFloat(-MIN_MAX_ROT, MIN_MAX_ROT);
            //    _nextRotMod = _rnd.Next(500, 1000);
            //}

            //this.RotationSpeed = _rotSmooth.Add(_targRot);


            //if (currentFrame % _nextVeloMod == 0)
            //{
            //    _targVelo = new D2DPoint(_rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO), _rnd.NextFloat(-MIN_MAX_VELO, MIN_MAX_VELO));
            //    _nextVeloMod = _rnd.Next(500, 1000);
            //}

            //this.Velocity = _veloSmooth.Add(_targVelo);

            var vec = AngleToVector(-this.Rotation);
            this.Velocity = vec * this.Velocity.Length();
        }

        public override void Render(D2DGraphics gfx)
        {
            gfx.DrawPolygon(this.Polygon.Poly, D2DColor.White, 1f, D2DDashStyle.Solid, D2DColor.OrangeRed);
        }

    }
}
