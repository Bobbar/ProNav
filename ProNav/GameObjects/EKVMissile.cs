using ProNav.GameObjects.Guidance;
using unvell.D2DLib;

namespace ProNav.GameObjects
{
    /// <summary>
    /// Inspired by https://en.wikipedia.org/wiki/Exoatmospheric_Kill_Vehicle
    /// </summary>
    public class EKVMissile : Missile
    {
        public Target Target { get; set; }

        private static readonly D2DPoint[] _missilePoly = new D2DPoint[]
        {
            new D2DPoint(-10, -10),
            new D2DPoint(10, -10),
            new D2DPoint(10, 10),
            new D2DPoint(-10, 10),

        };

        private static readonly D2DPoint[] _flamePoly = new D2DPoint[]
        {
            new D2DPoint(-8, 2),
            new D2DPoint(-10, 0),
            new D2DPoint(-8, -2),
        };

        private RenderPoly FlamePoly;
        private GuidanceBase _guidance;

        private readonly float THRUST = 3000f;
        private readonly float MASS = 35f;
        private readonly int NUM_THRUSTERS = 4;
        private float _thrustAngle = 0;

        private D2DColor _flameFillColor = new D2DColor(0.6f, D2DColor.Yellow);
        private D2DColor _fillColor = new D2DColor(0.4f, D2DColor.Gray);

        private float _renderOffset = 0.8f;//1f;

        public EKVMissile(Ship player, Target target) : base(player.Position, player.Velocity, 0f)
        {
            this.Target = target;
            this.Polygon = new RenderPoly(_missilePoly);
            this.FlamePoly = new RenderPoly(_flamePoly, new D2DPoint(-4f, 0));

            // This only seems to work with the advanced guidance.
            _guidance = new AdvancedGuidance(this, target);
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            base.Update(dt, viewport, renderScale + _renderOffset);

            var accel = D2DPoint.Zero;

            var guideAngle = _guidance.GuideTo(dt);
            var aimDir = Helpers.AngleToVectorDegrees(guideAngle);
            var rotAmt = aimDir.Cross(this.Velocity);
            guideAngle -= rotAmt;

            if (this.Velocity.Length() == 0f)
                _thrustAngle = (this.Target.Position - this.Position).Angle();
            else
                _thrustAngle = guideAngle;

            var thrustAmts = ComputeThrustAmounts(_thrustAngle, NUM_THRUSTERS);

            float ang = 0f;
            foreach (var amt in thrustAmts)
            {
                accel += Helpers.AngleToVectorDegrees(this.Rotation + ang) * THRUST * amt;
                ang += (360f / NUM_THRUSTERS);
            }

            this.Velocity += dt * accel / MASS;

            _flameFillColor.g = _rnd.NextFloat(0.6f, 0.86f);

            //this.Rotation += 60f * dt;

            if (this.Target.IsExpired)
                this.IsExpired = true;
        }

        public override void Wrap(D2DSize viewport)
        {
            var padding = 2000f;

            if (this.Position.X < -padding)
                IsExpired = true;

            if (this.Position.X > viewport.width + padding)
                IsExpired = true;

            if (this.Position.Y < -padding)
                IsExpired = true;

            if (this.Position.Y > viewport.height + padding)
                IsExpired = true;
        }

        public override void Render(D2DGraphics gfx)
        {
            var thrustAmts = ComputeThrustAmounts(_thrustAngle, NUM_THRUSTERS);

            const float MAX_LEN = 50f;
            float ang = 0f;
            foreach (var amt in thrustAmts)
            {
                if (amt > 0f)
                {
                    var vec = Helpers.AngleToVectorDegrees(ang) * MAX_LEN * amt;
                    FlamePoly.SourcePoly[1].X = -(15f + MAX_LEN * amt);
                    FlamePoly.Update(this.Position, this.Rotation + ang, 1f);
                    gfx.DrawPolygon(this.FlamePoly.Poly, _flameFillColor, 1f, D2DDashStyle.Solid, _flameFillColor);
                }

                ang += (360f / NUM_THRUSTERS);
            }

            gfx.DrawPolygon(this.Polygon.Poly, D2DColor.White, 0.5f, D2DDashStyle.Solid, _fillColor);

            if (World.ShowTracking)
            {
                gfx.FillEllipse(new D2DEllipse(_guidance.CurrentAimPoint, new D2DSize(5f, 5f)), D2DColor.LawnGreen);
                gfx.FillEllipse(new D2DEllipse(_guidance.StableAimPoint, new D2DSize(4f, 4f)), D2DColor.Blue);
                gfx.FillEllipse(new D2DEllipse(_guidance.ImpactPoint, new D2DSize(3f, 3f)), D2DColor.Red);
            }
        }

        /// <summary>
        /// Percentage of thrust to assign to each thruster.
        /// </summary>
        /// <param name="direction">Thrust direction</param>
        /// <param name="nThrusters">Number of thrusters</param>
        /// <returns>An array containing the thrust percentage for each thruster given the specified direction.</returns>
        private float[] ComputeThrustAmounts(float direction, int nThrusters)
        {
            var amts = new float[nThrusters];
            var angle = 0f;
            var max = 360f / nThrusters;

            for (int i = 0; i < nThrusters; i++)
            {
                var diff = Helpers.AngleDiff(direction, this.Rotation + angle);
                var fact = 1f - Helpers.Factor(diff, max);
                amts[i] = fact;
                angle += max;
            }

            return amts;
        }
    }
}
