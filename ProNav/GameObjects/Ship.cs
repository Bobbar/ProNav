using unvell.D2DLib;

namespace ProNav.GameObjects
{
    public class Ship : GameObjectPoly
    {
        public bool FlameOn = false;

        private const float THRUST = 65f;//10f;

        private D2DColor _fillColor = new D2DColor(0.8f, D2DColor.DarkGray);
        private D2DColor _flameFillColor = new D2DColor(0.8f, D2DColor.Yellow);

        private static readonly D2DPoint[] _shipPoly = new D2DPoint[]
        {
            new D2DPoint(5, 0),
            new D2DPoint(-4, 4),
            new D2DPoint(-2, 0),
            new D2DPoint(-4, -4)
        };

        private static readonly D2DPoint[] _flamePoly = new D2DPoint[]
        {
            new D2DPoint(-2, 0),
            new D2DPoint(-4, 3),
            new D2DPoint(-7, 0),
            new D2DPoint(-4, -3)
        };

        private RenderPoly FlamePoly;
        private float _renderOffset = 2f;
        public Action<Bullet> FireBulletCallback { get; set; }

        public Ship() : base(_shipPoly)
        {
            FlamePoly = new RenderPoly(_flamePoly);
        }

        public Ship(D2DPoint pos) : base(pos, D2DPoint.Zero, _shipPoly)
        {
            FlamePoly = new RenderPoly(_flamePoly);
        }


        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            base.Update(dt, viewport, renderScale + _renderOffset);

            if (FlameOn)
            {
                var rads = this.Rotation * ((float)Math.PI / 180f);
                var vec = new D2DPoint((float)Math.Cos(rads), (float)Math.Sin(rads));
                vec *= (THRUST / vec.Length());
                this.Velocity += dt * vec;
            }

            this.Velocity *= 0.9f;

            FlamePoly.Update(this.Position, Rotation, renderScale + _renderOffset + 1f);

            var velo = this.Velocity.Length();
            _flamePoly[2].X = -_rnd.Next(7 + (int)(velo * 0.2f), 9 + (int)(velo * 0.2f));
            _flameFillColor.g = _rnd.NextFloat(0.6f, 0.86f);
        }


        public override void Render(D2DGraphics gfx)
        {
            if (FlameOn)
            {
                gfx.DrawPolygon(this.FlamePoly.Poly, D2DColor.Transparent, 1f, D2DDashStyle.Solid, _flameFillColor);
            }

            gfx.DrawPolygon(this.Polygon.Poly, D2DColor.White, 1f, D2DDashStyle.Solid, _fillColor);

            var rads = this.Rotation * ((float)Math.PI / 180f);
            var vec = new D2DPoint((float)Math.Cos(rads), (float)Math.Sin(rads));
            gfx.DrawLine(this.Position, this.Position + vec * 200f, new D2DColor(0.2f, D2DColor.LightGreen), 3f, D2DDashStyle.Dot);
        }

        public void FireBullet()
        {
            var bullet = new Bullet(this.Position, this.Rotation);
            FireBulletCallback(bullet);
        }

        public void FireBullet(Target target, Action<D2DPoint> addExplosion)
        {
            var bullet = new TargetedBullet(this.Position, target);
            bullet.AddExplosionCallback = addExplosion;
            FireBulletCallback(bullet);
        }

    }
}
