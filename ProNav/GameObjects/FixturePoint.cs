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
    }
}
