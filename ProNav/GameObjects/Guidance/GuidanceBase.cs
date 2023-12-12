﻿namespace ProNav.GameObjects.Guidance
{
    public abstract class GuidanceBase
    {
        public D2DPoint ImpactPoint { get; set; }
        public D2DPoint StableAimPoint { get; set; }
        public D2DPoint CurrentAimPoint { get; set; }

        protected Missile Missile { get; set; }
        protected Target Target { get; set; }

        public bool _missedTarget = false;
        private float _prevTargDist = 0f;
        private float _reEngageMod = 0f;
        private float _missDistTraveled = 0f;

        private readonly float MISS_TARG_DIST = 500f; // Distance to be considered a miss when the closing rate goes negative.
        private readonly float REENGAGE_DIST = 1500f; // How far we must be from the target before re-engaging after a miss.
        private readonly float ARM_DIST = 600f;

        protected GuidanceBase(Missile missile, Target target)
        {
            Missile = missile;
            Target = target;
        }

        public float GuideTo(float dt)
        {
            // The guidance logic doesn't work when velo is zero.
            // Always return the current rotation if we aren't moving yet.
            if (Missile.Velocity.Length() == 0f)
                return Missile.Rotation;

            var rotFactor = 1f;
            var veloAngle = Missile.Velocity.Angle();

            // Get rotation from implementation.
            var rotation = GetGuidanceDirection(dt);

            // Compute closing rate and detect when we miss the target.
            var targDist = D2DPoint.Distance(Missile.Position, Target.Position);
            var closingRate = _prevTargDist - targDist;
            _prevTargDist = targDist;

            if (closingRate < 0.1f)
            {
                if (!_missedTarget && targDist < MISS_TARG_DIST)
                {
                    _missedTarget = true;
                    _missDistTraveled = Missile.DistTraveled;
                    _reEngageMod += REENGAGE_DIST * 0.5f;
                }
            }
               
            // Reduce the rotation amount to fly a straighter course until
            // we are the specified distance away from the target.
            var missDist = Missile.DistTraveled - _missDistTraveled;

            if (_missedTarget)
            {
                var reengageDist = REENGAGE_DIST + _reEngageMod;

                if (missDist < reengageDist / 2f)
                    rotFactor = 1f - Helpers.Factor(missDist, reengageDist / 2f);
                else
                    rotFactor = Helpers.Factor(missDist / 2f, reengageDist);
            }

            if (_missedTarget && missDist >= REENGAGE_DIST + _reEngageMod)
                _missedTarget = false;

            // Lerp from current rotation towards guidance rotation as we 
            // approach the specified arm distance.
            var armFactor = Helpers.Factor(Missile.DistTraveled, ARM_DIST);

            var finalRot = Helpers.LerpAngle(veloAngle, rotation, rotFactor * armFactor);

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
