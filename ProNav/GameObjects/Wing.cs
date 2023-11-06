using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public Wing(float renderLen, float area, float rotation, D2DPoint position)
        {
            RenderLength = renderLen;
            Area = area;
            Position = position;
            ReferencePosition = position;
            Rotation = rotation;
            this.Velocity = D2DPoint.Zero;
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            D2DPoint nextVelo = D2DPoint.Zero;

            if (_prevPosition != D2DPoint.Zero)
                nextVelo = (this.Position - _prevPosition);
            else
                _prevPosition = this.Position;
            
            _prevPosition = this.Position;

            if (nextVelo.Length() <= MAX_VELO)
                this.Velocity = nextVelo;
            else
                Debug.WriteLine($"Err velo too high!  ({nextVelo.Length()})");
        }

        public override void Render(D2DGraphics gfx)
        {
            if (World.ShowAero)
            {
                gfx.DrawLine(this.Position, this.Position + (LiftVector * 0.05f), D2DColor.SkyBlue, 0.5f);
                gfx.DrawLine(this.Position, this.Position + (DragVector * 0.08f), D2DColor.Red, 0.5f);
            }
          
            var startB = this.Position - Helpers.AngleToVectorDegrees(this.Rotation - this.Deflection) * RenderLength;
            var endB = this.Position + Helpers.AngleToVectorDegrees(this.Rotation - this.Deflection) * RenderLength;
            gfx.DrawLine(startB, endB, D2DColor.DarkGray, 1f);


            var start = this.Position - Helpers.AngleToVectorDegrees(this.Rotation) * RenderLength;
            var end = this.Position + Helpers.AngleToVectorDegrees(this.Rotation) * RenderLength;
            gfx.DrawLine(start, end, D2DColor.Blue, 1f);
           
        }

    }
}
