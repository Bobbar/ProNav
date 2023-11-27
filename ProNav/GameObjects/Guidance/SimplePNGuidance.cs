namespace ProNav.GameObjects.Guidance
{
    public class SimplePNGuidance : IGuidance
    {
        public D2DPoint ImpactPoint { get; set; }
        public D2DPoint StableAimPoint { get; set; }
        public D2DPoint CurrentAimPoint { get; set; }
        public Missile Missile { get; set; }
        public Target Target { get; set; }

        public SimplePNGuidance(Missile missile, Target target)
        {
            Missile = missile;
            Target = target;
        }

        public float GuideTo(float dt)
        {
            const float pValue = 3f;
            const float ARM_DIST = 600f;

            var target = this.Target.CenterOfPolygon();
            var veloAngle = this.Missile.Velocity.Angle(true);

            var los = target - this.Missile.Position;
            var navigationTime = los.Length() / this.Missile.Velocity.Length();
            var targRelInterceptPos = los + (Target.Velocity * navigationTime);
            ImpactPoint = targRelInterceptPos;
            targRelInterceptPos *= pValue;

            var leadRotation = ((target + targRelInterceptPos) - this.Missile.Position).Angle(true);
            var targetRot = leadRotation;

            var armFactor = Helpers.Factor(Missile.DistTraveled, ARM_DIST);
            var finalRot = Helpers.LerpAngle(veloAngle, targetRot, armFactor);

            return finalRot;
        }
    }
}
