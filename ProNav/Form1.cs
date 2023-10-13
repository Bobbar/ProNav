using ProNav.GameObjects;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using unvell.D2DLib;

namespace ProNav
{
    public partial class Form1 : Form
    {
        private D2DDevice _device;
        private D2DGraphics _gfx;
        private Task _renderThread;

        private const float DT = 0.08f;//0.1f;
        private const float RENDER_SCALE = -0.3f;//-0.5f;//0.06f;
        private const int PHYSICS_STEPS = 10;
        private const float ROTATE_RATE = 2f;

        private float _zoomScale = 0.3f;//1.2f;//0.3f;
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
        private bool _blurOn = false;
        private bool _oneStep = false;
        private bool _moveShip = false;
        private bool _spawnTargetKey = false;

        //private List<GameObject> _missiles = new List<GameObject>();
        //private List<GameObject> _targets = new List<GameObject>();

        private List<GameObjectPoly> _missiles = new List<GameObjectPoly>();
        private List<GameObjectPoly> _targets = new List<GameObjectPoly>();
        private Ship _player = new Ship();

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

            _renderThread = new Task(RenderLoop, TaskCreationOptions.LongRunning);
            _renderThread.Start();

            _player.Position = new D2DPoint(_viewPortSize.width * 0.5f, _viewPortSize.height * 0.5f);
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

            ////_targets.Add(new RotatingMovingTarget(new D2DPoint(100, 300)));
            //_targets.Add(new ErraticMovingTarget(new D2DPoint(100, 300)));

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
            while (!this.Disposing)
            {
                _stopRenderEvent.Wait();

                if (_blurOn)
                    _gfx.BeginRender();
                else
                    _gfx.BeginRender(D2DColor.Black);

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


                        _missiles.ForEach(o => o.Render(_gfx));
                        _targets.ForEach(o => o.Render(_gfx));

                        DoCollisions();
                    }

                    _oneStep = false;
                }

                _player.Update(DT, _viewPortSize, RENDER_SCALE);
                _player.Render(_gfx);


                _gfx.PopTransform();
                _gfx.EndRender();


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
                        targ.IsDestroyed = true;
                        _targets.Remove(targ);
                        //_missiles.Remove(missile);
                        missile.IsExpired = true;
                    }
                }
            }

            for (int r = 0; r < _missiles.Count; r++)
            {
                var missile = _missiles[r] as Missile;

                if (missile.IsExpired)
                    _missiles.Remove(missile);
            }


        }

        private void SpawnTargets(int num)
        {
            StopRender();

            var vpSzHalf = new D2DSize(_viewPortSize.width * 0.5f, _viewPortSize.height * 0.5f);


            for (int i = 0; i < num; i++)
            {
                var pos = new D2DPoint(_rnd.NextFloat((vpSzHalf.width - (vpSzHalf.width * 0.5f)), vpSzHalf.width + (vpSzHalf.width * 0.5f)), _rnd.NextFloat((vpSzHalf.height - (vpSzHalf.height * 0.5f)), vpSzHalf.height + (vpSzHalf.height * 0.5f)));
                SpawnTarget(pos);
                //var targ = new ErraticMovingTarget(pos);
                //_targets.Add(targ);
            }

            ResumeRender();
        }

        private void SpawnTarget(D2DPoint pos)
        {
            var targ = new ErraticMovingTarget(pos);
            //var targ = new RotatingMovingTarget(pos);
            //var targ = new LinearMovingTarget(pos);

            _targets.Add(targ);
        }

        private void TargetAll()
        {
            StopRender();

            lock (_missiles)
            {
                foreach (var targ in _targets)
                {
                    var missile = new GuidedMissile(_player, targ as Target);
                    _missiles.Add(missile);
                }
            }

            //Debug.WriteLine("----");
            ResumeRender();
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case 'p':
                    //_isPaused = !_isPaused;

                    if (!_isPaused)
                        PauseRender();
                    else
                        ResumeRender();

                    break;

                case 'b':
                    _blurOn = !_blurOn;
                    break;

                case 'n':
                    _isPaused = true;
                    _oneStep = true;
                    break;

                case 'r':
                    SpawnTargets(1);
                    //SpawnRoids(1);
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

                case 'q':

                    //SetPlayer();
                    _isPaused = true;
                    break;

                case 'o':
                    //MissileTest();
                    Refresh();
                    break;


            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            _player.FlameOn = false;
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
                    //FireBullet();
                    //FireMissile();
                    //FireMissile(new D2DPoint(e.X, e.Y));
                    //FireMissile(new D2DPoint(e.X * ViewPortScaleMulti, e.Y * ViewPortScaleMulti));

                    //_player.Velocity = D2DPoint.Zero;

                    break;

                case MouseButtons.Middle:
                    //SelectRoid(new D2DPoint(e.X, e.Y));
                    //_player.Velocity = D2DPoint.Zero;

                    TargetAll();

                    break;

            }
        }

        private void Form1_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                //_player.RotationSpeed += ROTATE_RATE;
                _player.Rotation += ROTATE_RATE * 1f;
                //_player.TailTrim += ROTATE_RATE * 0.25f;

            }
            else
            {
                _player.Rotation -= ROTATE_RATE * 1f;
                //_player.TailTrim -= ROTATE_RATE * 0.25f;

                //_player.RotationSpeed -= ROTATE_RATE;
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
    }
}