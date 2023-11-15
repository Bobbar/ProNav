using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProNav.GameObjects.Guidance
{
    public class BasicLOSGuidance : IGuidance
    {
        public D2DPoint ImpactPoint { get; set; }
        public D2DPoint StableAimPoint { get; set; }
        public D2DPoint CurrentAimPoint { get; set; }
        public GuidedMissile Missile { get; set; }
        public Target Target { get; set; }

        public BasicLOSGuidance(GuidedMissile missile, Target target)
        {
            Missile = missile;
            Target = target;
        }

        public float GuideTo(float dt)
        {
            const float pValue = 0.5f;
            const float ARM_DIST = 600f;

            var target = this.Target.CenterOfPolygon();
            var targDist = D2DPoint.Distance(target, this.Missile.Position);
            var veloAngle = this.Missile.Velocity.Angle(true);

            var navigationTime = targDist / this.Missile.Velocity.Length();
            var los = (target + Target.Velocity * navigationTime) - this.Missile.Position;

            var angle = this.Missile.Velocity.AngleBetween(los, true);
            var adjustment = pValue * angle * D2DPoint.Normalize(los);

            var leadRotation = adjustment.Angle(true);
            var targetRot = leadRotation;

            var armFactor = Helpers.Factor(Missile.DistTraveled, ARM_DIST);
            var finalRot = Helpers.LerpAngle(veloAngle, targetRot, armFactor);

            ImpactPoint = (target + Target.Velocity * navigationTime);

            if (angle == 0f)
                return veloAngle;

            return finalRot;
        }
    }
}
