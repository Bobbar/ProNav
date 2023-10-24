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
        private Size _maxSize = new Size();
        private PointF _minPos = new PointF(0, 0);


        public CollisionGrid(Size gridSize, int sideLen)
        {
            _sideLen = sideLen;
            _maxSize = gridSize;

            int nX = gridSize.Width / _sideLen;
            int nY = gridSize.Height / _sideLen;
            _dims = new Size(nX, nY);

            Grid = new List<GameObject>[_dims.Width * _dims.Height];
        }

        public void Clear()
        {
            Grid = new List<GameObject>[_dims.Width * _dims.Height];
        }

        public List<GameObject> GetNearest(GameObject obj)
        {
            var idx = ToGridCoors(obj.Position - new D2DPoint(_minPos.X, _minPos.Y));
            var nearest = new List<GameObject>();
            var ns = NCells(idx);

            foreach (var n in ns)
            {
                nearest.AddRange(Grid[n]);
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
                        if (UpdateMin(obj.Position))
                        {
                            return;
                        }

                        var newIdx = CellIdx(obj.Position - new D2DPoint(_minPos.X, _minPos.Y));

                        if (newIdx > Grid.Length)
                        {
                            var idxCoords = CellIdxToCoord(newIdx);
                            Resize(new Size(idxCoords.X, idxCoords.Y));
                            return;
                        }


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

            //Prune();
        }

        public void Prune()
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
                        Grid[i].RemoveAt(j);
                }
            }
        }



        private bool UpdateMin(D2DPoint pos)
        {
            bool updated = false;
            var newMin = new PointF(Math.Min(_minPos.X, pos.X), Math.Min(_minPos.Y, pos.Y));
            _minPos = newMin;
            var maxIdx = CellIdx(new D2DPoint(_maxSize.Width + -newMin.X, _maxSize.Height + -newMin.Y));

            if (maxIdx > Grid.Length)
            {
                Resize(new Size(_dims.Width + (-(int)_minPos.X / _sideLen), _dims.Height + (-(int)_minPos.Y / _sideLen)));
                updated = true;
            }

            return updated;
        }

        private void Resize(Size newSize)
        {
            const int padding = 40;

            _dims = new Size(newSize.Width >= _dims.Width ? newSize.Width + padding : _dims.Width, newSize.Height >= _dims.Height ? newSize.Height + padding : _dims.Height);
            var newGrid = new List<GameObject>[_dims.Width * _dims.Height];

            Array.Copy(Grid, newGrid, Grid.Length);
            Grid = null;
            Grid = newGrid;

            Debug.WriteLine($"Resize: {_dims}");

            Update();
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


        public void Add(GameObject obj)
        {
            UpdateMin(obj.Position);

            var idx = CellIdx(obj.Position - new D2DPoint(_minPos.X, _minPos.Y));

            if (idx < 0)
                return;

            if (Grid[idx] == null)
                Grid[idx] = new List<GameObject> { obj };
            else
                Grid[idx].Add(obj);
        }

        public void Add(GameObjectPoly obj)
        {
            UpdateMin(obj.Position);

            var idx = CellIdx(obj.Position - new D2DPoint(_minPos.X, _minPos.Y));

            if (idx < 0)
                return;

            if (Grid[idx] == null)
                Grid[idx] = new List<GameObject> { obj };
            else
                Grid[idx].Add(obj);
        }

        private Point ToGridCoors(D2DPoint pnt)
        {
            return new Point((int)Math.Floor(pnt.Y / _sideLen), (int)Math.Floor(pnt.X / _sideLen));
        }

        private int CellIdx(D2DPoint pnt)
        {
            return CellIdx((int)Math.Floor(pnt.Y / _sideLen), (int)Math.Floor(pnt.X / _sideLen));
        }

        private int CellIdx(int x, int y)
        {
            return y * _dims.Height + x;
        }

        private Point CellIdxToCoord(int idx)
        {
            int x = idx / _dims.Height;
            int y = idx % _dims.Height;
            return new Point(x, y);
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

    }
}
