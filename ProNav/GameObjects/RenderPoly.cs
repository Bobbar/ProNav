﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ProNav.GameObjects
{
    public class RenderPoly
    {
        public D2DPoint[] Poly;
        public D2DPoint[] SourcePoly;

        public RenderPoly(D2DPoint[] polygon) 
        {
            Poly = new D2DPoint[polygon.Length];
            SourcePoly = new D2DPoint[polygon.Length];

            Array.Copy(polygon, Poly, polygon.Length);
            Array.Copy(polygon, SourcePoly, polygon.Length);
        }

        public void Update(D2DPoint pos, float rotation, float scale)
        {
            ApplyTranslation(SourcePoly, Poly, rotation, pos, scale);
        }

        private void ApplyTranslation(D2DPoint[] src, D2DPoint[] dst, float rotation, D2DPoint translation, float scale = 1f)
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
    }
}