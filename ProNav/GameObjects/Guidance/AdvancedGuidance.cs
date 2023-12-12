namespace ProNav.GameObjects.Guidance
{


    public class AdvancedGuidance : GuidanceBase
    {
        private D2DPoint _prevTargPos = D2DPoint.Zero;
        private D2DPoint _prevImpactPnt = D2DPoint.Zero;

        private float _prevVelo = 0f;
        private float _prevTargetDist = 0f;
        private float _prevTargVeloAngle = 0f;

        public AdvancedGuidance(Missile missile, Target target) : base(missile, target)
        { }

        public override float GetGuidanceDirection(float dt)
        {
            // Tweakables
            const float MAX_ROT_RATE = 1.5f; // Max rotation rate.
            const float MIN_ROT_RATE = 1.0f; // Min rotation rate.
            const float MIN_ROT_SPEED = 600f; // Speed at which rotation rate will be the smallest.
            const float ROT_MOD_DIST = 1000f; // Distance to begin increasing rotation rate. (Get more aggro the closer we get)
            const float ROT_MOD_AMT = 1f; // Max amount to increase rot rate per above distance.
            const float IMPACT_POINT_DELTA_THRESH = 2f; // Smaller value = target impact point later. (Waits until the point has stabilized more)
            const float MIN_CLOSE_RATE = 0.3f; // Min closing rate required to aim at predicted impact point.

            var target = this.Target.CenterOfPolygon();
            var targetVelo = this.Target.Velocity * dt;
            var veloMag = this.Missile.Velocity.Length();
            var veloAngle = this.Missile.Velocity.Angle();

            var deltaV = veloMag - _prevVelo;
            _prevVelo = veloMag;

            if (_prevTargPos == D2DPoint.Zero)
            {
                _prevTargPos = target;
                return veloAngle;
            }
            _prevTargPos = target;

            var targDist = D2DPoint.Distance(this.Missile.Position, target);

            // Set initial impact point directly on the target.
            var impactPnt = target;

            // Refine the impact point when able.
            // Where will the target be when we arrive?
            if (Missile.DistTraveled > 0)
            {
                var tarVeloAngle = targetVelo.Angle();
                var targAngleDelta = tarVeloAngle - _prevTargVeloAngle;
                _prevTargVeloAngle = tarVeloAngle;

                var timeToImpact = ImpactTime(targDist, (veloMag * dt), (deltaV * dt));
                impactPnt = RefineImpact(target, targetVelo, targAngleDelta, timeToImpact, dt);
            }

            ImpactPoint = impactPnt; // Red

            // Compute the speed (delta) of the impact point as it is refined.
            // Slower sleep = higher confidence.
            var impactPntDelta = D2DPoint.Distance(_prevImpactPnt, impactPnt);
            _prevImpactPnt = impactPnt;

            // Only update the stable aim point when the predicted impact point is moving slowly.
            // If it begins to move quickly (when the target changes velo/direction) we keep targeting the previous point until it slows down again.
            var impactDeltaFact = Helpers.Factor(IMPACT_POINT_DELTA_THRESH, impactPntDelta);
            StableAimPoint = D2DPoint.Lerp(StableAimPoint, impactPnt, impactDeltaFact); // Blue

            // Compute closing rate and lerp between the target and predicted locations.
            // We gradually incorporate the predicted location as closing rate increases.
            var closingRate = _prevTargetDist - targDist;
            _prevTargetDist = targDist;
            var closeRateFact = Helpers.Factor(closingRate, MIN_CLOSE_RATE);
            var aimDirection = D2DPoint.Lerp(D2DPoint.Normalize(target - this.Missile.Position), D2DPoint.Normalize(StableAimPoint - this.Missile.Position), closeRateFact);
            CurrentAimPoint = D2DPoint.Lerp(target, StableAimPoint, closeRateFact); // Green

            // Compute velo norm, tangent & rotations.
            var veloNorm = D2DPoint.Normalize(this.Missile.Velocity);
            var veloNormTan = new D2DPoint(veloNorm.Y, -veloNorm.X);

            var rotAmtNorm = Helpers.RadsToDegrees(aimDirection.Cross(veloNorm));
            var rotAmtTan = -1f * Helpers.RadsToDegrees(aimDirection.Cross(veloNormTan));

            // Lerp between the two rotations as angle diff changes.
            var targetDirAngle = aimDirection.Angle();
            var targetAngleDiff = Helpers.AngleDiff(veloAngle, targetDirAngle);

            //var angDiffFact = Helpers.Factor(Math.Abs(targetAngleDiff), 180f); // Favors the tangent.
            var angDiffFact = Helpers.Factor(Math.Abs(targetAngleDiff), 360f); // Favors the normal.
            var rotLerp = Helpers.Lerp(rotAmtNorm, rotAmtTan, angDiffFact);

            // Reduce rotation rate as velocity increases. Helps conserve inertia and reduce drag.
            var veloFact = Helpers.Factor(veloMag, MIN_ROT_SPEED);
            var rotFact = Math.Clamp((MAX_ROT_RATE * (1f - veloFact)) + MIN_ROT_RATE, MIN_ROT_RATE, MAX_ROT_RATE);

            // Increase rotation rate modifier as we approach the target.
            var rotMod = (1f - Helpers.Factor(targDist, ROT_MOD_DIST)) * ROT_MOD_AMT;
            rotFact += rotMod;

            // Offset our current rotation from our current velocity vector to compute the next rotation.
            var nextRot = -(rotLerp * rotFact);
            return veloAngle + nextRot;
        }

        private float ImpactTime(float dist, float velo, float accel)
        {
            var finalVelo = (float)Math.Sqrt((Math.Pow(velo, 2f) + 2f * accel * dist));

            return (finalVelo - velo) / accel;
        }

        private D2DPoint RefineImpact(D2DPoint targetPos, D2DPoint targetVelo, float targAngleDelta, float timeToImpact, float dt)
        {
            // To obtain a high order target position we basically run a small simulation here.
            // This considers the target velocity as well as the change in angular velocity.

            D2DPoint predicted = targetPos;
            const int MAX_FTI = 6000; // Max iterations allowed.

            if (timeToImpact >= 1 && timeToImpact < MAX_FTI)
            {
                var targLoc = targetPos;
                var angle = targetVelo.Angle();

                // Advance the target position and velocity angle.
                for (int i = 0; i <= timeToImpact; i++)
                {
                    var avec = Helpers.AngleToVectorDegrees(angle) * targetVelo.Length();
                    targLoc += avec;
                    angle += targAngleDelta;
                    angle = Helpers.ClampAngle(angle);
                }

                // Include the remainder after the loop.
                var rem = timeToImpact % (int)timeToImpact;
                angle += targAngleDelta * rem;
                angle = Helpers.ClampAngle(angle);
                targLoc += (Helpers.AngleToVectorDegrees(angle) * targetVelo.Length()) * rem;

                predicted = targLoc;
            }

            return predicted;
        }
    }
}
