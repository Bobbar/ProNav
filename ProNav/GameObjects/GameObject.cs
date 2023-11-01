using System.Numerics;
using unvell.D2DLib;

namespace ProNav.GameObjects
{
    public abstract class GameObject
    {
        public bool IsExpired { get; set; } = false;

        public D2DPoint Position { get; set; }

        public D2DPoint Velocity { get; set; }

        protected long currentFrame = 0;

        public float Rotation
        {
            get { return _rotation; }

            set
            {
                _rotation = ClampAngle(value);
            }
        }


        protected float _rotation = 0f;

        //protected Random _rnd = new Random();
        protected Random _rnd => Helpers.Rnd;

        public float RotationSpeed { get; set; }

        public GameObject() { }

        public GameObject(D2DPoint pos)
        {
            Position = pos;
        }

        public GameObject(D2DPoint pos, D2DPoint velo)
        {
            Position = pos;
            Velocity = velo;
        }

        public GameObject(D2DPoint pos, float rotation)
        {
            Position = pos;
            Rotation = rotation;
        }

        public GameObject(D2DPoint pos, D2DPoint velo, float rotation)
        {
            Position = pos;
            Velocity = velo;
            Rotation = rotation;
        }

        public GameObject(D2DPoint pos, D2DPoint velo, float rotation, float rotationSpeed)
        {
            Position = pos;
            Velocity = velo;
            Rotation = rotation;
            RotationSpeed = rotationSpeed;
        }

        public virtual void Update(float dt, D2DSize viewport, float renderScale)
        {
            Position += Velocity * dt;

            Rotation += RotationSpeed * dt;

            Wrap(viewport);

            currentFrame++;
        }

        public virtual void Wrap(D2DSize viewport)
        {
            if (this.Position.X < 0f)
                this.Position = new D2DPoint(viewport.width, this.Position.Y);

            if (this.Position.X > viewport.width)
                this.Position = new D2DPoint(0, this.Position.Y);

            if (this.Position.Y < 0f)
                this.Position = new D2DPoint(this.Position.X, viewport.height);

            if (this.Position.Y > viewport.height)
                this.Position = new D2DPoint(this.Position.X, 0);
        }

        public abstract void Render(D2DGraphics gfx);


        protected float AngleDiff(float a, float b) => Helpers.AngleDiff(a, b);
        protected double AngleDiffD(double a, double b) => Helpers.AngleDiffD(a, b);
        protected D2DPoint AngleToVector(float angle) => Helpers.AngleToVectorDegrees(angle);
        protected D2DPoint AngleToVectorD(double angle) => Helpers.AngleToVectorDegreesD(angle);
        protected float ClampAngle(float angle) => Helpers.ClampAngle(angle);
        protected double ClampAngleD(double angle) => Helpers.ClampAngleD(angle);



        protected D2DPoint ApplyTranslation(D2DPoint src, float rotation, D2DPoint translation, float scale = 1f)
        {
            var mat = Matrix3x2.CreateScale(scale);
            mat *= Matrix3x2.CreateRotation(rotation * (float)(Math.PI / 180f), D2DPoint.Zero);
            mat *= Matrix3x2.CreateTranslation(translation);

            return D2DPoint.Transform(src, mat);
        }
    }


    public class GameObjectPoly: GameObject
    {
        public RenderPoly Polygon;


        public GameObjectPoly()
        {
        }

        public GameObjectPoly(D2DPoint pos) : base(pos)
        {
        }

        public GameObjectPoly(D2DPoint pos, D2DPoint velo) : base(pos, velo)
        {
        }

        public GameObjectPoly(D2DPoint pos, D2DPoint velo, float rotation) : base(pos, velo, rotation)
        {
        }

        public GameObjectPoly(D2DPoint pos, D2DPoint velo, D2DPoint[] polygon) : base(pos, velo)
        {
            Polygon = new RenderPoly(polygon);
        }

        public GameObjectPoly(D2DPoint[] polygon)
        {
            Polygon = new RenderPoly(polygon);
        }

        public override void Update(float dt, D2DSize viewport, float renderScale)
        {
            base.Update(dt, viewport, renderScale);

            Polygon.Update(this.Position, this.Rotation, renderScale);
        }

        public override void Render(D2DGraphics gfx)
        {
            gfx.DrawPolygon(this.Polygon.Poly, D2DColor.White, 1f, D2DDashStyle.Solid, D2DColor.White);
        }

        public bool Contains(D2DPoint pnt)
        {
            int i, j = 0;
            bool c = false;
            for (i = 0, j = Polygon.Poly.Length - 1; i < Polygon.Poly.Length; j = i++)
            {
                if (((Polygon.Poly[i].Y > pnt.Y) != (Polygon.Poly[j].Y > pnt.Y)) && (pnt.X < (Polygon.Poly[j].X - Polygon.Poly[i].X) * (pnt.Y - Polygon.Poly[i].Y) / (Polygon.Poly[j].Y - Polygon.Poly[i].Y) + Polygon.Poly[i].X))
                    c = !c;
            }

            return c;
        }

        public D2DPoint CenterOfPolygon()
        {
            double[] centroid = new double[2];

            // List iteration
            // Link reference:
            // https://en.wikipedia.org/wiki/Centroid
            foreach (var point in this.Polygon.Poly)
            {
                centroid[0] += point[0];
                centroid[1] += point[1];
            }

            centroid[0] /= this.Polygon.Poly.Length;
            centroid[1] /= this.Polygon.Poly.Length;

            return new D2DPoint((float)centroid[0], (float)centroid[1]);
        }

        public static D2DPoint[] RandomPoly(int nPoints, int radius)
        {
            //var rnd = new Random();
            var rnd = Helpers.Rnd;

            var poly = new D2DPoint[nPoints];
            var dists = new float[nPoints];

            for (int i = 0; i < nPoints; i++)
            {
                dists[i] = rnd.Next(radius / 2, radius);
            }

            var radians = rnd.NextFloat(0.8f, 1.01f);
            var angle = 0f;

            for (int i = 0; i < nPoints; i++)
            {
                var pnt = new D2DPoint((float)Math.Cos(angle * radians) * dists[i], (float)Math.Sin(angle * radians) * dists[i]);
                poly[i] = pnt;
                angle += (float)(2f * Math.PI / nPoints);
            }

            return poly;
        }

    }

}
