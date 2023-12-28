namespace ProNav.GameObjects.Guidance
{
    public class QuadraticPNGuidance : GuidanceBase
    {
        private float _prevDir = 0f;

        public QuadraticPNGuidance(Missile missile, GameObjectPoly target) : base(missile, target)
        {
            _prevDir = missile.Rotation;
        }

        public override float GetGuidanceDirection(float dt)
        {
            D2DPoint direction;
            var target = this.Target.CenterOfPolygon();
            var targetRot = this.Missile.Rotation;

            if (GetInterceptDirection(this.Missile.Position, target, this.Missile.Velocity.Length(), this.Target.Velocity, out direction))
            {
                targetRot = direction.Angle(true);
                _prevDir = targetRot;
            }
            else
            {
                targetRot = _prevDir;
                //well, I guess we cant intercept then
            }

            if (float.IsNaN(targetRot))
                targetRot = this.Missile.Rotation;

            return targetRot;
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
            if (missileSpeed == 0f)
            {
                result = D2DPoint.Zero;
                return false;
            }

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
