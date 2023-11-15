using System.Diagnostics;
using unvell.D2DLib;

namespace ProNav.GameObjects
{
    public class Wing : GameObject
    {
        private readonly float MAX_DEFLECTION = 40f;
        private readonly float MAX_VELO = 300f;
       
        public float RenderLength { get; set; }
        public float Area { get; set; }
        public float Deflection
        { 
            get { return _deflection; }
            set
            {
                if (value >= -MAX_DEFLECTION && value <= MAX_DEFLECTION)
                    _deflection = value;
                else
                    _deflection = Math.Sign(value) * MAX_DEFLECTION;
            }
        } 

        public D2DPoint LiftVector { get; set; }
        public D2DPoint DragVector { get; set; }
        public float AoA { get; set; }
        public D2DPoint ReferencePosition { get; set; }

        private D2DPoint _prevPosition;
        private float _deflection = 0f;
        private Missile _missle;

        public Wing(Missile missile, float renderLen, float area, D2DPoint position)
        {
            RenderLength = renderLen;
            Area = area;
            Position = position;
            ReferencePosition = position;
            Rotation = missile.Rotation;
            this.Velocity = D2DPoint.Zero;
            _missle = missile;
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            this.Rotation = _missle.Rotation + this.Deflection;
            this.Position = ApplyTranslation(this.ReferencePosition, _missle.Rotation, _missle.Position, renderScale);

            var nextVelo = D2DPoint.Zero;

            if (_prevPosition != D2DPoint.Zero)
                nextVelo = (this.Position - _prevPosition);
            else
                _prevPosition = this.Position;

            _prevPosition = this.Position;

            if (nextVelo.Length() <= MAX_VELO)
                this.Velocity = nextVelo;
            else
            {
                Debug.WriteLine($"Err velo too high!  ({nextVelo.Length()})");
                this.Velocity = this.Velocity.Normalized() * MAX_VELO;
            }
        }

        public override void Render(D2DGraphics gfx)
        {
            // Draw a fixed box behind the moving wing. Helps to visualize deflection.
            var startB = this.Position - Helpers.AngleToVectorDegrees(this.Rotation - this.Deflection) * RenderLength;
            var endB = this.Position + Helpers.AngleToVectorDegrees(this.Rotation - this.Deflection) * RenderLength;
            gfx.DrawLine(startB, endB, D2DColor.DarkGray, 2f);

            // Draw wing.
            var start = this.Position - Helpers.AngleToVectorDegrees(this.Rotation) * RenderLength;
            var end = this.Position + Helpers.AngleToVectorDegrees(this.Rotation) * RenderLength;
            gfx.DrawLine(start, end, D2DColor.Blue, 1f, D2DDashStyle.Solid, D2DCapStyle.Round, D2DCapStyle.Round);

            if (World.ShowAero)
            {
                const float SCALE = 0.04f;
                gfx.DrawLine(this.Position, this.Position + (LiftVector * SCALE), D2DColor.SkyBlue, 0.5f, D2DDashStyle.Solid, D2DCapStyle.Flat, D2DCapStyle.Triangle);
                gfx.DrawLine(this.Position, this.Position + (DragVector * SCALE), D2DColor.Red, 0.5f, D2DDashStyle.Solid, D2DCapStyle.Flat, D2DCapStyle.Triangle);
            }

        }

    }
}
