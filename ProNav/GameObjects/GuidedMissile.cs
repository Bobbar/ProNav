using ProNav.GameObjects.Guidance;
using System.Diagnostics;
using unvell.D2DLib;

namespace ProNav.GameObjects
{
    public class GuidedMissile : Missile
    {
        public GameObjectPoly Target { get; set; }

        private static readonly D2DPoint[] _missilePoly = new D2DPoint[]
        {
            new D2DPoint(28, 0),
            new D2DPoint(25, 2),
            new D2DPoint(-20, 2),
            new D2DPoint(-22, 4),
            new D2DPoint(-22, -4),
            new D2DPoint(-20, -2),
            new D2DPoint(25, -2)
        };


        private static readonly D2DPoint[] _flamePoly = new D2DPoint[]
        {
            new D2DPoint(-8, 2),
            new D2DPoint(-10, 0),
            new D2DPoint(-8, -2),
        };

        private float TotalMass
        {
            get { return MASS + _currentFuel; }

        }

        private readonly float THURST_VECTOR_AMT = 1f;
        private readonly float LIFESPAN = 70f;
        private readonly float BURN_RATE = 1.7f;
        private readonly float THRUST = 4000f;
        private readonly float MASS = 45.3f;
        private readonly float FUEL = 40f;
        private readonly float BOOST_FUEL = 20f;

        private float _age = 0;
        private float _currentFuel = 0f;
        private float _currentBoostFuel = 0f;

        private RenderPoly FlamePoly;
        private D2DColor _flameFillColor = new D2DColor(0.6f, D2DColor.Yellow);
        private float _renderOffset = 1.5f;

        private GuidanceType GuidanceType = GuidanceType.Advanced;
        private GuidanceBase _guidance;

        private bool _useControlSurfaces = false;
        private bool _useThrustVectoring = false;
        private Wing _tailWing;
        private Wing _noseWing;
        private Wing _rocketBody;
        private FixturePoint _centerOfThrust;
        private FixturePoint _warheadCenterMass;
        private FixturePoint _motorCenterMass;
        private FixturePoint _flamePos;

        public GuidedMissile(Ship player, GameObjectPoly target, GuidanceType guidance = GuidanceType.Advanced, bool useControlSurfaces = false, bool useThrustVectoring = false) : base(player.Position, player.Velocity, player.Rotation)
        {
            _currentFuel = FUEL;
            _currentBoostFuel = BOOST_FUEL;

            _centerOfThrust = new FixturePoint(this, new D2DPoint(-22, 0));
            _warheadCenterMass = new FixturePoint(this, new D2DPoint(4f, 0));
            _motorCenterMass = new FixturePoint(this, new D2DPoint(-11f, 0));
            _flamePos = new FixturePoint(this, new D2DPoint(-22f, 0));

            this.GuidanceType = guidance;
            this.Target = target;
            this.Polygon = new RenderPoly(_missilePoly, new D2DPoint(-2f, 0f));
            this.FlamePoly = new RenderPoly(_flamePoly, new D2DPoint(6f, 0));
            this.Rotation = player.Rotation;

            _useControlSurfaces = useControlSurfaces;
            _useThrustVectoring = useThrustVectoring;

            if (_useControlSurfaces)
            {
                _tailWing = new Wing(this, 4f, 0.2f, 40f, 7000f, new D2DPoint(-22f, 0));
                _rocketBody = new Wing(this, 0f, 0.15f, 4000f, D2DPoint.Zero);
                _noseWing = new Wing(this, 4f, 0.05f, 20f, 5000f, new D2DPoint(19.5f, 0));
            }
            else
            {
                _rocketBody = new Wing(this, 4f, 0.4f, D2DPoint.Zero);
            }

            switch (GuidanceType)
            {
                case GuidanceType.Advanced:
                    _guidance = new AdvancedGuidance(this, target);
                    break;

                case GuidanceType.BasicLOS:
                    _guidance = new BasicLOSGuidance(this, target);
                    break;

                case GuidanceType.SimplePN:
                    _guidance = new SimplePNGuidance(this, target);
                    break;

                case GuidanceType.QuadraticPN:
                    _guidance = new QuadraticPNGuidance(this, target);
                    break;
            }
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            if (_age == 0f)
                base.Update(dt, viewport, renderScale + _renderOffset);

            _age += dt;

            if (_age > LIFESPAN)
                this.IsExpired = true;

            D2DPoint accel = D2DPoint.Zero;

            // Apply aerodynamics.
            var liftDrag = D2DPoint.Zero;

            if (_useControlSurfaces)
            {
                var tailForce = _tailWing.GetLiftDragForce();
                var noseForce = _noseWing.GetLiftDragForce();
                var bodyForce = _rocketBody.GetLiftDragForce();
                liftDrag += tailForce + noseForce + bodyForce;

                // Compute torque and rotation result.
                var tailTorque = GetTorque(_tailWing, tailForce);
                var bodyTorque = GetTorque(_rocketBody, bodyForce);
                var noseTorque = GetTorque(_noseWing, noseForce);
                var thrustTorque = GetTorque(_centerOfThrust.Position, GetThrust(thrustVector: _useThrustVectoring));
                var torqueRot = (tailTorque + bodyTorque + noseTorque + thrustTorque) * dt;

                this.RotationSpeed += torqueRot / this.TotalMass;
            }
            else
            {
                var bodyForce = _rocketBody.GetLiftDragForce();
                liftDrag += bodyForce;
            }

            // Apply guidance.
            var guideRotation = _guidance.GuideTo(dt);

            if (_useControlSurfaces)
            {
                const float TAIL_AUTH = 1f;
                const float NOSE_AUTH = 0f;

                // Compute deflection.
                var veloAngle = this.Velocity.Angle(true);
                var nextDeflect = Helpers.ClampAngle180(guideRotation - veloAngle);

                // Adjust the deflection as speed, rotation speed and AoA increases.
                // This is to try to prevent over-rotation caused by thrust vectoring.
                if (_currentFuel > 0f && _useThrustVectoring)
                {
                    const float MIN_DEF_SPD = 600f; // Minimum speed required for full deflection.
                    var spdFact = Helpers.Factor(this.Velocity.Length(), MIN_DEF_SPD);
                    nextDeflect *= spdFact;

                    const float MAX_DEF_AOA = 110f; // Maximum AoA allowed. Reduce deflection as AoA increases.
                    var aoaFact = 1f - Helpers.Factor(Math.Abs(_rocketBody.AoA), MAX_DEF_AOA);
                    nextDeflect *= aoaFact;

                    const float MAX_DEF_ROT_SPD = 310f; // Maximum rotation speed allowed. Reduce deflection to try to control rotation speed.
                    var rotSpdFact = 1f - Helpers.Factor(Math.Abs(this.RotationSpeed), MAX_DEF_ROT_SPD);
                    nextDeflect *= rotSpdFact;
                }

                _tailWing.Deflection = TAIL_AUTH * -nextDeflect;
                _noseWing.Deflection = NOSE_AUTH * nextDeflect;
            }
            else
            {
                this.Rotation = guideRotation;
            }

            // Add thrust and integrate acceleration.
            accel += GetThrust(thrustVector: false) * dt / TotalMass;
            accel += dt * (liftDrag / TotalMass);

            this.Velocity += accel;

            if (_currentFuel > 0f)
            {
                _currentFuel -= BURN_RATE * dt;
                _currentBoostFuel -= BURN_RATE * dt;
            }

            base.Update(dt, viewport, renderScale + _renderOffset);

            if (_useControlSurfaces)
            {
                _tailWing.Update(dt, viewport, renderScale + _renderOffset);
                _noseWing.Update(dt, viewport, renderScale + _renderOffset);
                _rocketBody.Update(dt, viewport, renderScale + _renderOffset);
            }
            else
            {
                _rocketBody.Update(dt, viewport, renderScale + _renderOffset);
            }

            _centerOfThrust.Update(dt, viewport, renderScale + _renderOffset);
            _warheadCenterMass.Update(dt, viewport, renderScale + _renderOffset);
            _motorCenterMass.Update(dt, viewport, renderScale + _renderOffset);
            _flamePos.Update(dt, viewport, renderScale + _renderOffset);

            float flameAngle = 0f;

            if (_useThrustVectoring)
            {
                flameAngle = GetThrust(_useThrustVectoring).Angle();
            }
            else
            {
                const float DEF_AMT = 0.2f; // How much the flame will be deflected in relation to velocity.
                flameAngle = this.Rotation - (Helpers.ClampAngle180(this.Rotation - this.Velocity.Angle(true)) * DEF_AMT);
            }

            // Make the flame do flamey things...(Wiggle and color)
            var thrust = GetThrust().Length();
            var len = this.Velocity.Length() * 0.05f;
            len += thrust * 0.02f;
            len *= 0.5f;
            FlamePoly.SourcePoly[1].X = -_rnd.NextFloat(9f + len, 11f + len);
            _flameFillColor.g = _rnd.NextFloat(0.6f, 0.86f);

            FlamePoly.Update(_flamePos.Position, flameAngle, renderScale + _renderOffset);

            if (FUEL <= 0f && this.Velocity.Length() <= 5f)
                this.IsExpired = true;

            if (Target.IsExpired)
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
            //gfx.DrawLine(this.Position, this.Position + (AngleToVector(this.Rotation) * 50f), D2DColor.White);
            //gfx.DrawLine(this.Position, this.Position + (this.Velocity * 1f), D2DColor.Blue);

            if (_currentFuel > 0f)
                gfx.DrawPolygon(this.FlamePoly.Poly, _flameFillColor, 1f, D2DDashStyle.Solid, _flameFillColor);

            var fillColor = D2DColor.White;

            switch (GuidanceType)
            {
                case GuidanceType.Advanced:
                    fillColor = D2DColor.White;
                    break;

                case GuidanceType.BasicLOS:
                    fillColor = D2DColor.SkyBlue;
                    break;

                case GuidanceType.SimplePN:
                    fillColor = D2DColor.Orange;
                    break;

                case GuidanceType.QuadraticPN:
                    fillColor = D2DColor.PaleVioletRed;
                    break;
            }

            gfx.DrawPolygon(this.Polygon.Poly, D2DColor.White, 0.5f, D2DDashStyle.Solid, fillColor);

            //DrawFuelGauge(gfx);

            if (_useControlSurfaces)
            {
                _tailWing.Render(gfx);
                _noseWing.Render(gfx);

                //var totLift = _tailWing.LiftVector + _noseWing.LiftVector + _rocketBody.LiftVector;
                //gfx.DrawLine(this.Position, this.Position + (totLift * 1.4f), D2DColor.SkyBlue, 0.5f, D2DDashStyle.Solid, D2DCapStyle.Flat, D2DCapStyle.Triangle);
            }

            _rocketBody.Render(gfx);

            if (World.ShowTracking)
            {
                gfx.FillEllipse(new D2DEllipse(_guidance.CurrentAimPoint, new D2DSize(5f, 5f)), D2DColor.LawnGreen);
                gfx.FillEllipse(new D2DEllipse(_guidance.StableAimPoint, new D2DSize(4f, 4f)), D2DColor.Blue);
                gfx.FillEllipse(new D2DEllipse(_guidance.ImpactPoint, new D2DSize(3f, 3f)), D2DColor.Red);
            }

            //// Center of mass and center of lift.
            //gfx.FillEllipse(new D2DEllipse(this.Position, new D2DSize(1f, 1f)), D2DColor.Orange);
            //gfx.FillEllipse(new D2DEllipse((_tailWing.Position + _noseWing.Position) / 2f, new D2DSize(1f, 1f)), D2DColor.CornflowerBlue);
            //gfx.FillEllipse(new D2DEllipse(this.CenterOfPolygon(), new D2DSize(1f, 1f)), D2DColor.Green);
            //gfx.FillEllipse(this.CenterOfPolygon(), 2f, D2DColor.LightGreen);

            //_centerOfThrust.Render(gfx);
            //_warheadCM.Render(gfx);
            //_motorCM.Render(gfx);
            //_flamePos.Render(gfx);

            //var sumCm = (MASS * _warheadCM.Position + _currentFuel * _motorCM.Position) / (MASS + _currentFuel);
            //gfx.FillEllipse(new D2DEllipse(sumCm, new D2DSize(1f, 1f)), D2DColor.Orange);

            //gfx.DrawText(Math.Round(this.Velocity.Length(), 1).ToString(), D2DColor.Black, "Consolas", 1f, new D2DRect(this.Position, new D2DSize(4f, 4f)));
        }

        private void DrawFuelGauge(D2DGraphics gfx)
        {
            const float HEIGHT = 6f;
            const float WIDTH = 2f;

            var pos = new D2DPoint(5.5f, HEIGHT * 0.5f);
            var offsetPos = this.ApplyTranslation(pos, this.Rotation, this.Position);
            var angleVec = Helpers.AngleToVectorDegrees(this.Rotation - 90f);

            // Background
            var vec1 = angleVec * HEIGHT;
            gfx.DrawLine(offsetPos, offsetPos + vec1, D2DColor.DarkGray, WIDTH);

            // Gauge
            var vec2 = angleVec * (HEIGHT * (_currentFuel / FUEL));
            gfx.DrawLine(offsetPos, offsetPos + vec2, _currentBoostFuel > 0f ? D2DColor.Red : D2DColor.DarkRed, WIDTH);
        }

        private float GetTorque(Wing wing, D2DPoint force)
        {
            return GetTorque(wing.Position, force);
        }

        private float GetTorque(D2DPoint pos, D2DPoint force)
        {
            // How is it so simple?
            var r = pos - GetCenterOfGravity();

            var torque = Helpers.Cross(r, force);
            return torque;
        }

        private D2DPoint GetCenterOfGravity()
        {
            var cm = (MASS * _warheadCenterMass.Position + _currentFuel * _motorCenterMass.Position) / (MASS + _currentFuel);
            return cm;
        }

        private D2DPoint GetThrust(bool thrustVector = false)
        {
            var thrust = D2DPoint.Zero;

            if (_currentFuel > 0f)
            {
                D2DPoint vec;

                if (thrustVector)
                    vec = AngleToVector(this.Rotation + (_tailWing.Deflection * THURST_VECTOR_AMT));
                else
                    vec = AngleToVector(this.Rotation);

                vec *= THRUST;

                thrust = vec;
            }

            return thrust;
        }
    }
}
