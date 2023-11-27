using ProNav.GameObjects;
using System.Diagnostics;

namespace ProNav
{

    /// <summary>
    /// Provides nearest neighbor search for game objects.
    /// </summary>
    public class CollisionGrid
    {
        private const int GROW_PADDING = 20;

        private List<GameObject>[] _grid;
        private int _sideLen = 10;
        private Size _dims = new Size();
        private MinMax _bounds = new MinMax();
        private HashSet<int> _usedCells = new HashSet<int>(); // Used to track populated cells.

        public CollisionGrid(SizeF gridSize, int sideLen)
        {
            _sideLen = sideLen;

            int nX = (int)(gridSize.Width / _sideLen) + GROW_PADDING;
            int nY = (int)(gridSize.Height / _sideLen) + GROW_PADDING;

            _dims = new Size(nX, nY);
            _bounds = new MinMax(0, 0, gridSize.Width, gridSize.Height);

            _grid = new List<GameObject>[_dims.Width * _dims.Height];
        }

        public void Add(GameObject obj)
        {
            lock (_grid)
            {
                EnsureCapacity(obj.Position);

                var idx = CellIdx(OffsetPos(obj.Position));

                if (idx < 0)
                    return;

                if (_grid[idx] == null)
                    _grid[idx] = NewList(obj);
                else
                    _grid[idx].Add(obj);
            }
        }

        public void Add(List<GameObject> objs)
        {
            lock (_grid)
            {
                foreach (var obj in objs)
                {
                    EnsureCapacity(obj.Position);

                    var idx = CellIdx(OffsetPos(obj.Position));

                    if (idx < 0)
                        return;

                    if (_grid[idx] == null)
                        _grid[idx] = NewList(obj);
                    else
                        _grid[idx].Add(obj);
                }
            }
        }

        public void Clear()
        {
            foreach (var idx in _usedCells)
            {
                var objs = _grid[idx];
                objs.Clear();
            }
        }

        public IEnumerable<GameObject> GetNearest(GameObject obj)
        {
            return GetNearest(obj.Position);
        }

        public IEnumerable<GameObject> GetNearest(D2DPoint pos)
        {
            lock (_grid)
            {
                var idx = ToGridCoors(OffsetPos(pos));
                var ns = NCells(idx);

                foreach (var n in ns)
                {
                    var objs = _grid[n];
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
            _usedCells.Clear();

            for (int i = 0; i < _grid.Length; i++)
            {
                var objs = _grid[i];

                if (objs == null)
                    continue;

                for (int j = 0; j < objs.Count; j++)
                {
                    var obj = objs[j];

                    if (obj == null)
                        continue;

                    if (obj.IsExpired)
                    {
                        _grid[i].RemoveAt(j);
                    }
                    else
                    {
                        EnsureCapacity(obj.Position);

                        var newIdx = CellIdx(OffsetPos(obj.Position));
                        _usedCells.Add(newIdx);

                        if (newIdx != i)
                        {
                            if (_grid[newIdx] == null)
                                _grid[newIdx] = NewList(obj);
                            else
                                _grid[newIdx].Add(obj);

                            _grid[i].RemoveAt(j);

                        }
                    }
                }
            }
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
                var objs = _grid[idx];
                count += objs.Count;
            }

            return count;
        }

        public List<GameObject> GetAllObjects()
        {
            var allObjs = new List<GameObject>();

            foreach (var idx in _usedCells)
            {
                var objs = _grid[idx];
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
                var objs = _grid[idx];

                for (int i = 0; i < objs.Count; i++)
                    if (objs[i].IsExpired)
                        _grid[idx].RemoveAt(i);
            }
        }

        private void EnsureCapacity(D2DPoint pos)
        {
            EnsureCapacity(pos.X, pos.Y);
        }

        private void EnsureCapacity(float posX, float posY)
        {
            _bounds.Update(posX, posY);

            var newSize = new SizeF(_bounds.Width, _bounds.Height);
            var newDims = new Size(Math.Max((int)((newSize.Width / _sideLen) + GROW_PADDING), _dims.Width), Math.Max((int)((newSize.Height / _sideLen) + GROW_PADDING), _dims.Height));
            var newLength = newDims.Width * newDims.Height;

            if (newLength > _grid.Length || newDims.Width > _dims.Width || newDims.Height > _dims.Height)
            {
                ResizeGrid(newDims);
            }
        }

        private void ResizeGrid(Size newDims)
        {
            _dims = newDims;

            var newGrid = new List<GameObject>[_dims.Width * _dims.Height];

            Array.Copy(_grid, newGrid, _grid.Length);
            _grid = newGrid;

            Debug.WriteLine($"Resize: {_dims}");
        }

        /// <summary>
        /// Offsets the specified position in order to normalize it to the grid location.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
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
                        if (_grid[cellIdx] != null)
                            yield return cellIdx;
                    }
                }
            }
        }

    }

}
