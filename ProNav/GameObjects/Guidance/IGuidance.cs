using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProNav.GameObjects.Guidance
{
    public interface IGuidance
    {
        D2DPoint ImpactPoint { get; set; }
        D2DPoint StableAimPoint { get; set; }
        D2DPoint CurrentAimPoint { get; set; }
        GuidedMissile Missile { get; set; }
        Target Target { get; set; }

        float GuideTo(float dt);
    }
}
