namespace ProNav.GameObjects.Guidance
{
    public class BasicLOSGuidance : GuidanceBase
    {
        public BasicLOSGuidance(Missile missile, GameObjectPoly target) : base(missile, target)
        { }

        public override float GetGuidanceDirection(float dt)
        {
            const float pValue = 20f;

            var target = this.Target.CenterOfPolygon();
            var targDist = D2DPoint.Distance(target, this.Missile.Position);
            var veloAngle = this.Missile.Velocity.Angle(true);

            var navigationTime = targDist / this.Missile.Velocity.Length();
            var los = (target + Target.Velocity * navigationTime) - this.Missile.Position;

            var angle = this.Missile.Velocity.AngleBetween(los, true);
            var adjustment = pValue * angle * D2DPoint.Normalize(los);

            var leadRotation = adjustment.Angle(true);
            var targetRot = leadRotation;

            ImpactPoint = (target + Target.Velocity * navigationTime);

            if (angle == 0f)
                return veloAngle;

            return targetRot;
        }
    }
}
