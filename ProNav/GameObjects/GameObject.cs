using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using unvell.D2DLib;

namespace ProNav.GameObjects
{
    public abstract class GameObject
    {
        public D2DPoint Position { get; set; }

        public D2DPoint Velocity { get; set; }

        public float Rotation
        {
            get { return _rotation; }

            set
            {
                _rotation = value % 360;

                if (_rotation < 0f)
                    _rotation += 360f;
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
            Position = Position.Add(new D2DPoint(Velocity.X * dt, Velocity.Y * dt));

            Rotation += RotationSpeed * dt;

            Wrap(viewport);
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


        protected float VecAngle(D2DPoint vec)
        {
            var angle = Math.Atan2(vec.Y, vec.X) * (180f / (float)Math.PI);

            angle = angle % 360;

            if (angle < 0f)
                angle += 360f;

            return (float)angle;
        }

        protected double VecAngleD(D2DPoint vec)
        {
            var angle = Math.Atan2(vec.Y, vec.X) * (180d / Math.PI);

            angle = angle % 360d;

            if (angle < 0d)
                angle += 360d;

            return angle;
        }

        protected float AngleDiff(float a, float b)
        {
            var normDeg = ModSign((a - b), 360f);

            var absDiffDeg = Math.Min(360f - normDeg, normDeg);

            return absDiffDeg;
        }

        protected double AngleDiffD(double a, double b)
        {
            var normDeg = ModSignD((a - b), 360d);

            var absDiffDeg = Math.Min(360d - normDeg, normDeg);

            return absDiffDeg;

        }

        protected float ModSign(float a, float n)
        {
            return a - (float)Math.Floor(a / n) * n;
        }

        protected double ModSignD(double a, double n)
        {
            return a - Math.Floor(a / n) * n;
        }

        protected D2DPoint AngleToVector(float angle)
        {
            var rads = angle * ((float)Math.PI / 180f);
            var vec = new D2DPoint((float)Math.Cos(rads), (float)Math.Sin(rads));
            return vec;
        }

        protected D2DPoint AngleToVectorD(double angle)
        {
            var rads = angle * (Math.PI / 180d);
            var vec = new D2DPoint((float)Math.Cos(rads), (float)Math.Sin(rads));
            return vec;
        }

        protected float Cross(D2DPoint vector1, D2DPoint vector2)
        {
            return (vector1.X * vector2.Y) - (vector1.Y * vector2.X);
        }

        protected float ClampAngle(float angle)
        {
            var ret = angle % 360f;

            if (ret < 0f)
                ret += 360f;

            return ret;
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
            //Polygon = polygon;
            //this.srcPoly = polygon;

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


        protected void ApplyTranslation(D2DPoint[] src, D2DPoint[] dst, float scale = 1f)
        {
            var mat = Matrix3x2.CreateScale(scale);
            mat *= Matrix3x2.CreateRotation(this.Rotation * (float)(Math.PI / 180f), D2DPoint.Zero);
            mat *= Matrix3x2.CreateTranslation(this.Position);

            for (int i = 0; i < dst.Length; i++)
            {
                var transPnt = D2DPoint.Transform(src[i], mat);
                dst[i] = transPnt;
            }
        }

        protected void ApplyTranslation(D2DPoint[] src, D2DPoint[] dst, float rotation, D2DPoint translation, float scale = 1f)
        {
            var mat = Matrix3x2.CreateScale(scale);
            mat *= Matrix3x2.CreateRotation(rotation * (float)(Math.PI / 180f), D2DPoint.Zero);
            mat *= Matrix3x2.CreateTranslation(translation);

            for (int i = 0; i < dst.Length; i++)
            {
                var transPnt = D2DPoint.Transform(src[i], mat);
                dst[i] = transPnt;
            }
        }

        protected D2DPoint ApplyTranslation(D2DPoint src, float rotation, D2DPoint translation, float scale = 1f)
        {
            var mat = Matrix3x2.CreateScale(scale);
            mat *= Matrix3x2.CreateRotation(rotation * (float)(Math.PI / 180f), D2DPoint.Zero);
            mat *= Matrix3x2.CreateTranslation(translation);

            return D2DPoint.Transform(src, mat);
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
