namespace ProNav.GameObjects.Guidance
{
    public class SimplePNGuidance : GuidanceBase
    {
        public SimplePNGuidance(Missile missile, GameObjectPoly target) : base(missile, target)
        { }

        public override float GetGuidanceDirection(float dt)
        {
            const float pValue = 2f;

            var target = this.Target.CenterOfPolygon();
            var los = target - this.Missile.Position;
            var navigationTime = los.Length() / (this.Missile.Velocity.Length() * dt);
            var targRelInterceptPos = los + ((Target.Velocity * dt) * navigationTime);

            ImpactPoint = targRelInterceptPos;
            targRelInterceptPos *= pValue;

            var leadRotation = ((target + targRelInterceptPos) - this.Missile.Position).Angle(true);
            var targetRot = leadRotation;

            return targetRot;
        }
    }
}
