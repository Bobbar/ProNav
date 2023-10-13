using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unvell.D2DLib;

namespace ProNav.GameObjects
{
    public class GuidedMissile : Missile
    {
        //public Target Target { get; set; }
        public Target Target { get; set; }


        private static readonly D2DPoint[] _missilePoly = new D2DPoint[]
        {
            new D2DPoint(9, 0),
            new D2DPoint(6, 2),
            new D2DPoint(-6, 2),
            new D2DPoint(-8, 4),
            new D2DPoint(-8, -4),
            new D2DPoint(-6, -2),
            new D2DPoint(6, -2)

        };

        private static readonly D2DPoint[] _flamePoly = new D2DPoint[]
        {
            new D2DPoint(-8, 2),
            new D2DPoint(-10, 0),
            new D2DPoint(-8, -2),
        };


        private RenderPoly FlamePoly;



        private float LIFESPAN = 70f;//40f;
        private float _age = 0;
        private float THRUST = 6000f;//4000f;
        private float MASS = 35.3f;
        private float FUEL = 50f;
        private float _thrustBoost = 0;

        private float BURN_RATE
        {
            //get { return THRUST / 1000f; }

            get { return (THRUST + _thrustBoost) / 2500f; }

        }

        private float TotalMass
        {
            get { return MASS + FUEL; }
        }




        private D2DPoint _prevTargPos = D2DPoint.Zero;
        private D2DPoint _prevPos = D2DPoint.Zero;
        private D2DPoint _prevImpactPnt = D2DPoint.Zero;
        private D2DPoint _stableAimPoint = D2DPoint.Zero;
        private D2DPoint _finalAimPoint = D2DPoint.Zero;
        private D2DPoint _impactPnt = D2DPoint.Zero;
        private D2DPoint _liftVector = D2DPoint.Zero;
        private D2DPoint _dragVector = D2DPoint.Zero;

        private D2DPoint _lastMissedLoc = D2DPoint.Zero;
        private D2DPoint _missLoc = D2DPoint.Zero;


        private float _prevVelo = 0f;
        private float _prevTargetDist = 0f;
        private double _prevTargVeloAngle = 0f;
        private float _distTraveled = 0f;
        private bool _missedTarget = false;
        private float _reEngageMod = 0f;

        //private SmoothFloat _impactDeltaSmooth = new SmoothFloat(2);
        //private SmoothFloat _rotAmtSmooth = new SmoothFloat(2);
        //private SmoothFloat _angleDiffSmooth = new SmoothFloat(10);
        //private SmoothDouble _targAngleDeltaSmooth = new SmoothDouble(4);
        //private SmoothFloat _dvSmooth = new SmoothFloat(5);
        //private SmoothDouble _ftiSmooth = new SmoothDouble(1);
        //private SmoothFloat _closingRateSmooth = new SmoothFloat(5);
        //private SmoothPos _impactPntSmooth = new SmoothPos(1);
        //private SmoothPos _dirSmooth = new SmoothPos(2);
        //private SmoothPos _aimPointSmooth = new SmoothPos(1);
        //private SmoothPos _targetVeloSmooth = new SmoothPos(2);

        private SmoothFloat _impactDeltaSmooth = new SmoothFloat(1);
        private SmoothFloat _rotAmtSmooth = new SmoothFloat(1);
        private SmoothFloat _angleDiffSmooth = new SmoothFloat(5);
        private SmoothDouble _targAngleDeltaSmooth = new SmoothDouble(7);
        private SmoothFloat _dvSmooth = new SmoothFloat(1);
        private SmoothDouble _ftiSmooth = new SmoothDouble(1);
        private SmoothFloat _closingRateSmooth = new SmoothFloat(5);
        private SmoothPos _impactPntSmooth = new SmoothPos(1);
        private SmoothPos _dirSmooth = new SmoothPos(5);
        private SmoothPos _aimPointSmooth = new SmoothPos(5);
        private SmoothPos _targetVeloSmooth = new SmoothPos(5);


        private D2DColor _fLiftCol = new D2DColor(0.9f, D2DColor.Blue);
        private D2DColor _fDragCol = new D2DColor(0.9f, D2DColor.Blue);
        private D2DColor _nfLiftCol = new D2DColor(0.6f, D2DColor.GreenYellow);
        private D2DColor _nfDragCol = new D2DColor(0.6f, D2DColor.Yellow);
        private D2DColor _flameFillColor = new D2DColor(0.6f, D2DColor.Yellow);

        public GuidedMissile(Ship player, Target target) : base(player.Position, player.Velocity, player.Rotation)
        {
            this.Target = target;
            this.Polygon = new RenderPoly(_missilePoly);
            this.FlamePoly = new RenderPoly(_flamePoly);

            _prevPos = this.Position;
            _prevTargPos = target.Position;

        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            base.Update(dt, viewport, renderScale + 2.5f);

            _age += dt;

            if (_age > LIFESPAN)
                this.IsExpired = true;

            // ***  THRUST ***
            // Convert rotation into velo vector and integrate.
            var rads = this.Rotation * ((float)Math.PI / 180f);
            var vec = new D2DPoint((float)Math.Cos(rads), (float)Math.Sin(rads));
            vec *= THRUST + _thrustBoost;

            if (this.FUEL > 0f)
                this.Velocity += dt * (vec / TotalMass);

            var target = this.Target.CenterOfPolygon();
            var veloMag = this.Velocity.Length();
            var veloMagSq = (float)Math.Pow(veloMag, 2f);

            // Compute velo tangent. For lift/drag and rotation calcs.
            var veloNorm = D2DPoint.Normalize(this.Velocity);
            var veloNormTan = new D2DPoint(veloNorm.Y, -Cross(veloNorm, new D2DPoint(0, 1))); // Up

            // Compute angle of attack.
            var aoaRads = Cross(AngleToVector(this.Rotation), veloNorm);
            var aoa = aoaRads * (180f / (float)Math.PI);

            var rot = GuideTo(target, dt);
            this.Rotation = rot;

            // Compute lift force as velocity tangent with angle-of-attack effecting magnitude and direction. Velocity magnitude is factored as well.
            // Greater AoA and greater velocity = more lift force.

            // Wing & air parameters.
            const float AOA_FACT = 0.2f;//0.12f;//0.11f;
            const float VELO_FACT = 0.2f;//0.9f;
            const float WING_AREA = 0.2f;//0.4f;//0.09f;//0.11f;//0.09f;//0.09f;//0.11f;
            const float MAX_LIFT = 31000f;//36000f; // Max lift force allowed. (Approx 60 Gs)
            const float MAX_AOA = 40f;//40f; // Max AoA allowed before lift force reduces. (Stall)
            const float AIR_DENSITY = 1.225f;
            const float PARASITIC_DRAG = 0.5f;//1.9f; // Lift/Velo indi

            // Alternate drag formula.  Much more spiky, less tweakable.
            var dragAoa = 1f - (float)Math.Cos(2f * aoaRads);
            var dragForce = dragAoa * AOA_FACT * WING_AREA * 0.5f * AIR_DENSITY * veloMagSq * VELO_FACT;
            dragForce += veloMag * (WING_AREA + PARASITIC_DRAG);
            var dragVec = -veloNorm * dragForce;

            var aoaFact = Helpers.Factor(MAX_AOA, Math.Abs(aoa));
            var coeffLift = (float)Math.Sin(2f * aoaRads) * aoaFact;

            //Alternate lift formula.
            //var c = (float)Math.Pow(AIR_DENSITY * veloMag, 2f);
            //float coeffLift = 0.01f;
            //if (c > 0f)
            //	coeffLift = (4f * (aoa * aoaFact)) / (float)Math.Sqrt(c);

            var liftForce = AIR_DENSITY * 0.5f * (float)Math.Pow(veloMag, 2f) * WING_AREA * coeffLift;
            liftForce = Math.Clamp(liftForce, -MAX_LIFT, MAX_LIFT);


            // *** LIFT/DRAG ***
            var liftVec = veloNormTan * liftForce;
            this.Velocity += dt * ((liftVec + dragVec) / TotalMass);

            var dist = D2DPoint.Distance(this.Position, _prevPos);
            _prevPos = this.Position;
            _distTraveled += dist;

            if (FUEL > 0f)
                FUEL -= BURN_RATE * dt;

            if (FUEL <= 0f && veloMag <= 70f)
                this.IsExpired = true;

            if (Target.IsDestroyed)
                this.IsExpired = true;

            _liftVector = liftVec;
            _dragVector = dragVec;

            // Make the flame do flamey things...(Wiggle and color)
            FlamePoly.SourcePoly[1].X = -_rnd.Next(7 + (int)(veloMag * 0.1f), 9 + (int)(veloMag * 0.1f));
            _flameFillColor.g = _rnd.NextFloat(0.6f, 0.86f);

            FlamePoly.Update(this.Position, this.Rotation, renderScale + 2.5f);
        }

        public override void Wrap(D2DSize viewport)
        {
            var padding = 2000f;

            if (this.Position.X < -padding)
                IsExpired = true;

            if (this.Position.X > viewport.width + padding)
                IsExpired = true;

            if (this.Position.Y < -padding)
                IsExpired = true;

            if (this.Position.Y > viewport.height + padding)
                IsExpired = true;
        }

        public override void Render(D2DGraphics gfx)
        {
            gfx.DrawPolygon(this.Polygon.Poly, D2DColor.White, 1f, D2DDashStyle.Solid, D2DColor.White);


            if (this.FUEL > 0f)
                gfx.DrawPolygon(this.FlamePoly.Poly, _flameFillColor, 1f, D2DDashStyle.Solid, _flameFillColor);

            gfx.DrawLine(this.Position, this.Position + (_liftVector * 0.05f), D2DColor.SkyBlue);
            gfx.DrawLine(this.Position, this.Position + (_dragVector * 0.05f), D2DColor.Red);


            gfx.FillEllipse(new D2DEllipse(_finalAimPoint, new D2DSize(8f, 8f)), D2DColor.LawnGreen);
            gfx.FillEllipse(new D2DEllipse(_stableAimPoint, new D2DSize(6f, 6f)), D2DColor.Blue);
            gfx.FillEllipse(new D2DEllipse(_impactPnt, new D2DSize(4f, 4f)), D2DColor.Red);
        }


        private float GuideTo(D2DPoint target, float dt)
        {
            var targetVelo = target - _prevTargPos;
            targetVelo = _targetVeloSmooth.Add(targetVelo);

            var veloMag = this.Velocity.Length();
            var veloAngle = VecAngle(this.Velocity);

            var deltaV = veloMag - _prevVelo;
            deltaV = _dvSmooth.Add(deltaV);
            _prevVelo = veloMag;

            if (_prevTargPos == D2DPoint.Zero)
            {
                _prevTargPos = target;
                return veloAngle;
            }
            _prevTargPos = target;

            // Closing rate and number of frames until impact.
            var targDist = D2DPoint.Distance(this.Position, target);
            var closingRate = _prevTargetDist - targDist; // What about using closing rate to predicted impact point instead of target?
            closingRate = _closingRateSmooth.Add(closingRate);
            _prevTargetDist = targDist;

            var framesToImpact = TimeOfArrival(veloMag, deltaV, targDist, dt);
            //var framesToImpact = (double)targDist / (veloMag * dt);
            framesToImpact = _ftiSmooth.Add(framesToImpact);
            var tarVeloAngle = VecAngleD(D2DPoint.Normalize(targetVelo));
            var targAngleDelta = AngleDiffD(tarVeloAngle, _prevTargVeloAngle);

            // Handle cases where the target rotation wraps around from 360 to 0 degrees and vice versa.
            if (Math.Abs(_prevTargVeloAngle - tarVeloAngle) < 180f)
            {
                targAngleDelta = targAngleDelta * Math.Sign(_prevTargVeloAngle - tarVeloAngle);
                _targAngleDeltaSmooth.Add(targAngleDelta);
            }

            _prevTargVeloAngle = tarVeloAngle;

            // Set initial impact point directly on the target.
            var impactPnt = target;

            // Refine the impact point when able.
            // Where will the target be when we arrive?
            // A mini simulation basically.
            if (_distTraveled > 0)
            {
                impactPnt = RefineImpact(target, targetVelo, _targAngleDeltaSmooth.Current, framesToImpact, dt);
                impactPnt = _impactPntSmooth.Add(impactPnt);
            }

            _impactPnt = impactPnt; // Red

            // Compute the speed (delta) of the impact point as it is refined.
            // Slower sleep = higher confidence.
            var impactPntDelta = D2DPoint.Distance(_prevImpactPnt, impactPnt);
            _impactDeltaSmooth.Add(impactPntDelta);
            _prevImpactPnt = impactPnt;


            const float IMPACT_POINT_DELTA_THRESH = 2f;//3f; // Smaller value = target impact point later. (Waits until the point has stablized more)
            const float MIN_CLOSE_RATE = 0.3f;//0.5f; // Min closing rate required to aim at predicted impact point.

            // Only update the stable aim point when the predicted impact point is moving slowly.
            // If it begins to move quickly (when the target changes velo/direction) we keep targeting the previous point until it slows down again.
            if (_distTraveled == 0f)
                _stableAimPoint = impactPnt;

            _stableAimPoint = D2DPoint.Lerp(_stableAimPoint, impactPnt, Helpers.Factor(IMPACT_POINT_DELTA_THRESH, _impactDeltaSmooth.Current));
            _stableAimPoint = _aimPointSmooth.Add(_stableAimPoint); // Blue

            // Begin targeting the predicted impact point once we have a positive closing rate.
            // Gradually incorporate the direction to the predicted impact point.
            // If we are moving away from the target, we can not rely on the predicted point to be accurate,
            // so just point directly at the target.
            var closeRateFact = Helpers.Factor(closingRate, MIN_CLOSE_RATE);
            var aimDirection = D2DPoint.Lerp(D2DPoint.Normalize(target - this.Position), D2DPoint.Normalize(_stableAimPoint - this.Position), closeRateFact);
            aimDirection = _dirSmooth.Add(aimDirection);
            _finalAimPoint = D2DPoint.Lerp(target, _stableAimPoint, closeRateFact); // Green

            // Compute velo norm & tangent.
            var veloNorm = D2DPoint.Normalize(this.Velocity);
            var veloNormTan = new D2DPoint(veloNorm.Y, -Cross(veloNorm, new D2DPoint(0, 1))); // Up

            // Compute two tangental rotations.
            var rotAmtNorm = Cross(aimDirection, veloNorm) * (180f / (float)Math.PI);
            var rotAmtTan = -Cross(aimDirection, veloNormTan) * (180f / (float)Math.PI);

            // Blend between the two rotations as angle diff changes.
            var targetDirAngle = VecAngle(aimDirection);
            _angleDiffSmooth.Add(AngleDiff(veloAngle, targetDirAngle));


            float angDiffFact;

            if (closingRate < MIN_CLOSE_RATE)
                angDiffFact = Helpers.Factor(Math.Abs(_angleDiffSmooth.Current), 180f);
            else
                angDiffFact = Helpers.Factor(Math.Abs(_angleDiffSmooth.Current), 360f);

            //var angDiffFact = Helpers.Factor(Math.Abs(_angleDiffSmooth.Current), 180f); // Favors the tangent.
            ////var angDiffFact = Helpers.Factor(Math.Abs(_angleDiffSmooth.Current), 360f); // Favors the normal.

            var rotLerp = Helpers.Lerp(rotAmtNorm, rotAmtTan, angDiffFact);
            _rotAmtSmooth.Add(rotLerp);

            const float MAX_ROT_RATE = 2.8f; // Max rotation rate.
            const float MIN_ROT_RATE = 1.8f; // Min rotation rate.
            const float MIN_ROT_SPEED = 600f; // Speed at which rotation rate will be the smallest.
            const float ARM_DIST = 400f; // How far we must travel before engaging the target.
            const float MISS_TARG_DIST = 80f; // Distance to target to be considered a miss.
            const float REENGAGE_DIST = 2500f; // How far we must be from the target before re-engaging after a miss.
            const float ROT_MOD_DIST = 1000f; // Distance to begin increasing rotation rate. (Get more aggro the closer we get)
            const float ROT_MOD_AMT = 1f; //1.5f; // Max amount to increase rot rate per above distance.

            // Reduce rotation rate as velocity increases. Helps conserve inertia and reduce drag.
            var veloFact = Helpers.Factor(veloMag, MIN_ROT_SPEED);
            var rotFact = Math.Clamp((MAX_ROT_RATE * (1f - veloFact)) + MIN_ROT_RATE, MIN_ROT_RATE, MAX_ROT_RATE);

            // Increase rotation rate modifier as we approach the target.
            var rotMod = (1f - Helpers.Factor(targDist, ROT_MOD_DIST)) * ROT_MOD_AMT;
            rotFact += rotMod;

            // Increase rotation authority as we approach the arm distance.
            var rotAuthority = Helpers.Factor(_distTraveled, ARM_DIST);

            // Increase thrust during initial launch phase.
            _thrustBoost = THRUST * (1f - rotAuthority);

            // Detect when we miss the target.
            if (closingRate < MIN_CLOSE_RATE)
            {
                if (!_missedTarget && targDist < MISS_TARG_DIST)
                {
                    _missedTarget = true;

                    //this.IsExpired = true;

                    // Increase re-engage dist slightly with each miss.
                    _reEngageMod += REENGAGE_DIST * 0.5f; //0.15f;

                    _lastMissedLoc = target;
                    _missLoc = this.Position;


                    //// ??
                    //_stableAimPoint = target;

                    Debug.WriteLine($"Miss dist: {D2DPoint.Distance(_missLoc, _lastMissedLoc)}  Impact dist: {D2DPoint.Distance(_finalAimPoint, target)}  PosVImp: {D2DPoint.Distance(_finalAimPoint, this.Position)}");
                }
            }
            else
            {
                _missedTarget = false;

                //rotAuthority = Helpers.Factor(closingRate, MIN_CLOSE_RATE * 4f);

            }


            // Reduce rotation authority if we missed until we are the specified distance away from the target.
            // This helps give us room to turn around and make another attempt.
            if (_missedTarget && targDist < REENGAGE_DIST + _reEngageMod)
                rotAuthority = Helpers.Factor(targDist, REENGAGE_DIST + _reEngageMod);


            // Offset our current rotation from our current velocity vector to compute the next rotation.
            var nextRot = -(_rotAmtSmooth.Current * rotFact);
            return veloAngle + (nextRot * rotAuthority);
        }

        private double TimeOfArrival(double velo, double dv, double dist, double dt)
        {
            if (velo == 0f)
                return 0;

            double pos = 0;
            double steps = 0f;

            while (pos < dist && velo > 0f)
            {
                velo += dv;

                if (pos + (velo * dt) > dist)
                    break;

                pos += velo * dt;

                steps += dt;
            }

            return steps / dt;
        }

        private D2DPoint RefineImpact(D2DPoint targetPos, D2DPoint targetVelo, double targAngleDelta, double framesToImpact, float dt)
        {
            D2DPoint predicted = targetPos;

            //if (Math.Abs(targAngleDelta) == 0f)
            //    return predicted;

            if (framesToImpact >= 1 && framesToImpact < 6000)
            {
                var targLoc = targetPos;
                var angle = VecAngleD(targetVelo);

                for (int i = 0; i <= framesToImpact; i++)
                {
                    var avec = AngleToVectorD(angle) * targetVelo.Length();
                    targLoc += avec;
                    angle += -targAngleDelta;
                }

                //var rem = framesToImpact % (int)framesToImpact;
                //angle += -targAngleDelta * rem;
                //targLoc += (AngleToVectorD(angle) * targetVelo.Length()) * (float)rem;

                predicted = targLoc;
            }


            return predicted;
        }

    }
}
