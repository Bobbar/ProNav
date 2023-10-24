using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing.Design;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using ProNav.GameObjects;

namespace ProNav
{
    public class CollisionGrid
    {
        public List<GameObject>[] Grid;

        private int _sideLen = 10;
        private Size _dims = new Size();
        private MinMax _minMaxPos = new MinMax();
        private const float GROW_FACTOR = 2.0f;

        public CollisionGrid(SizeF gridSize, int sideLen)
        {
            _sideLen = sideLen;

            int nX = (int)gridSize.Width / _sideLen;
            int nY = (int)gridSize.Height / _sideLen;
            _dims = new Size(nX, nY);

            _minMaxPos = new MinMax(0, 0, gridSize.Width, gridSize.Height);
            Grid = new List<GameObject>[_dims.Width * _dims.Height];
        }

        public void Add(GameObject obj)
        {
            EnsureCapacity(obj.Position);

            var idx = CellIdx(OffsetPos(obj.Position));

            if (idx < 0)
                return;

            if (Grid[idx] == null)
                Grid[idx] = new List<GameObject> { obj };
            else
                Grid[idx].Add(obj);
        }

        public void Add(GameObjectPoly obj)
        {
            EnsureCapacity(obj.Position);

            var idx = CellIdx(OffsetPos(obj.Position));

            if (idx < 0)
                return;

            if (Grid[idx] == null)
                Grid[idx] = new List<GameObject> { obj };
            else
                Grid[idx].Add(obj);
        }

        public void Clear()
        {
            Grid = new List<GameObject>[_dims.Width * _dims.Height];
        }

        public List<GameObject> GetNearest(GameObject obj)
        {
            var idx = ToGridCoors(OffsetPos(obj.Position));
            var nearest = new List<GameObject>();
            var ns = NCells(idx);

            foreach (var n in ns)
            {
                var objs = Grid[n];
                nearest.AddRange(objs);
            }

            return nearest;
        }

        public void Update()
        {
            for (int i = 0; i < Grid.Length; i++)
            {
                var objs = Grid[i];

                if (objs == null)
                    continue;

                for (int j = 0; j < objs.Count; j++)
                {
                    var obj = objs[j];

                    if (obj == null) 
                        continue;

                    if (obj.IsExpired)
                    {
                        Grid[i].RemoveAt(j);
                    }
                    else
                    {
                        EnsureCapacity(obj.Position);

                        var newIdx = CellIdx(OffsetPos(obj.Position));

                        if (Grid[i] != null)
                        {
                            if (newIdx != i)
                            {
                                if (Grid[newIdx] == null)
                                    Grid[newIdx] = new List<GameObject> { obj };
                                else
                                    Grid[newIdx].Add(obj);

                                Grid[i].RemoveAt(j);

                            }
                        }
                    }
                }
            }
        }

        public void Resize(float x, float y)
        {
            EnsureCapacity(new D2DPoint(x, y));
        }

        private void EnsureCapacity(D2DPoint pos)
        {
            UpdateMin(pos);
            
            var newSize = new SizeF(_minMaxPos.Width, _minMaxPos.Height);
            var newDims = new Size(Math.Max((int)(newSize.Width / _sideLen * GROW_FACTOR), _dims.Width), Math.Max((int)(newSize.Height / _sideLen * GROW_FACTOR), _dims.Height));
            var newLength = newDims.Width * newDims.Height;

            if (newLength > Grid.Length || newDims.Width > _dims.Width || newDims.Height > _dims.Height)
            {
                ResizeGrid(newDims);
            }
        }

        private void ResizeGrid(Size newDims)
        {
            _dims = newDims;

            var newGrid = new List<GameObject>[_dims.Width * _dims.Height];

            Array.Copy(Grid, newGrid, Grid.Length);
            Grid = newGrid;

            Debug.WriteLine($"Resize: {_dims}");
        }

        private void UpdateMin(D2DPoint pos)
        {
            _minMaxPos.Update(pos.X, pos.Y);
        }

        public int ObjCount()
        {
            int count = 0;
            for (int i = 0; i < Grid.Length; i++)
            {
                var objs = Grid[i];

                if (objs != null)
                    count += objs.Count;
            }

            return count;
        }

        private D2DPoint OffsetPos(D2DPoint pos)
        {
            return pos - new D2DPoint(_minMaxPos.MinX, _minMaxPos.MinY);
        }

       

        private Point ToGridCoors(D2DPoint pnt)
        {
            return new Point((int)Math.Floor(pnt.X / _sideLen), (int)Math.Floor(pnt.Y / _sideLen));
        }

        private int CellIdx(D2DPoint pnt)
        {
            return CellIdx((int)Math.Floor(pnt.X / _sideLen), (int)Math.Floor(pnt.Y / _sideLen));
        }

        private int CellIdx(int x, int y)
        {
            return y * _dims.Width + x;
        }

        private List<int> NCells(Point pnt)
        {
            var ns = new List<int>();

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    int ox = pnt.X + x;
                    int oy = pnt.Y + y;

                    if (ox >= 0 && ox < _dims.Width && oy >= 0 && oy < _dims.Height)
                    {
                        var cellIdx = CellIdx(ox, oy);
                        if (Grid[cellIdx] != null)
                            ns.Add(cellIdx);
                    }
                }
            }

            return ns;
        }

        private class MinMax
        {
            public float MinX;
            public float MinY;
            public float MaxX;
            public float MaxY;

            public float Width
            {
                get
                {
                    return Math.Abs(MinX - MaxX);
                }
            }

            public float Height
            {
                get
                {
                    return Math.Abs(MinY - MaxY);
                }
            }

            public MinMax()
            {
                MinX = float.MaxValue;
                MinY = float.MaxValue;
                MaxX = float.MinValue;
                MaxY = float.MinValue;
            }

            public MinMax(float minX, float minY, float maxX, float maxY)
            {
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
            }

            public void Update(float x, float y)
            {
                MinX = Math.Min(MinX, x);
                MinY = Math.Min(MinY, y);
                MaxX = Math.Max(MaxX, x);
                MaxY = Math.Max(MaxY, y);
            }

            public void Update(MinMax minMax)
            {
                MinX = Math.Min(MinX, minMax.MinX);
                MinY = Math.Min(MinY, minMax.MinY);
                MaxX = Math.Max(MaxX, minMax.MaxX);
                MaxY = Math.Max(MaxY, minMax.MaxY);
            }

            public void Reset()
            {
                MinX = float.MaxValue;
                MinY = float.MaxValue;
                MaxX = float.MinValue;
                MaxY = float.MinValue;
            }
        }

    }

}
