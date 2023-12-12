﻿using ProNav.GameObjects.Guidance;
using System.Diagnostics;
using unvell.D2DLib;

namespace ProNav.GameObjects
{
    public class GuidedMissile : Missile
    {
        public Target Target { get; set; }

        private static readonly D2DPoint[] _missilePoly = new D2DPoint[]
        {
            new D2DPoint(14, 0),
            new D2DPoint(11, 2),
            new D2DPoint(-6, 2),
            new D2DPoint(-8, 4),
            new D2DPoint(-8, -4),
            new D2DPoint(-6, -2),
            new D2DPoint(11, -2)
        };

        private static readonly D2DPoint[] _flamePoly = new D2DPoint[]
        {
            new D2DPoint(-8, 2),
            new D2DPoint(-10, 0),
            new D2DPoint(-8, -2),
        };

        private float BURN_RATE
        {
            get { return THRUST / BURN_RATE_DIVISOR; }
        }

        private float TotalMass
        {
            get { return MASS + _currentFuel; }

        }

        private readonly float THURST_VECTOR_AMT = 2f;
        private readonly float LIFESPAN = 50f;
        private readonly float BURN_RATE_DIVISOR = 2500f;
        private readonly float THRUST = 4000f;
        private readonly float BOOST_THRUST = 8000f;

        private readonly float MASS = 45.3f;
        private readonly float FUEL = 40f;
        private readonly float BOOST_FUEL = 10f;

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

        public GuidedMissile(Ship player, Target target, GuidanceType guidance = GuidanceType.Advanced, bool useControlSurfaces = false, bool useThrustVectoring = false) : base(player.Position, player.Velocity, player.Rotation)
        {
            _currentFuel = FUEL;
            _currentBoostFuel = BOOST_FUEL;
            _centerOfThrust = new FixturePoint(this, new D2DPoint(-11, 0));

            this.GuidanceType = guidance;
            this.Target = target;
            this.Polygon = new RenderPoly(_missilePoly, new D2DPoint(-2f, 0f));
            this.FlamePoly = new RenderPoly(_flamePoly, new D2DPoint(-2.3f, 0));
            this.Rotation = player.Rotation;

            _useControlSurfaces = useControlSurfaces;
            _useThrustVectoring = useThrustVectoring;

            if (_useControlSurfaces)
            {
                _tailWing = new Wing(this, 4f, 0.2f, 30f, new D2DPoint(-8.5f, 0));
                _rocketBody = new Wing(this, 0f, 0.1f, D2DPoint.Zero);
                _noseWing = new Wing(this, 4f, 0.1f, 30f, new D2DPoint(8f, 0));
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
            _age += dt;

            if (_age > LIFESPAN)
                this.IsExpired = true;

            D2DPoint accel = D2DPoint.Zero;

            // Apply aerodynamics.
            var liftDrag = D2DPoint.Zero;

            if (_useControlSurfaces)
            {
                var tailForce = LiftDragForce(_tailWing);
                var noseForce = LiftDragForce(_noseWing);
                var bodyForce = LiftDragForce(_rocketBody);
                liftDrag += tailForce + noseForce + bodyForce;

                // Compute torque and rotation result.
                var tailTorque = GetTorque(_tailWing, tailForce);
                var bodyTorque = GetTorque(_rocketBody, bodyForce);
                var noseTorque = GetTorque(_noseWing, noseForce);
                var thrustTorque = GetTorque(_centerOfThrust.Position, GetThrust(thrustVector: _useThrustVectoring));
                var torqueRot = (tailTorque + bodyTorque + noseTorque + thrustTorque) * dt;
                
                var inertia = this.TotalMass;

                if (World.UseAlternateInertia)
                    inertia = GetInertia(this.Polygon, this.TotalMass);

                this.Rotation += torqueRot / inertia;
            }
            else
            {
                var bodyForce = LiftDragForce(_rocketBody);
                liftDrag += bodyForce;
            }

            // Apply guidance.
            var guideRotation = _guidance.GuideTo(dt);

            if (_useControlSurfaces)
            {
                const float TAIL_AUTH = 1f;
                const float NOSE_AUTH = 0.3f;

                // Compute deflection.
                var veloAngle = this.Velocity.Angle(true);
                var nextDeflect = Helpers.ClampAngle180(guideRotation - veloAngle);

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

            if (FUEL <= 0f && this.Velocity.Length() <= 30f)
                this.IsExpired = true;

            if (Target.IsExpired)
                this.IsExpired = true;

            // Make the flame do flamey things...(Wiggle and color)
            var thrust = GetThrust().Length();
            var len = this.Velocity.Length() * 0.05f;
            len += thrust * 0.01f;
            len *= 0.5f;
            FlamePoly.SourcePoly[1].X = -_rnd.NextFloat(9f + len, 11f + len);
            _flameFillColor.g = _rnd.NextFloat(0.6f, 0.86f);

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

            const float DEF_AMT = 0.2f; // How much the flame will be deflected in relation to velocity.
            var angle = this.Rotation - (Helpers.ClampAngle180(this.Rotation - this.Velocity.Angle(true)) * DEF_AMT);
            FlamePoly.Update(this.Position, angle, renderScale + _renderOffset);
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
            //gfx.DrawPolygon(this.Polygon.Poly, D2DColor.White, 1f, D2DDashStyle.Solid, D2DColor.White);

            DrawFuelGauge(gfx);

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
            //_centerOfThrust.Render(gfx);
        }

        private void DrawFuelGauge(D2DGraphics gfx)
        {
            const float HEIGHT = 6f;
            const float WIDTH = 2f;

            var pos = new D2DPoint(5.5f, HEIGHT * 0.5f);
            var offsetPos = this.ApplyTranslation(pos, this.Rotation, this.Position);

            // Background
            var vec1 = Helpers.AngleToVectorDegrees(this.Rotation - 90f) * HEIGHT;
            gfx.DrawLine(offsetPos, offsetPos + vec1, D2DColor.DarkGray, WIDTH);

            // Gauge
            var vec2 = Helpers.AngleToVectorDegrees(this.Rotation - 90f) * (HEIGHT * (_currentFuel / FUEL));
            gfx.DrawLine(offsetPos, offsetPos + vec2, _currentBoostFuel > 0f ? D2DColor.Red : D2DColor.DarkRed, WIDTH);
        }

        private D2DPoint LiftDragForce(Wing wing)
        {
            if (wing.Velocity.Length() == 0f)
                return D2DPoint.Zero;

            var velo = -World.Wind;

            if (World.UseAlternateInertia)
                velo += (wing.Velocity / (World.DT / World.PHYSICS_STEPS));
            else
                velo += this.Velocity + wing.Velocity;

            var veloMag = velo.Length();
            var veloMagSq = (float)Math.Pow(veloMag, 2f);

            // Compute velo tangent. For lift/drag and rotation calcs.
            var veloNorm = D2DPoint.Normalize(velo);
            var veloNormTan = new D2DPoint(veloNorm.Y, -veloNorm.X);

            // Compute angle of attack.
            var aoaRads = AngleToVector(wing.Rotation).Cross(veloNorm);
            var aoa = Helpers.RadsToDegrees(aoaRads);

            // Compute lift force as velocity tangent with angle-of-attack effecting magnitude and direction. Velocity magnitude is factored as well.
            // Greater AoA and greater velocity = more lift force.

            // Wing & air parameters.
            const float AOA_FACT = 0.2f; // How much AoA effects drag.
            const float VELO_FACT = 0.4f; // How much velocity effects drag.
            float WING_AREA = wing.Area; // Area of the wing. Effects lift & drag forces.
            const float MAX_LIFT = 20000f; // Max lift force allowed.
            const float MAX_AOA = 40f; // Max AoA allowed before lift force reduces. (Stall)
            float AIR_DENSITY = World.AirDensity;
            const float PARASITIC_DRAG = 0.5f;

            // Drag force.
            var dragAoa = 1f - (float)Math.Cos(2f * aoaRads);
            var dragForce = dragAoa * AOA_FACT * WING_AREA * 0.5f * AIR_DENSITY * veloMagSq * VELO_FACT;
            dragForce += veloMag * (WING_AREA * PARASITIC_DRAG);

            // Lift force.
            var aoaFact = Helpers.Factor(MAX_AOA, Math.Abs(aoa));
            var coeffLift = (float)Math.Sin(2f * aoaRads) * aoaFact;
            var liftForce = AIR_DENSITY * 0.5f * veloMagSq * WING_AREA * coeffLift;
            liftForce = Math.Clamp(liftForce, -MAX_LIFT, MAX_LIFT);

            var dragVec = -veloNorm * dragForce;
            var liftVec = veloNormTan * liftForce;

            wing.LiftVector = liftVec;
            wing.DragVector = dragVec;
            wing.AoA = aoa;

            return (liftVec + dragVec);
        }

        private float GetTorque(Wing wing, D2DPoint force)
        {
            return GetTorque(wing.Position, force);
        }

        private float GetTorque(D2DPoint pos, D2DPoint force)
        {
            // How is it so simple?
            var r = pos - this.Position;
            var torque = Helpers.Cross(r, force);
            return torque;
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

                // Ease out boost thrust.
                vec *= Helpers.Lerp(THRUST, BOOST_THRUST, _currentBoostFuel, BOOST_FUEL);

                thrust = vec;
            }

            return thrust;
        }
    }
}
