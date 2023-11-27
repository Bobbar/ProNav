using unvell.D2DLib;

namespace ProNav.GameObjects
{
    public abstract class Missile : GameObjectPoly
    {
        public float DistTraveled = 0f;
        private D2DPoint _prevPos = D2DPoint.Zero;

        public Missile() { }

        public Missile(D2DPoint pos) : base(pos)
        {
            _prevPos = pos;
        }

        public Missile(D2DPoint pos, D2DPoint velo) : base(pos, velo)
        {
            _prevPos = pos;
        }

        public Missile(D2DPoint pos, D2DPoint velo, float rotation) : base(pos, velo, rotation)
        {
            _prevPos = pos;
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            base.Update(dt, viewport, renderScale);

            var dist = D2DPoint.Distance(this.Position, _prevPos);
            _prevPos = this.Position;
            DistTraveled += dist;
        }
    }
}
