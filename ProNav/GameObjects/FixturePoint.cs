using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unvell.D2DLib;

namespace ProNav.GameObjects
{
    public class FixturePoint : GameObject
    {
        public GameObject GameObject { get; private set; }
        public D2DPoint ReferencePosition { get; private set; }

        public FixturePoint(GameObject gameObject, D2DPoint referencePosition)
        {
            this.GameObject = gameObject;
            this.ReferencePosition = referencePosition;
            this.Position = ApplyTranslation(ReferencePosition, gameObject.Rotation, gameObject.Position, World.RenderScale);
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
           this.Position = ApplyTranslation(ReferencePosition, GameObject.Rotation, GameObject.Position, renderScale);
        }


        public override void Render(D2DGraphics gfx)
        {
            //gfx.FillEllipse(new D2DEllipse(this.Position, new D2DSize(5f, 5f)), D2DColor.Red);
        }
    }
}
