using ProNav.GameObjects;
using System.Diagnostics;

namespace ProNav
{

    /// <summary>
    /// Provides nearest neighbor search for game objects.
    /// </summary>
    public class CollisionGrid
    {
        public List<GameObject>[] Grid;

        private int _sideLen = 10;
        private Size _dims = new Size();
        private MinMax _bounds = new MinMax();
        private const int GROW_PADDING = 20;
        private HashSet<int> _usedCells = new HashSet<int>(); // Used to track populated cells.

        public CollisionGrid(SizeF gridSize, int sideLen)
        {
            _sideLen = sideLen;

            int nX = (int)gridSize.Width / _sideLen;
            int nY = (int)gridSize.Height / _sideLen;
            _dims = new Size(nX, nY);

            _bounds = new MinMax(0, 0, gridSize.Width, gridSize.Height);
            Grid = new List<GameObject>[_dims.Width * _dims.Height];
        }

        public void Add(GameObject obj)
        {
            lock (Grid)
            {
                EnsureCapacity(obj.Position);

                var idx = CellIdx(OffsetPos(obj.Position));

                if (idx < 0)
                    return;

                if (Grid[idx] == null)
                    Grid[idx] = NewList(obj);
                else
                    Grid[idx].Add(obj);
            }
        }

        public void Add(GameObjectPoly obj)
        {
            lock (Grid)
            {
                EnsureCapacity(obj.Position);

                var idx = CellIdx(OffsetPos(obj.Position));

                if (idx < 0)
                    return;

                if (Grid[idx] == null)
                    Grid[idx] = NewList(obj);
                else
                    Grid[idx].Add(obj);
            }
        }

        public void Clear()
        {
            foreach (var idx in _usedCells)
            {
                var objs = Grid[idx];
                objs.Clear();
            }
        }

        public IEnumerable<GameObject> GetNearest(GameObject obj)
        {
            return GetNearest(obj.Position);
        }

        public IEnumerable<GameObject> GetNearest(D2DPoint pos)
        {
            lock (Grid)
            {
                var idx = ToGridCoors(OffsetPos(pos));
                var ns = NCells(idx);

                foreach (var n in ns)
                {
                    var objs = Grid[n];
                    for (int i = 0; i < objs.Count; i++)
                        yield return objs[i];
                }
            }
        }

        /// <summary>
        /// Move objects to new grid locations and increases capacity as needed. Removes expired objects.
        /// </summary>
        public void Update()
        {
            var usedCells = new HashSet<int>();

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
                        usedCells.Add(newIdx);

                        if (newIdx != i)
                        {
                            if (Grid[newIdx] == null)
                                Grid[newIdx] = NewList(obj);
                            else
                                Grid[newIdx].Add(obj);

                            Grid[i].RemoveAt(j);

                        }
                    }
                }
            }

            _usedCells = usedCells;
        }

        public void Resize(float width, float height)
        {
            EnsureCapacity(new D2DPoint(width, height));
        }

        public int ObjCount()
        {
            int count = 0;

            foreach (var idx in _usedCells)
            {
                var objs = Grid[idx];
                count += objs.Count;
            }

            return count;
        }

        public List<GameObject> GetAllObjects()
        {
            var allObjs = new List<GameObject>();

            foreach (var idx in _usedCells)
            {
                var objs = Grid[idx];
                allObjs.AddRange(objs);
            }

            return allObjs;
        }

        private List<GameObject> NewList(GameObject obj)
        {
            return new List<GameObject> { obj };
        }

        private void PruneExpired()
        {
            foreach (var idx in _usedCells)
            {
                var objs = Grid[idx];

                for (int i = 0; i < objs.Count; i++)
                    if (objs[i].IsExpired)
                        Grid[idx].RemoveAt(i);
            }
        }

        private void EnsureCapacity(D2DPoint pos)
        {
            _bounds.Update(pos.X, pos.Y);

            var newSize = new SizeF(_bounds.Width, _bounds.Height);
            var newDims = new Size(Math.Max((int)((newSize.Width / _sideLen) + GROW_PADDING), _dims.Width), Math.Max((int)((newSize.Height / _sideLen) + GROW_PADDING), _dims.Height));
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

        private D2DPoint OffsetPos(D2DPoint pos)
        {
            return pos - new D2DPoint(_bounds.MinX, _bounds.MinY);
        }

        private Point ToGridCoors(D2DPoint pnt)
        {
            return new Point((int)Math.Floor(pnt.X / _sideLen), (int)Math.Floor(pnt.Y / _sideLen));
        }

        private int CellIdx(D2DPoint pnt)
        {
            var coords = ToGridCoors(pnt);
            return CellIdx(coords.X, coords.Y);
        }

        private int CellIdx(int x, int y)
        {
            return y * _dims.Width + x;
        }

        private IEnumerable<int> NCells(Point pnt)
        {
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
                            yield return cellIdx;
                    }
                }
            }
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
