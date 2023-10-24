using ProNav.GameObjects;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using unvell.D2DLib;

namespace ProNav
{
    public partial class Form1 : Form
    {
        private D2DDevice _device;
        private D2DGraphics _gfx;
        private Task _renderThread;

        private const float DT = 0.1f;
        private const float RENDER_SCALE = -0.3f;
        private const int PHYSICS_STEPS = 8;
        private const float ROTATE_RATE = 2f;

        private float _zoomScale = 0.2f;
        private float ZoomScale
        {
            get => _zoomScale;

            set
            {
                if (value >= 0.1f && value <= 3f)
                    _zoomScale = value;

                Debug.WriteLine($"Zoom: {_zoomScale}");
            }
        }

        private float ViewPortScaleMulti
        {
            get
            {
                var multi = 1f / _zoomScale;
                return multi;
            }
        }

        private D2DSize _viewPortSize;
        private D2DRect _viewPortRect;

        private ManualResetEventSlim _pauseRenderEvent = new ManualResetEventSlim(true);
        private ManualResetEventSlim _stopRenderEvent = new ManualResetEventSlim(true);

        private bool _isPaused = false;
        private bool _trailsOn = false;
        private bool _oneStep = false;
        private bool _moveShip = false;
        private bool _spawnTargetKey = false;
        private bool _killRender = false;
        private bool _fireBurst = false;
        private bool _motionBlur = false;
        private bool _shiftDown = false;
        private bool _useCollisionGrid = true;
        private bool _renderEveryStep = true;

        private const int BURST_NUM = 10;
        private const int BURST_FRAMES = 3;
        private int _burstCount = 0;
        private int _burstFrame = 0;
        private long _lastRenderTime = 0;
        private float _renderFPS = 0;

        private LockedList<GameObjectPoly> _missiles = new LockedList<GameObjectPoly>();
        private LockedList<GameObjectPoly> _targets = new LockedList<GameObjectPoly>();
        private LockedList<GameObject> _bullets = new LockedList<GameObject>();

        private int _colGridSideLen = 100;
        private CollisionGrid _colGrid;

        private Ship _player = new Ship();
        private GuidedMissile.GuidanceType _guidanceType = GuidedMissile.GuidanceType.Advanced;

        private D2DColor _blurColor = new D2DColor(0.05f, D2DColor.Black);
        private D2DPoint _infoPosition = new D2DPoint(30, 30);

        private Random _rnd => Helpers.Rnd;

        public Form1()
        {
            InitializeComponent();

            this.MouseWheel += Form1_MouseWheel;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            InitGfx();

            StartRenderThread();

            _player.Position = new D2DPoint(_viewPortSize.width * 0.5f, _viewPortSize.height * 0.5f);
            _player.FireBulletCallback = b => { _bullets.Add(b); _colGrid.Add(b); };
        }

        private void StartRenderThread()
        {
            _renderThread?.Dispose();
            _renderThread = new Task(RenderLoop, TaskCreationOptions.LongRunning);
            _renderThread.Start();
        }

        private void InitGfx()
        {
            _device?.Dispose();
            _device = D2DDevice.FromHwnd(this.Handle);
            _gfx = new D2DGraphics(_device);
            _gfx.Antialias = true;
            _device.Resize();

            _viewPortSize = new D2DSize(this.Width * ViewPortScaleMulti, this.Height * ViewPortScaleMulti);
            _viewPortRect = new D2DRect(0, 0, this.Width * ViewPortScaleMulti, this.Height * ViewPortScaleMulti);

            _colGrid = new CollisionGrid(new Size((int)(_viewPortSize.width), (int)(_viewPortSize.height)), _colGridSideLen);
        }

        private void ResizeGfx()
        {
            StopRender();

            _device?.Resize();

            _viewPortSize = new D2DSize(this.Width * ViewPortScaleMulti, this.Height * ViewPortScaleMulti);
            _viewPortRect = new D2DRect(0, 0, this.Width * ViewPortScaleMulti, this.Height * ViewPortScaleMulti);

            _colGrid.Resize(_viewPortSize.width, _viewPortSize.height);

            ResumeRender();
        }

        private void RenderLoop()
        {
            while (!this.Disposing && !_killRender)
            {
                _stopRenderEvent.Wait();

                if (_trailsOn || _motionBlur)
                    _gfx.BeginRender();
                else
                    _gfx.BeginRender(D2DColor.Black);

                if (_motionBlur)
                    _gfx.FillRectangle(_viewPortRect, _blurColor);

                _gfx.PushTransform();
                _gfx.ScaleTransform(_zoomScale, _zoomScale);

                // Render stuff...

                if (!_isPaused || _oneStep)
                {
                    var partialDT = DT / PHYSICS_STEPS;

                    for (int i = 0; i < PHYSICS_STEPS; i++)
                    {
                        _missiles.ForEach(o => o.Update(partialDT, _viewPortSize, RENDER_SCALE));
                        _targets.ForEach(o => o.Update(partialDT, _viewPortSize, RENDER_SCALE));
                        _bullets.ForEach(o => o.Update(partialDT, _viewPortSize, RENDER_SCALE));

                        if (_renderEveryStep)
                        {
                            _missiles.ForEach(o => o.Render(_gfx));
                            _targets.ForEach(o => o.Render(_gfx));
                            _bullets.ForEach(o => o.Render(_gfx));

                        }

                        if (_useCollisionGrid)
                            DoCollisionsGrid();
                        else
                            DoCollisions();
                    }

                    _oneStep = false;
                }

                if (!_renderEveryStep || _isPaused)
                {
                    _missiles.ForEach(o => o.Render(_gfx));
                    _targets.ForEach(o => o.Render(_gfx));
                    _bullets.ForEach(o => o.Render(_gfx));
                }

                _player.Update(DT, _viewPortSize, RENDER_SCALE);
                _player.Render(_gfx);

                _gfx.PopTransform();

                DrawOverlays(_gfx);

                _gfx.EndRender();

                var fps = TimeSpan.TicksPerSecond / (float)(DateTime.Now.Ticks - _lastRenderTime);
                _lastRenderTime = DateTime.Now.Ticks;
                _renderFPS = fps;

                if (_fireBurst)
                {
                    if (_burstFrame == 0 || _burstFrame < BURST_FRAMES)
                    {
                        TargetAllWithBullet();
                        _burstCount++;
                        _burstFrame = 0;
                    }

                    _burstFrame++;

                    if (_burstCount == BURST_NUM)
                    {
                        //_fireBurst = false;
                        _burstCount = 0;
                        _burstFrame = 0;
                    }
                }

                if (!_pauseRenderEvent.Wait(0))
                {
                    _isPaused = true;
                    _pauseRenderEvent.Set();
                }
            }
        }

        private void PauseRender()
        {
            if (!_isPaused)
            {
                _pauseRenderEvent.Reset();
                _pauseRenderEvent.Wait();
            }
        }

        private void ResumeRender()
        {
            if (_isPaused && _stopRenderEvent.Wait(0))
            {
                _pauseRenderEvent.Set();
                _isPaused = false;
            }

            if (!_stopRenderEvent.Wait(0))
                _stopRenderEvent.Set();
        }

        private void StopRender()
        {
            _stopRenderEvent.Reset();
            Thread.Sleep(32);
        }

        private void DoCollisionsGrid()
        {
            _colGrid.Update();

            for (int r = 0; r < _targets.Count; r++)
            {
                var targ = _targets[r] as Target;
                var nearest = _colGrid.GetNearest(targ);

                foreach (var obj in nearest)
                {
                    if (targ.Contains(obj.Position) || targ.Contains(obj.Position + (obj.Velocity * (DT / 8f))))
                    {
                        targ.IsExpired = true;
                        obj.IsExpired = true;
                    }
                }
            }

            for (int o = 0; o < _missiles.Count; o++)
            {
                var missile = _missiles[o];

                if (missile.IsExpired)
                    _missiles.RemoveAt(o);
            }

            for (int o = 0; o < _targets.Count; o++)
            {
                var targ = _targets[o];

                if (targ.IsExpired)
                    _targets.RemoveAt(o);
            }

            for (int o = 0; o < _bullets.Count; o++)
            {
                var bullet = _bullets[o];

                if (bullet.IsExpired)
                    _bullets.RemoveAt(o);
            }
        }

        private void DoCollisions()
        {
            for (int r = 0; r < _targets.Count; r++)
            {
                var targ = _targets[r] as Target;

                for (int m = 0; m < _missiles.Count; m++)
                {
                    var missile = _missiles[m] as Missile;

                    if (targ.Contains(missile.Position) || targ.Contains(missile.Position + (missile.Velocity * (DT / 8f))))
                    {
                        targ.IsExpired = true;
                        missile.IsExpired = true;
                    }
                }


                for (int b = 0; b < _bullets.Count; b++)
                {
                    var bullet = _bullets[b];

                    if (targ.Contains(bullet.Position))
                    {
                        targ.IsExpired = true;
                        bullet.IsExpired = true;
                    }
                }
            }

            for (int o = 0; o < _missiles.Count; o++)
            {
                var missile = _missiles[o];

                if (missile.IsExpired)
                    _missiles.RemoveAt(o);
            }

            for (int o = 0; o < _targets.Count; o++)
            {
                var targ = _targets[o];

                if (targ.IsExpired)
                    _targets.RemoveAt(o);
            }

            for (int o = 0; o < _bullets.Count; o++)
            {
                var bullet = _bullets[o];

                if (bullet.IsExpired)
                    _bullets.RemoveAt(o);
            }

        }

        private void SpawnTargets(int num)
        {
            var vpSzHalf = new D2DSize(_viewPortSize.width * 0.5f, _viewPortSize.height * 0.5f);

            for (int i = 0; i < num; i++)
            {
                var pos = new D2DPoint(_rnd.NextFloat((vpSzHalf.width - (vpSzHalf.width * 0.5f)), vpSzHalf.width + (vpSzHalf.width * 0.5f)), _rnd.NextFloat((vpSzHalf.height - (vpSzHalf.height * 0.5f)), vpSzHalf.height + (vpSzHalf.height * 0.5f)));
                SpawnTarget(pos);
            }
        }

        private void SpawnTarget(D2DPoint pos)
        {
            //var targ = new ErraticMovingTarget(pos);
            //var targ = new RotatingMovingTarget(pos);
            //var targ = new LinearMovingTarget(pos);

            Target targ = null;
            var rndT = _rnd.Next(3);
            switch (rndT)
            {
                case 0:
                    targ = new ErraticMovingTarget(pos);
                    break;

                case 1:
                    targ = new RotatingMovingTarget(pos);
                    break;

                case 2:
                    targ = new LinearMovingTarget(pos);
                    break;
            }

            _targets.Add(targ);
        }

        private void TargetAllWithMissile()
        {
            for (int i = 0; i < _targets.Count; i++)
            {
                var targ = _targets[i];
                var missile = new GuidedMissile(_player, targ as Target, _guidanceType);

                _missiles.Add(missile);
                _colGrid.Add(missile);
            }

            //Debug.WriteLine("----");
        }

        private void TargetAllWithBullet()
        {
            if (_targets.Count == 0)
                return;

            for (int i = 0; i < _targets.Count; i++)
            {
                var targ = _targets[i];
                _player.FireBullet(targ as Target, p => AddBulletExplosion(p));
            }
        }

        private void AddBulletExplosion(D2DPoint pos)
        {
            const int numParticles = 40;
            const float velo = 1000f;
            const float lifetime = 0.4f;//0.2f;
            float angle = 0f;

            //float radStep = 0.05f;
            //while (angle <= Helpers.DegreesToRads(360f))
            //{
            //    var vec = Helpers.AngleToVectorRads(angle) * velo;
            //    var bullet = new Bullet(pos, vec, lifetime);

            //    _bullets.Add(bullet);
            //    _colGrid.Add(bullet);

            //    angle += radStep;
            //}


            float radStep = 1f;
            for (int i = 0; i < numParticles; i++)
            {
                var vec = new D2DPoint((float)Math.Cos(angle * radStep) * velo, (float)Math.Sin(angle * radStep) * velo);
                var bullet = new Bullet(pos, vec, lifetime);

                _bullets.Add(bullet);
                _colGrid.Add(bullet);

                angle += (float)(2f * Math.PI / numParticles);
            }
        }

        private void Clear()
        {
            PauseRender();
            _missiles.Clear();
            _targets.Clear();
            _bullets.Clear();
            _colGrid.Clear();
            ResumeRender();
        }

        private void Benchmark()
        {
            PauseRender();
            //Clear();

            SpawnTargets(20);


            for (int i = 0; i < 200; i++)
                TargetAllWithBullet();

            ResumeRender();
        }

        private void AccTest()
        {
            _killRender = true;
            Thread.Sleep(32);

            _missiles.Clear();
            _targets.Clear();

            _player.Position = new D2DPoint(106, 100);
            _player.Rotation = 0;

            Helpers.Rnd = new Random(1234);

            int ntests = 100;
            int hits = 0;
            int misses = 0;

            for (int i = 0; i < ntests; i++)
            {
                SpawnTargets(1);
                TargetAllWithMissile();

                Debug.WriteLine($"Test {i + 1}");

                while (_missiles.Count > 0 && _targets.Count > 0)
                {
                    var partialDT = DT / PHYSICS_STEPS;

                    for (int s = 0; s < PHYSICS_STEPS; s++)
                    {
                        _missiles.ForEach(o => o.Update(partialDT, _viewPortSize, RENDER_SCALE));
                        _targets.ForEach(o => o.Update(partialDT, _viewPortSize, RENDER_SCALE));


                        //_missiles.ForEach(o => o.Render(_gfx));
                        //_targets.ForEach(o => o.Render(_gfx));

                        DoCollisions();
                    }
                }

                if (_missiles.Count == 0 && _targets.Count == 1)
                    misses++;
                else if (_missiles.Count == 0 && _targets.Count == 0)
                    hits++;

            }


            Debug.WriteLine($"Hits: {hits}  Misses: {misses}");

            _killRender = false;
            Helpers.Rnd = new Random(1234);

            StartRenderThread();
            ResumeRender();
        }

        private void DrawOverlays(D2DGraphics gfx)
        {
            //DrawRadial(_gfx, new D2DPoint(this.Width * 0.5f, this.Height * 0.5f));
            DrawInfo(_gfx, _infoPosition);

            //DrawGrid(gfx);
        }

        private void DrawInfo(D2DGraphics gfx, D2DPoint pos)
        {
            string infoText = string.Empty;
            infoText += $"Guidance Type: {_guidanceType.ToString()}\n";

            var numObj = _missiles.Count + _targets.Count + _bullets.Count;
            infoText += $"Num Objects: {numObj}\n";

            infoText += $"FPS: {Math.Round(_renderFPS, 0)}\n";

            gfx.DrawText(infoText, D2DColor.GreenYellow, "Consolas", 12f, pos.X, pos.Y);
        }

        private void DrawGrid(D2DGraphics gfx)
        {
            int rects = 0;
            for (int x = 0; x < this.Width / _colGridSideLen; x++)
            {
                for (int y = 0; y < this.Height / _colGridSideLen; y++)
                {
                    gfx.DrawRectangle(new D2DRect(new D2DPoint(x * _colGridSideLen, y * _colGridSideLen), new D2DSize(_colGridSideLen, _colGridSideLen)), D2DColor.LightGray);
                    rects++;
                }
            }
        }

        private float _testAngle = 0f;
        private void DrawRadial(D2DGraphics gfx, D2DPoint pos)
        {
            const float radius = 300f;
            const float step = 10f;

            float angle = 0f;

            while (angle < 360f)
            {
                var vec = Helpers.AngleToVectorDegrees(angle);
                vec = pos + (vec * radius);

                gfx.DrawLine(pos, vec, D2DColor.Gray);

                gfx.DrawText(angle.ToString(), D2DColor.White, "Consolas", 12f, vec.X, vec.Y);

                angle += step;
            }

            gfx.DrawEllipse(new D2DEllipse(pos, new D2DSize(radius, radius)), D2DColor.White);


            //float testDiff = 200f;
            //float testFact = 0.6f;
            //float angle1 = _testAngle;
            //float angle2 = _testAngle + testDiff;
            ////float angleDiff = Helpers.AngleDiff(angle1, angle2);
            ////float angleDiff = Helpers.LerpAngle(angle1, angle2, 0.5f);
            //float angleDiff = Helpers.Lerp(angle1, angle2, testFact);
            //float angleDiff2 = Helpers.ClampAngle(Helpers.LerpAngle(angle1, angle2, testFact));

            ////gfx.DrawLine(pos, pos + Helpers.AngleToVector(angle1) * (radius), D2DColor.Red);
            ////gfx.DrawLine(pos, pos + Helpers.AngleToVector(angle2) * (radius), D2DColor.Blue);
            ////gfx.DrawLine(pos, pos + Helpers.AngleToVector(angle2 + angleDiff) * (radius), D2DColor.Yellow);

            //gfx.DrawLine(pos, pos + Helpers.AngleToVector(angle1) * (radius), D2DColor.Red);
            //gfx.DrawLine(pos, pos + Helpers.AngleToVector(angle2) * (radius), D2DColor.Blue);
            //gfx.DrawLine(pos, pos + Helpers.AngleToVector(angleDiff) * (radius), D2DColor.Yellow);
            //gfx.DrawLine(pos, pos + Helpers.AngleToVector(angleDiff2) * (radius), D2DColor.Green);

            //if (!_isPaused)
            //    _testAngle -= 1f;
        }



        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case 'p':
                    if (!_isPaused)
                        PauseRender();
                    else
                        ResumeRender();

                    break;

                case 'b':
                    _motionBlur = !_motionBlur;
                    break;

                case 't':
                    _trailsOn = !_trailsOn;
                    break;

                case 'n':
                    _isPaused = true;
                    _oneStep = true;
                    break;

                case 'r':
                    //SpawnTargets(1);
                    SpawnTargets(5);

                    break;

                case 'a':
                    _spawnTargetKey = true;
                    break;

                case 'm':
                    _moveShip = true;
                    break;

                case 'c':
                    Clear();
                    break;

                case '=' or '+':
                    ZoomScale += 0.05f;
                    ResizeGfx();
                    break;

                case '-':
                    ZoomScale -= 0.05f;
                    ResizeGfx();
                    break;

                case 'o':
                    //AccTest();
                    //Test();
                    //Helpers.Rnd = new Random(1234);
                    //Benchmark();
                    break;
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            _player.FlameOn = false;
            _fireBurst = false;
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:

                    if (_spawnTargetKey)
                    {
                        _spawnTargetKey = false;
                        SpawnTarget(new D2DPoint(e.X * ViewPortScaleMulti, e.Y * ViewPortScaleMulti));
                    }
                    else if (_moveShip)
                    {
                        _player.Position = new D2DPoint(e.X * ViewPortScaleMulti, e.Y * ViewPortScaleMulti);
                        _moveShip = false;
                    }
                    else
                    {
                        _player.FlameOn = true;
                        _player.RotationSpeed = 0f;
                    }

                    break;

                case MouseButtons.Right:
                    //_player.FireBullet();
                    //TargetAllWithBullet();
                    _fireBurst = true;
                    break;

                case MouseButtons.Middle:
                    TargetAllWithMissile();
                    //TargetAllWithBullet();
                    break;

            }
        }

        private void Form1_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (!_shiftDown)
            {
                if (e.Delta > 0)
                    _player.Rotation += ROTATE_RATE * 1f;
                else
                    _player.Rotation -= ROTATE_RATE * 1f;

                Debug.WriteLine($"PlrRot: {_player.Rotation}");
            }
            else
            {
                var len = Enum.GetNames(typeof(GuidedMissile.GuidanceType)).Length;
                var cur = (int)_guidanceType;
                int next = cur;

                if (e.Delta < 0)
                    next = (next + 1) % len;
                else
                    next = (next - 1) < 0 ? len - 1 : next - 1;

                _guidanceType = (GuidedMissile.GuidanceType)next;
            }

        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            ResizeGfx();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            ResizeGfx();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            ResizeGfx();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            _shiftDown = e.Shift;
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            _shiftDown = e.Shift;
        }
    }
}