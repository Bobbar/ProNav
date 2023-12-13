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

        public virtual void Render(D2DGraphics gfx) { }


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


    public class GameObjectPoly : GameObject
    {
        public RenderPoly Polygon = new RenderPoly();

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

        public virtual bool Contains(GameObjectPoly obj)
        {
            var poly1 = obj.Polygon.Poly;

            // First do velocity compensation collisions.
            // Extend line segments from the current position to the next position and check for intersections.
            for (int i = 0; i < poly1.Length; i++)
            {
                var pnt1 = poly1[i];
                var pnt2 = pnt1 + (obj.Velocity * World.SUB_DT);
                if (PolyIntersect(pnt1, pnt2, this.Polygon.Poly))
                    return true;
            }

            // Same as above but for the central point.
            if (PolyIntersect(obj.Position, obj.Position + (obj.Velocity * World.SUB_DT), this.Polygon.Poly))
                return true;

            // Plain old point-in-poly collisions.
            // Check for center point first.
            if (Contains(obj.Position))
                return true;

            // Check all other poly points.
            foreach (var pnt in obj.Polygon.Poly)
            {
                if (PointInPoly(pnt, this.Polygon.Poly))
                    return true;
            }

            // Reverse of above. Just in case...
            foreach (var pnt in Polygon.Poly)
            {
                if (PointInPoly(pnt, obj.Polygon.Poly))
                    return true;
            }

            return false;
        }

        private bool LinesIntersect(D2DPoint a1, D2DPoint a2, D2DPoint b1, D2DPoint b2)
        {
            var s1 = a2 - a1;
            var s2 = b2 - b1;

            var s = (-s1.Y * (a1.X - b1.X) + s1.X * (a1.Y - b1.Y)) / (-s2.X * s1.Y + s1.X * s2.Y);
            var t = (s2.X * (a1.Y - b1.Y) - s2.Y * (a1.X - b1.X)) / (-s2.X * s1.Y + s1.X * s2.Y);

            if (s >= 0f && s <= 1f && t >= 0f && t <= 1f)
            {
                return true;
            }

            return false; // No collision
        }

        private bool PolyIntersect(D2DPoint a, D2DPoint b, D2DPoint[] poly)
        {
            // Check the segment against every segment in the polygon.
            for (int i = 0; i < poly.Length - 1; i++)
            {
                var pnt1 = poly[i];
                var pnt2 = poly[i + 1];

                if (LinesIntersect(a, b, pnt1, pnt2))
                    return true;
            }

            return false;
        }

        public virtual bool Contains(D2DPoint pnt)
        {
            return PointInPoly(pnt, this.Polygon.Poly);
        }

        private bool PointInPoly(D2DPoint pnt, D2DPoint[] poly)
        {
            int i, j = 0;
            bool c = false;
            for (i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].Y > pnt.Y) != (poly[j].Y > pnt.Y)) && (pnt.X < (poly[j].X - poly[i].X) * (pnt.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X))
                    c = !c;
            }

            return c;
        }

        public D2DPoint CenterOfPolygon()
        {
            var centroid = D2DPoint.Zero;

            // List iteration
            // Link reference:
            // https://en.wikipedia.org/wiki/Centroid
            foreach (var point in this.Polygon.Poly)
                centroid += point;

            centroid /= this.Polygon.Poly.Length;
            return centroid;
        }

        public float GetInertia(RenderPoly poly, float mass)
        {
            var sum1 = 0f;
            var sum2 = 0f;
            var n = poly.SourcePoly.Length;

            for (int i = 0; i < n; i++)
            {
                var v1 = poly.SourcePoly[i];
                var v2 = poly.SourcePoly[(i + 1) % n];
                var a = Helpers.Cross(v2, v1);
                var b = D2DPoint.Dot(v1, v1) + D2DPoint.Dot(v1, v2) + D2DPoint.Dot(v2, v2);

                sum1 += a * b;
                sum2 += a;
            }

            return (mass * sum1) / (6.0f * sum2);
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
