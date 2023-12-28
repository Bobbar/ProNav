using System.Diagnostics;

namespace ProNav.GameObjects.Guidance
{
    public abstract class GuidanceBase
    {
        public D2DPoint ImpactPoint { get; set; }
        public D2DPoint StableAimPoint { get; set; }
        public D2DPoint CurrentAimPoint { get; set; }

        protected Missile Missile { get; set; }
        protected GameObjectPoly Target { get; set; }


        public bool _missedTarget = false;
        private float _prevTargDist = 0f;
        private float _reEngageMod = 0f;
        private float _missDistTraveled = 0f;
        private float _missDirection = 0f; // O.o

        private readonly float MISS_TARG_DIST = 500f; // Distance to be considered a miss when the closing rate goes negative.
        private readonly float REENGAGE_DIST = 1000f;//1500f; // How far we must be from the target before re-engaging after a miss.
        private readonly float ARM_DIST = 1200f;//600f;

        protected GuidanceBase(Missile missile, GameObjectPoly target)
        {
            Missile = missile;
            Target = target;
        }

        public float GuideTo(float dt)
        {
            // The guidance logic doesn't work when velo is zero (or very close).
            // Always return the current rotation if we aren't moving yet.
            if (Missile.Velocity.Length() == 0f)
                return Missile.Rotation;

            var rotFactor = 1f;
            var veloAngle = Missile.Velocity.Angle();
            var initialAngle = veloAngle;

            // Get rotation from implementation.
            var rotation = GetGuidanceDirection(dt);

            if (float.IsNaN(rotation))
                Debugger.Break();

            // Compute closing rate and detect when we miss the target.
            var targDist = D2DPoint.Distance(Missile.Position, Target.Position);
            var closingRate = _prevTargDist - targDist;
            _prevTargDist = targDist;

            if (closingRate < 0.1f)
            {
                if (!_missedTarget && targDist < MISS_TARG_DIST)
                {
                    if (World.ExpireMissilesOnMiss)
                        this.Missile.IsExpired = true;

                    _missedTarget = true;
                    _missDistTraveled = Missile.DistTraveled;
                    _reEngageMod += REENGAGE_DIST * 0.5f;
                    _missDirection = Missile.Rotation;
                }
            }

            // Reduce the rotation amount to fly a straighter course until
            // we are the specified distance away from the target.
            var missDist = Missile.DistTraveled - _missDistTraveled;

            if (_missedTarget)
            {
                var reengageDist = REENGAGE_DIST + _reEngageMod;
                rotFactor = Helpers.Factor(missDist, reengageDist);
                initialAngle = _missDirection;
            }

            if (_missedTarget && missDist >= REENGAGE_DIST + _reEngageMod)
                _missedTarget = false;

            // Lerp from current rotation towards guidance rotation as we 
            // approach the specified arm distance.
            var armFactor = Helpers.Factor(Missile.DistTraveled, ARM_DIST);

            var finalRot = Helpers.LerpAngle(initialAngle, rotation, rotFactor * armFactor);

            return finalRot;
        }

        /// <summary>
        /// Implement guidance, dummy!
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public abstract float GetGuidanceDirection(float dt);
    }
}
