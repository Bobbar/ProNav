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

        private const float DT = 0.1f;//0.08f;
        private const float RENDER_SCALE = -0.3f;
        private const int PHYSICS_STEPS = 10;
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

        private const int BURST_NUM = 10;
        private const int BURST_FRAMES = 3;
        private int _burstCount = 0;
        private int _burstFrame = 0;

        private LockedList<GameObjectPoly> _missiles = new LockedList<GameObjectPoly>();
        private LockedList<GameObjectPoly> _targets = new LockedList<GameObjectPoly>();
        private LockedList<GameObject> _bullets = new LockedList<GameObject>();

        private Ship _player = new Ship();

        private D2DColor _blurColor = new D2DColor(0.1f, D2DColor.Black);

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
            _player.FireBulletCallback = b => { _bullets.Add(b); };
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
        }

        private void ResizeGfx()
        {
            StopRender();

            _device?.Resize();

            _viewPortSize = new D2DSize(this.Width * ViewPortScaleMulti, this.Height * ViewPortScaleMulti);
            _viewPortRect = new D2DRect(0, 0, this.Width * ViewPortScaleMulti, this.Height * ViewPortScaleMulti);

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

                        _missiles.ForEach(o => o.Render(_gfx));
                        _targets.ForEach(o => o.Render(_gfx));
                        _bullets.ForEach(o => o.Render(_gfx));

                        DoCollisions();
                    }

                    _oneStep = false;
                }

                //_missiles.ForEach(o => o.Render(_gfx));
                //_targets.ForEach(o => o.Render(_gfx));
                //_bullets.ForEach(o => o.Render(_gfx));

                _player.Update(DT, _viewPortSize, RENDER_SCALE);
                _player.Render(_gfx);

                _gfx.PopTransform();
                _gfx.EndRender();


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
                    _missiles.Remove(missile);
            }

            for (int o = 0; o < _targets.Count; o++)
            {
                var targ = _targets[o];

                if (targ.IsExpired)
                    _targets.Remove(targ);
            }

            for (int o = 0; o < _bullets.Count; o++)
            {
                var bullet = _bullets[o];

                if (bullet.IsExpired)
                    _bullets.Remove(bullet);
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
                var missile = new GuidedMissile(_player, targ as Target);
                _missiles.Add(missile);
            }
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
            float radStep = 1.0f;
            float angle = 0f;

            for (int i = 0; i < numParticles; i++)
            {
                var vec = new D2DPoint((float)Math.Cos(angle * radStep) * velo, (float)Math.Sin(angle * radStep) * velo);
                _bullets.Add(new Bullet(pos, vec, lifetime));
                angle += (float)(2f * Math.PI / numParticles);
            }
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
                    SpawnTargets(1);
                    break;

                case 'a':
                    _spawnTargetKey = true;
                    break;

                case 'm':
                    _moveShip = true;
                    break;

                case 'c':
                    PauseRender();
                    _missiles.Clear();
                    _targets.Clear();
                    _bullets.Clear();
                    ResumeRender();
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
            if (e.Delta > 0)
                _player.Rotation += ROTATE_RATE * 1f;
            else
                _player.Rotation -= ROTATE_RATE * 1f;
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
    }
}