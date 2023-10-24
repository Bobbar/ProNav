using unvell.D2DLib;

namespace ProNav.GameObjects
{
    public class Bullet : GameObject
    {
        public float Speed = 800f;
        public float Lifetime = 30f;
        
        private float _age = 0;

        public Bullet() : base() { }

        public Bullet(D2DPoint pos) : base(pos) { }

        public Bullet(D2DPoint pos, float rotation) : base(pos, rotation) 
        { 
            this.Velocity = AngleToVector(rotation) * this.Speed;
        }

        public Bullet(D2DPoint pos, D2DPoint velo, float lifeSpan) : base(pos, velo)
        {
            this.Lifetime = lifeSpan;
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            base.Update(dt, viewport, renderScale);

            _age += dt;

            if (_age >= Lifetime) 
                this.IsExpired = true;
        }

        public override void Wrap(D2DSize viewport)
        {
            //base.Wrap(viewport);

            if (this.Position.X < 0f)
                this.IsExpired = true;

            if (this.Position.X > viewport.width)
                this.IsExpired = true;

            if (this.Position.Y < 0f)
                this.IsExpired = true;

            if (this.Position.Y > viewport.height)
                this.IsExpired = true;
        }

        public override void Render(D2DGraphics gfx)
        {
            gfx.FillEllipse(new D2DEllipse(this.Position, new D2DSize(5,5)), D2DColor.Red);
        }
    }

    public class TargetedBullet : Bullet
    {
        public Target Target;
        public Action<D2DPoint> AddExplosionCallback { get; set; }

        public TargetedBullet(D2DPoint pos, Target target, float speed) : base(pos)
        {
            this.Speed = speed;
            this.Target = target;

            AimAtTarget(this.Target);
        }

        public TargetedBullet(D2DPoint pos, Target target) : base(pos)
        {
            this.Target = target;

            AimAtTarget(this.Target);
        }

        public override void Render(D2DGraphics gfx)
        {
            //base.Render(gfx);

            gfx.FillEllipse(new D2DEllipse(this.Position, new D2DSize(10, 10)), D2DColor.Yellow);
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            base.Update(dt, viewport, renderScale);

            //if (this.Target.IsExpired)
            //    this.IsExpired = true;

            const float proxyDetDist = 100f;

            if (D2DPoint.Distance(this.Position, this.Target.Position) < proxyDetDist && !this.Target.IsExpired && !this.IsExpired)
            {
                AddExplosionCallback(this.Position);
                this.IsExpired = true;
            }

        }

        private void AimAtTarget(Target target)
        {
            var delta = target.Position - this.Position;
            var vr = target.Velocity - this.Velocity;
            var dist = D2DPoint.Distance(target.Position, this.Position);
            var deltaTime = AimAhead(delta, vr, this.Speed);
            var toa = dist / this.Speed;
            var impact = RefineImpact(target.Position, target.Velocity, target.RotationSpeed, toa, 0.01f);

            D2DPoint aimPoint = D2DPoint.Zero;

            if (target.RotationSpeed == 0f)
            {
                if (deltaTime > 0)
                    aimPoint = target.Position + target.Velocity * deltaTime;
            }
            else
            {
                aimPoint = impact;
            }

            var angle = D2DPoint.Normalize(aimPoint - this.Position);
            this.Velocity = angle * this.Speed;
        }

        private float AimAhead(D2DPoint delta, D2DPoint vr, float bulletSpd)
        {
            var a = D2DPoint.Dot(vr, vr) - bulletSpd * bulletSpd;
            var b = 2f * D2DPoint.Dot(vr, delta);
            var c = D2DPoint.Dot(delta, delta);

            var det = b * b - 4f * a * c;

            if (det > 0f)
                return 2f * c / ((float)Math.Sqrt(det) - b);
            else
                return -1f;
        }

        private D2DPoint RefineImpact(D2DPoint targetPos, D2DPoint targetVelo, float targAngleDelta, float framesToImpact, float dt)
        {
            D2DPoint predicted = targetPos;

            if (framesToImpact >= 1 && framesToImpact < 6000)
            {
                var targLoc = targetPos;
                var angle = targetVelo.AngleD();

                float step = 0f;

                while (step < framesToImpact)
                {
                    var avec = AngleToVectorD(angle) * targetVelo.Length();
                    targLoc += avec * dt;
                    angle += -targAngleDelta * dt;
                    angle = ClampAngle((float)angle);
                    step += dt;
                }

                predicted = targLoc;
            }


            return predicted;
        }

    }
}
