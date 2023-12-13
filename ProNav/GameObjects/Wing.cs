using unvell.D2DLib;

namespace ProNav.GameObjects
{
    public class Wing : GameObject
    {
        public float RenderLength { get; set; }
        public float Area { get; set; }
        public float Deflection
        {
            get { return _deflection; }
            set
            {
                if (value >= -_maxDeflection && value <= _maxDeflection)
                    _deflection = value;
                else
                    _deflection = Math.Sign(value) * _maxDeflection;
            }
        }

        public D2DPoint LiftVector { get; set; }
        public D2DPoint DragVector { get; set; }
        public float AoA { get; set; }

        private FixturePoint FixedPosition;
        private D2DPoint _prevPosition;
        private float _deflection = 0f;
        private Missile _missle;
        private float _maxDeflection = 40f;

        public Wing(Missile missile, float renderLen, float area, D2DPoint position)
        {
            FixedPosition = new FixturePoint(missile, position);

            RenderLength = renderLen;
            Area = area;
            Rotation = missile.Rotation;
            this.Velocity = D2DPoint.Zero;
            _missle = missile;
        }

        public Wing(Missile missile, float renderLen, float area, float maxDeflection, D2DPoint position)
        {
            FixedPosition = new FixturePoint(missile, position);

            RenderLength = renderLen;
            Area = area;
            Rotation = missile.Rotation;
            _maxDeflection = maxDeflection;
            this.Velocity = D2DPoint.Zero;
            _missle = missile;
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            FixedPosition.Update(dt, viewport, renderScale);

            this.Rotation = _missle.Rotation + this.Deflection;
            this.Position = FixedPosition.Position;

            var nextVelo = D2DPoint.Zero;

            if (_prevPosition != D2DPoint.Zero)
                nextVelo = (this.Position - _prevPosition) / dt;
            else
                _prevPosition = this.Position;

            _prevPosition = this.Position;

            this.Velocity = nextVelo;
        }

        public override void Render(D2DGraphics gfx)
        {
            // Draw a fixed box behind the moving wing. Helps to visualize deflection.
            var fixedVec = Helpers.AngleToVectorDegrees(this.Rotation - this.Deflection);
            var startB = this.Position - fixedVec * RenderLength;
            var endB = this.Position + fixedVec * RenderLength;
            gfx.DrawLine(startB, endB, D2DColor.DarkGray, 2f);

            // Draw wing.
            var wingVec = Helpers.AngleToVectorDegrees(this.Rotation);
            var start = this.Position - wingVec * RenderLength;
            var end = this.Position + wingVec * RenderLength;
            gfx.DrawLine(start, end, D2DColor.Blue, 1f, D2DDashStyle.Solid, D2DCapStyle.Round, D2DCapStyle.Round);

            if (World.ShowAero)
            {
                const float SCALE = 0.04f;
                gfx.DrawLine(this.Position, this.Position + (LiftVector * SCALE), D2DColor.SkyBlue, 0.5f, D2DDashStyle.Solid, D2DCapStyle.Flat, D2DCapStyle.Triangle);
                gfx.DrawLine(this.Position, this.Position + (DragVector * SCALE), D2DColor.Red, 0.5f, D2DDashStyle.Solid, D2DCapStyle.Flat, D2DCapStyle.Triangle);
            }
        }

        public D2DPoint GetLiftDragForce()
        {
            if (this.Velocity.Length() == 0f)
                return D2DPoint.Zero;

            var velo = -World.Wind;

            velo += this.Velocity;

            var veloMag = velo.Length();
            var veloMagSq = (float)Math.Pow(veloMag, 2f);

            // Compute velo tangent. For lift/drag and rotation calcs.
            var veloNorm = D2DPoint.Normalize(velo);
            var veloNormTan = new D2DPoint(veloNorm.Y, -veloNorm.X);

            // Compute angle of attack.
            var aoaRads = AngleToVector(this.Rotation).Cross(veloNorm);
            var aoa = Helpers.RadsToDegrees(aoaRads);

            // Compute lift force as velocity tangent with angle-of-attack effecting magnitude and direction. Velocity magnitude is factored as well.
            // Greater AoA and greater velocity = more lift force.

            // Wing & air parameters.
            const float AOA_FACT = 0.2f; // How much AoA effects drag.
            const float VELO_FACT = 0.4f; // How much velocity effects drag.
            float WING_AREA = this.Area; // Area of the wing. Effects lift & drag forces.
            const float MAX_LIFT = 20000f; // Max lift force allowed.
            const float MAX_AOA = 40f; // Max AoA allowed before lift force reduces. (Stall)
            float AIR_DENSITY = World.AirDensity;
            const float PARASITIC_DRAG = 0.5f;

            // Drag force.
            var dragAoa = 1f - (float)Math.Cos(2f * aoaRads);
            var dragForce = dragAoa * AOA_FACT * WING_AREA * 0.5f * AIR_DENSITY * veloMagSq * VELO_FACT;
            dragForce += veloMag * (WING_AREA * PARASITIC_DRAG);

            // Lift force.
            var aoaFact = Helpers.Factor(MAX_AOA, Math.Abs(aoa));
            var coeffLift = (float)Math.Sin(2f * aoaRads) * aoaFact;
            var liftForce = AIR_DENSITY * 0.5f * veloMagSq * WING_AREA * coeffLift;
            liftForce = Math.Clamp(liftForce, -MAX_LIFT, MAX_LIFT);

            var dragVec = -veloNorm * dragForce;
            var liftVec = veloNormTan * liftForce;

            this.LiftVector = liftVec;
            this.DragVector = dragVec;
            this.AoA = aoa;

            return (liftVec + dragVec);
        }

    }
}
