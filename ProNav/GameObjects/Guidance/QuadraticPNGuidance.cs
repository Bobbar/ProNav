namespace ProNav.GameObjects.Guidance
{
    public class QuadraticPNGuidance : IGuidance
    {
        public D2DPoint ImpactPoint { get; set; }
        public D2DPoint StableAimPoint { get; set; }
        public D2DPoint CurrentAimPoint { get; set; }
        public Missile Missile { get; set; }
        public Target Target { get; set; }

        private float _prevTargetDist = 0f;

        public QuadraticPNGuidance(Missile missile, Target target)
        {
            Missile = missile;
            Target = target;
        }

        public float GuideTo(float dt)
        {
            const float ARM_DIST = 600f;
            const float MIN_CLOSE_RATE = 10f; // Min closing rate required to aim at predicted impact point.

            D2DPoint direction;
            var target = this.Target.CenterOfPolygon();
            float target_rotation = this.Missile.Rotation;

            if (GetInterceptDirection(this.Missile.Position, target, this.Missile.Velocity.Length(), this.Target.Velocity, out direction))
            {
                target_rotation = direction.Angle(true);
            }
            else
            {
                //well, I guess we cant intercept then
            }

            var targDist = D2DPoint.Distance(target, this.Missile.Position);
            var targetRotation = (target - this.Missile.Position).Angle(true);
            var veloAngle = this.Missile.Velocity.Angle(true);

            var closingRate = (_prevTargetDist - targDist);
            _prevTargetDist = targDist;
            var closeRateFact = Helpers.Factor(closingRate, MIN_CLOSE_RATE);
            var targetRot = Helpers.LerpAngle(targetRotation, target_rotation, closeRateFact);

            var armFactor = Helpers.Factor(Missile.DistTraveled, ARM_DIST);
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
            var distance = los.Length();
            var alpha = Helpers.DegreesToRads(los.AngleBetween(targetVelocity));
            var vt = targetVelocity.Length();
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

            ImpactPoint = estimatedPos;

            result = D2DPoint.Normalize(estimatedPos - origin);

            return true;
        }
    }
}
