namespace ProNav.GameObjects.Guidance
{
    public abstract class GuidanceBase
    {
        public D2DPoint ImpactPoint { get; set; }
        public D2DPoint StableAimPoint { get; set; }
        public D2DPoint CurrentAimPoint { get; set; }

        protected Missile Missile { get; set; }
        protected Target Target { get; set; }

        private bool _missedTarget = false;
        private float _prevTargDist = 0f;

        const float MISS_TARG_DIST = 300f; // Distance to be considered a miss when the closing rate goes negative.
        const float REENGAGE_DIST = 2500f; // How far we must be from the target before re-engaging after a miss.
        const float ARM_DIST = 600f;

        protected GuidanceBase(Missile missile, Target target)
        {
            Missile = missile;
            Target = target;
        }

        public float GuideTo(float dt)
        {
            var rotFactor = 1f;
            var veloAngle = Missile.Velocity.Angle();

            // Get rotation from implementation.
            var rotation = GetGuidanceDirection(dt);

            // Compute closing rate and detect when we miss the target.
            var targDist = D2DPoint.Distance(Missile.Position, Target.Position);
            var closingRate = _prevTargDist - targDist;
            _prevTargDist = targDist;

            if (closingRate < 0f)
                if (!_missedTarget && targDist < MISS_TARG_DIST)
                    _missedTarget = true;
                else
                    _missedTarget = false;

            // Reduce the rotation amount to fly a straighter course until
            // we are the specified distance away from the target.
            if (_missedTarget && targDist < REENGAGE_DIST)
                rotFactor = Helpers.Factor(targDist, REENGAGE_DIST);

            // Lerp from current rotation towards guidance rotation as we 
            // approach the specified arm distance.
            var armFactor = Helpers.Factor(Missile.DistTraveled, ARM_DIST);
            var finalRot = Helpers.LerpAngle(veloAngle, rotation, armFactor);

            return finalRot * rotFactor;
        }

        /// <summary>
        /// Implement guidance, dummy!
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public abstract float GetGuidanceDirection(float dt);
    }
}
