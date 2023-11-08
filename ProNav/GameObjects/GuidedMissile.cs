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


        private RenderPoly FlamePoly;



        private float BURN_RATE
        {
            get { return (THRUST + _thrustBoost) / 2500f; }

        }

        private float TotalMass
        {
            get { return MASS + FUEL; }
        }

        private D2DPoint _prevTargPos = D2DPoint.Zero;
        private D2DPoint _prevPos = D2DPoint.Zero;
        private D2DPoint _prevImpactPnt = D2DPoint.Zero;
        private D2DPoint _stableAimPoint = D2DPoint.Zero;
        private D2DPoint _finalAimPoint = D2DPoint.Zero;
        private D2DPoint _impactPnt = D2DPoint.Zero;

        private float _prevVelo = 0f;
        private float _prevTargetDist = 0f;
        private double _prevTargVeloAngle = 0f;
        private float _distTraveled = 0f;
        private bool _missedTarget = false;
        private float _reEngageMod = 0f;

        private const float LIFESPAN = 50f;
        private float _age = 0;
        private const float THRUST = 3000f;
        private const float MASS = 25f;
        private float FUEL = 80f;
        private float _thrustBoost = 0;

        private D2DColor _flameFillColor = new D2DColor(0.6f, D2DColor.Yellow);
        private float _renderOffset = 1.5f;

        public GuidanceType Guidance { get; set; } = GuidanceType.Advanced;

        private bool _useControlSurfaces = false;
        private Wing _tailWing;
        private Wing _noseWing;
        private Wing _rocketBody;

        public GuidedMissile(Ship player, Target target, GuidanceType guidance = GuidanceType.Advanced, bool useControlSurfaces = false) : base(player.Position, player.Velocity, player.Rotation)
        {
            this.Guidance = guidance;
            this.Target = target;
            this.Polygon = new RenderPoly(_missilePoly);
            this.FlamePoly = new RenderPoly(_flamePoly);
            this.Rotation = player.Rotation;

            _prevPos = this.Position;
            _prevTargPos = target.Position;
            _useControlSurfaces = useControlSurfaces;

            if (_useControlSurfaces)
            {
                _tailWing = new Wing(4f, 0.2f, player.Rotation, new D2DPoint(-6f, 0));
                _noseWing = new Wing(4f, 0.1f, player.Rotation, new D2DPoint(7f, 0));
                _rocketBody = new Wing(0f, 0.1f, player.Rotation, D2DPoint.Zero);
            }
            else
            {
                _rocketBody = new Wing(4f, 0.4f, player.Rotation, D2DPoint.Zero);
            }
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            _age += dt;

            if (_age > LIFESPAN)
                this.IsExpired = true;

            D2DPoint accel = D2DPoint.Zero;

            accel += GetThrust() * dt / TotalMass;

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
                var noseTorque = GetTorque(_noseWing, noseForce);
                var bodyTorque = GetTorque(_rocketBody, bodyForce);
                var torqueRot = ((tailTorque + noseTorque + bodyTorque)) * dt;
                this.Rotation += torqueRot / TotalMass;
            }
            else
            {
                var bodyForce = LiftDragForce(_rocketBody);
                liftDrag += bodyForce;
            }

            accel += dt * (liftDrag / TotalMass);

            // Apply guidance.
            float guideRotation = this.Rotation;

            switch (Guidance)
            {
                case GuidanceType.Advanced:
                    guideRotation = GuideToAdvanced(dt);
                    break;

                case GuidanceType.BasicLOS:
                    guideRotation = GuideToBasicLOS(dt);
                    break;

                case GuidanceType.SimplePN:
                    guideRotation = GuideToSimplePN(dt);
                    break;

                case GuidanceType.QuadraticPN:
                    guideRotation = GuideToQuadraticPN(dt);
                    break;
            }

            // Guidance can return NaN if our velo is zero.
            if (this.Velocity.Length() == 0f)
                guideRotation = this.Rotation;

            if (_useControlSurfaces)
            {
                const float TAIL_AUTH = 1f;
                const float NOSE_AUTH = 0.5f;

                // Compute deflection.
                // The first two clamps might not be needed here.
                var veloAngle = this.Velocity.Angle(true);
                var nextDeflect = Helpers.ClampAngle(guideRotation);
                nextDeflect -= Helpers.ClampAngle(veloAngle);
                nextDeflect = Helpers.ClampAngle180(nextDeflect);

                _tailWing.Deflection = TAIL_AUTH * -nextDeflect;
                _noseWing.Deflection = NOSE_AUTH * nextDeflect;
            }
            else
            {
                this.Rotation = guideRotation;
            }

            // Integrate acceleration.
            this.Velocity += accel;

            var dist = D2DPoint.Distance(this.Position, _prevPos);
            _prevPos = this.Position;
            _distTraveled += dist;

            if (FUEL > 0f)
                FUEL -= BURN_RATE * dt;

            if (FUEL <= 0f && this.Velocity.Length() <= 70f)
                this.IsExpired = true;

            if (Target.IsExpired)
                this.IsExpired = true;

            // Make the flame do flamey things...(Wiggle and color)
            FlamePoly.SourcePoly[1].X = -_rnd.Next(7 + (int)(this.Velocity.Length() * 0.1f), 9 + (int)(this.Velocity.Length() * 0.1f));
            _flameFillColor.g = _rnd.NextFloat(0.6f, 0.86f);


            base.Update(dt, viewport, renderScale + _renderOffset);

            if (_useControlSurfaces)
            {
                _tailWing.Rotation = this.Rotation + _tailWing.Deflection;
                _noseWing.Rotation = this.Rotation + _noseWing.Deflection;
                _rocketBody.Rotation = this.Rotation;

                _tailWing.Position = ApplyTranslation(_tailWing.ReferencePosition, this.Rotation, this.Position, renderScale + _renderOffset);
                _noseWing.Position = ApplyTranslation(_noseWing.ReferencePosition, this.Rotation, this.Position, renderScale + _renderOffset);
                _rocketBody.Position = ApplyTranslation(_rocketBody.ReferencePosition, this.Rotation, this.Position, renderScale + _renderOffset);

                _tailWing.Update(dt, viewport, renderScale + _renderOffset);
                _noseWing.Update(dt, viewport, renderScale + _renderOffset);
                _rocketBody.Update(dt, viewport, renderScale + _renderOffset);
            }
            else
            {
                _rocketBody.Rotation = this.Rotation;
                _rocketBody.Position = ApplyTranslation(_rocketBody.ReferencePosition, this.Rotation, this.Position, renderScale + _renderOffset);
                _rocketBody.Update(dt, viewport, renderScale + _renderOffset);
            }

            FlamePoly.Update(this.Position, this.Rotation, renderScale + _renderOffset);
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
            if (this.FUEL > 0f)
                gfx.DrawPolygon(this.FlamePoly.Poly, _flameFillColor, 1f, D2DDashStyle.Solid, _flameFillColor);

            gfx.DrawPolygon(this.Polygon.Poly, D2DColor.White, 1f, D2DDashStyle.Solid, D2DColor.White);

            if (_useControlSurfaces)
            {
                _tailWing.Render(gfx);
                _noseWing.Render(gfx);
            }

            _rocketBody.Render(gfx);

            if (World.ShowTracking)
            {
                gfx.FillEllipse(new D2DEllipse(_finalAimPoint, new D2DSize(5f, 5f)), D2DColor.LawnGreen);
                gfx.FillEllipse(new D2DEllipse(_stableAimPoint, new D2DSize(4f, 4f)), D2DColor.Blue);
                gfx.FillEllipse(new D2DEllipse(_impactPnt, new D2DSize(3f, 3f)), D2DColor.Red);
            }
        }

        private D2DPoint LiftDragForce(Wing wing)
        {
            if (wing.Velocity.Length() == 0f)
                return D2DPoint.Zero;

            var veloMag = (this.Velocity + wing.Velocity).Length();
            var veloMagSq = (float)Math.Pow(veloMag, 2f);

            // Compute velo tangent. For lift/drag and rotation calcs.
            var veloNorm = D2DPoint.Normalize(this.Velocity + wing.Velocity);
            var veloNormTan = new D2DPoint(veloNorm.Y, -veloNorm.Cross(new D2DPoint(0, 1))); // Up

            // Compute angle of attack.
            var aoaRads = AngleToVector(wing.Rotation).Cross(veloNorm);
            var aoa = Helpers.RadsToDegrees(aoaRads);

            // Compute lift force as velocity tangent with angle-of-attack effecting magnitude and direction. Velocity magnitude is factored as well.
            // Greater AoA and greater velocity = more lift force.

            // Wing & air parameters.
            const float AOA_FACT = 0.2f; // How much AoA effects drag.
            const float VELO_FACT = 0.2f; // How much velocity effects drag.
            float WING_AREA = wing.Area; // Area of the wing. Effects lift & drag forces.
            const float MAX_LIFT = 20000f;//31000f; // Max lift force allowed.
            const float MAX_AOA = 40f; // Max AoA allowed before lift force reduces. (Stall)
            const float AIR_DENSITY = 1.225f;
            const float PARASITIC_DRAG = 0.5f;

            // Alternate drag formula.  Much more spiky, less tweakable.
            var dragAoa = 1f - (float)Math.Cos(2f * aoaRads);
            var dragForce = dragAoa * AOA_FACT * WING_AREA * 0.5f * AIR_DENSITY * veloMagSq * VELO_FACT;
            dragForce += veloMag * (WING_AREA + PARASITIC_DRAG);
            var dragVec = -veloNorm * dragForce;

            var aoaFact = Helpers.Factor(MAX_AOA, Math.Abs(aoa));
            var coeffLift = (float)Math.Sin(2f * aoaRads) * aoaFact;

            var liftForce = AIR_DENSITY * 0.5f * veloMagSq * WING_AREA * coeffLift;
            liftForce = Math.Clamp(liftForce, -MAX_LIFT, MAX_LIFT);

            var liftVec = veloNormTan * liftForce;

            wing.LiftVector = liftVec;
            wing.DragVector = dragVec;
            wing.AoA = aoa;

            return (liftVec + dragVec);
        }

        private float GetTorque(Wing wing, D2DPoint force)
        {
            // How is it so simple?
            var r = wing.Position - this.Position;
            var torque = Helpers.Cross(r, force);
            return torque;
        }

        private D2DPoint GetThrust()
        {
            // ***  THRUST ***
            // Convert rotation into velo vector.
            D2DPoint thrust = D2DPoint.Zero;

            if (this.FUEL > 0f)
            {
                var vec = AngleToVector(this.Rotation);
                vec *= THRUST + _thrustBoost;
                thrust = vec;
            }

            return thrust;
        }

        private float GuideToBasicLOS(float dt)
        {
            const float pValue = 0.5f;
            const float ARM_DIST = 600f;

            var target = this.Target.CenterOfPolygon();
            var targDist = D2DPoint.Distance(target, this.Position);
            var veloAngle = this.Velocity.Angle(true);

            var navigationTime = targDist / this.Velocity.Length();
            var los = (target + Target.Velocity * navigationTime) - this.Position;

            var angle = this.Velocity.AngleBetween(los, true);
            var adjustment = pValue * angle * D2DPoint.Normalize(los);

            var leadRotation = adjustment.Angle(true);
            var targetRot = leadRotation;

            var armFactor = Helpers.Factor(_distTraveled, ARM_DIST);
            var finalRot = Helpers.LerpAngle(veloAngle, targetRot, armFactor);

            _impactPnt = (target + Target.Velocity * navigationTime);

            if (angle == 0f)
                return veloAngle;

            return finalRot;
        }


        private float GuideToSimplePN(float dt)
        {
            const float pValue = 3f;
            const float ARM_DIST = 600f;

            var target = this.Target.CenterOfPolygon();
            var veloAngle = this.Velocity.Angle(true);

            var los = target - this.Position;
            var navigationTime = los.Length() / this.Velocity.Length();
            var targRelInterceptPos = los + (Target.Velocity * navigationTime);
            _impactPnt = targRelInterceptPos;
            targRelInterceptPos *= pValue;

            var leadRotation = ((target + targRelInterceptPos) - this.Position).Angle(true);
            var targetRot = leadRotation;

            var armFactor = Helpers.Factor(_distTraveled, ARM_DIST);
            var finalRot = Helpers.LerpAngle(veloAngle, targetRot, armFactor);

            return finalRot;
        }

        private float GuideToQuadraticPN(float dt)
        {
            const float ARM_DIST = 600f;
            const float MIN_CLOSE_RATE = 10f; // Min closing rate required to aim at predicted impact point.

            D2DPoint direction;
            var target = this.Target.CenterOfPolygon();
            float target_rotation = this.Rotation;

            if (GetInterceptDirection(this.Position, target, this.Velocity.Length(), this.Target.Velocity, out direction))
            {
                target_rotation = direction.Angle(true);
            }
            else
            {
                //well, I guess we cant intercept then
            }

            var targDist = D2DPoint.Distance(target, this.Position);
            var targetRotation = (target - this.Position).Angle(true);
            var veloAngle = this.Velocity.Angle(true);

            var closingRate = (_prevTargetDist - targDist);
            _prevTargetDist = targDist;
            var closeRateFact = Helpers.Factor(closingRate, MIN_CLOSE_RATE);
            var targetRot = Helpers.LerpAngle(targetRotation, target_rotation, closeRateFact);

            var armFactor = Helpers.Factor(_distTraveled, ARM_DIST);
            var finalRot = Helpers.LerpAngle(veloAngle, targetRot, armFactor);

            return finalRot;
        }

        private int SolveQuadratic(float a, float b, float c, out float root1, out float root2)
        {
            var discriminant = b * b - 4f * a * c;

            if (discriminant < 0)
            {
                root1 = float.PositiveInfinity;
                root2 = -root1;
                return 0;
            }

            root1 = (-b + (float)Math.Sqrt(discriminant)) / (2f * a);
            root2 = (-b - (float)Math.Sqrt(discriminant)) / (2f * a);

            return discriminant > 0f ? 2 : 1;
        }

        private bool GetInterceptDirection(D2DPoint origin, D2DPoint targetPosition, float missileSpeed, D2DPoint targetVelocity, out D2DPoint result)
        {
            var los = origin - targetPosition;
            var distance = los.Length(); ;
            var alpha = Helpers.DegreesToRads(los.AngleBetween(targetVelocity));
            var vt = targetVelocity.Length(); ;
            var vRatio = vt / missileSpeed;

            //solve the triangle, using cossine law
            if (SolveQuadratic(1 - (vRatio * vRatio), 2 * vRatio * distance * (float)Math.Cos(alpha), -distance * distance, out var root1, out var root2) == 0)
            {
                result = D2DPoint.Zero;
                return false;   //no intercept solution possible!
            }

            var interceptVectorMagnitude = Math.Max(root1, root2);
            var time = interceptVectorMagnitude / missileSpeed;
            var estimatedPos = targetPosition + targetVelocity * time;

            _impactPnt = estimatedPos;

            result = D2DPoint.Normalize(estimatedPos - origin);

            return true;
        }

        private float GuideToAdvanced(float dt)
        {
            const float MAX_ROT_RATE = 1.5f; // Max rotation rate.
            const float MIN_ROT_RATE = 1.0f; // Min rotation rate.
            const float MIN_ROT_SPEED = 600f; // Speed at which rotation rate will be the smallest.
            const float ARM_DIST = 600f; // How far we must travel before engaging the target.
            const float MISS_TARG_DIST = 200f; // Distance to target to be considered a miss.
            const float REENGAGE_DIST = 2500f; // How far we must be from the target before re-engaging after a miss.
            const float ROT_MOD_DIST = 1000f; // Distance to begin increasing rotation rate. (Get more aggro the closer we get)
            const float ROT_MOD_AMT = 1f; //1.5f; // Max amount to increase rot rate per above distance.
            const float IMPACT_POINT_DELTA_THRESH = 2f; // Smaller value = target impact point later. (Waits until the point has stabilized more)
            const float MIN_CLOSE_RATE = 1f; // Min closing rate required to aim at predicted impact point.

            var target = this.Target.CenterOfPolygon();
            var targetVelo = this.Target.Velocity * dt;
            var veloMag = this.Velocity.Length();
            var veloAngle = this.Velocity.Angle();

            var deltaV = veloMag - _prevVelo;
            _prevVelo = veloMag;

            if (_prevTargPos == D2DPoint.Zero)
            {
                _prevTargPos = target;
                return veloAngle;
            }
            _prevTargPos = target;

            // Closing rate and number of frames until impact.
            var targDist = D2DPoint.Distance(this.Position, target);
            var closingRate = _prevTargetDist - targDist; // What about using closing rate to predicted impact point instead of target?
            _prevTargetDist = targDist;

            var framesToImpact = (double)targDist / (veloMag * dt);
            var tarVeloAngle = targetVelo.AngleD();
            var targAngleDelta = AngleDiffD(tarVeloAngle, _prevTargVeloAngle);

            // Handle cases where the target rotation wraps around from 360 to 0 degrees and vice versa.
            if (Math.Abs(_prevTargVeloAngle - tarVeloAngle) < 180f)
            {
                targAngleDelta = targAngleDelta * Math.Sign(_prevTargVeloAngle - tarVeloAngle);
            }
            _prevTargVeloAngle = tarVeloAngle;

            // Set initial impact point directly on the target.
            var impactPnt = target;

            // Refine the impact point when able.
            // Where will the target be when we arrive?
            // A mini simulation basically.
            if (_distTraveled > 0)
            {
                impactPnt = RefineImpact(target, targetVelo, targAngleDelta, framesToImpact, dt);
            }

            _impactPnt = impactPnt; // Red

            // Compute the speed (delta) of the impact point as it is refined.
            // Slower sleep = higher confidence.
            var impactPntDelta = D2DPoint.Distance(_prevImpactPnt, impactPnt);
            _prevImpactPnt = impactPnt;

            // Only update the stable aim point when the predicted impact point is moving slowly.
            // If it begins to move quickly (when the target changes velo/direction) we keep targeting the previous point until it slows down again.
            var impactDeltaFact = Helpers.Factor(IMPACT_POINT_DELTA_THRESH, impactPntDelta);
            _stableAimPoint = D2DPoint.Lerp(_stableAimPoint, impactPnt, impactDeltaFact); // Blue

            // Begin targeting the predicted impact point once we have a positive closing rate.
            // Gradually incorporate the direction to the predicted impact point.
            // If we are moving away from the target, we can not rely on the predicted point to be accurate,
            // so just point directly at the target.
            var closeRateFact = Helpers.Factor(closingRate, MIN_CLOSE_RATE);
            var aimDirection = D2DPoint.Normalize(_stableAimPoint - this.Position);
            //var aimDirection = D2DPoint.Lerp(D2DPoint.Normalize(target - this.Position), D2DPoint.Normalize(_stableAimPoint - this.Position), closeRateFact);
            //_finalAimPoint = D2DPoint.Lerp(target, _stableAimPoint, closeRateFact); // Green
            _finalAimPoint = _stableAimPoint;

            // Compute velo norm & tangent.
            var veloNorm = D2DPoint.Normalize(this.Velocity);
            var veloNormTan = new D2DPoint(veloNorm.Y, -veloNorm.Cross(new D2DPoint(0, 1))); // Up

            // Compute two tangential rotations.
            var rotAmtNorm = aimDirection.Cross(veloNorm) * (180f / (float)Math.PI);
            var rotAmtTan = -aimDirection.Cross(veloNormTan) * (180f / (float)Math.PI);

            // Blend between the two rotations as angle diff changes.
            var targetDirAngle = aimDirection.Angle();
            var targetAngleDiff = AngleDiff(veloAngle, targetDirAngle);

            //var angDiffFact = Helpers.Factor(Math.Abs(targetAngleDiff), 180f); // Favors the tangent.
            var angDiffFact = Helpers.Factor(Math.Abs(targetAngleDiff), 360f); // Favors the normal.
            var rotLerp = Helpers.Lerp(rotAmtNorm, rotAmtTan, angDiffFact);

            // Reduce rotation rate as velocity increases. Helps conserve inertia and reduce drag.
            var veloFact = Helpers.Factor(veloMag, MIN_ROT_SPEED);
            var rotFact = Math.Clamp((MAX_ROT_RATE * (1f - veloFact)) + MIN_ROT_RATE, MIN_ROT_RATE, MAX_ROT_RATE);

            // Increase rotation rate modifier as we approach the target.
            var rotMod = (1f - Helpers.Factor(targDist, ROT_MOD_DIST)) * ROT_MOD_AMT;
            rotFact += rotMod;

            // Increase rotation authority as we approach the arm distance.
            var rotAuthority = Helpers.Factor(_distTraveled, ARM_DIST);

            // Detect when we miss the target.
            if (closingRate < MIN_CLOSE_RATE)
            {
                if (!_missedTarget && targDist < MISS_TARG_DIST)
                {
                    _missedTarget = true;

                    //this.IsExpired = true;

                    // Increase re-engage dist slightly with each miss.
                    _reEngageMod += REENGAGE_DIST * 0.5f;
                }
            }
            else
            {
                _missedTarget = false;
            }

            // Reduce rotation authority if we missed until we are the specified distance away from the target.
            // This helps give us room to turn around and make another attempt.
            if (_missedTarget && targDist < REENGAGE_DIST + _reEngageMod)
                rotAuthority = Helpers.Factor(targDist, REENGAGE_DIST + _reEngageMod);

            // Offset our current rotation from our current velocity vector to compute the next rotation.
            var nextRot = -(rotLerp * rotFact);
            return veloAngle + (nextRot * rotAuthority);
        }


        private D2DPoint RefineImpact(D2DPoint targetPos, D2DPoint targetVelo, double targAngleDelta, double framesToImpact, float dt)
        {
            D2DPoint predicted = targetPos;
            const int MAX_FTI = 6000;

            if (framesToImpact >= 1 && framesToImpact < MAX_FTI)
            {
                var targLoc = targetPos;
                var angle = targetVelo.AngleD();

                for (int i = 0; i <= framesToImpact; i++)
                {
                    var avec = AngleToVectorD(angle) * targetVelo.Length();
                    targLoc += avec;
                    angle += -targAngleDelta;
                    angle = ClampAngleD(angle);
                }

                var rem = framesToImpact % (int)framesToImpact;
                angle += -targAngleDelta * rem;
                angle = ClampAngleD(angle);
                targLoc += (AngleToVectorD(angle) * targetVelo.Length()) * (float)rem;

                predicted = targLoc;
            }

            return predicted;
        }

    }
}
