using System.Diagnostics;
using unvell.D2DLib;

namespace ProNav
{
    public static class World
    {
        public const int PHYSICS_STEPS = 8;

        public static float DT
        {
            get { return _dt; }
            set
            {
                _dt = Math.Clamp(value, 0.0004f, 1f);
            }
        }

        public static float SUB_DT
        {
            get
            {
                return DT / PHYSICS_STEPS;
            }
        }

        public static float RenderScale { get; set; } = 0.1f;

        public static float ZoomScale
        {
            get => _zoomScale;

            set
            {
                if (value >= 0.1f && value <= 3f)
                    _zoomScale = value;

                Debug.WriteLine($"Zoom: {_zoomScale}");
            }
        }


        public static D2DSize ViewPortSize { get; set; }
        public static D2DSize ViewPortBaseSize { get; set; }
        public static D2DRect ViewPortRect { get; set; }

        public static float ViewPortScaleMulti
        {
            get
            {
                var multi = 1f / _zoomScale;
                return multi;
            }
        }


        public static bool ShowAero = false;
        public static bool ShowMissileCloseup = false;
        public static bool ShowTracking = false;
        public static bool EnableWind = false;
        public static bool EnableTurbulence = false;
        public static bool ExpireMissilesOnMiss = false;

        private static float _zoomScale = 0.35f;
        private static float _dt = 0.06f;


        private const float MIN_TURB_DENS = 0.6f;
        private const float MAX_TURB_DENS = 1.225f;
        private const float MAX_WIND_MAG = 100f;

        public static float AirDensity = 1.225f;
        public static D2DPoint Wind = D2DPoint.Zero;

        private static RandomVariationFloat _airDensVariation = new RandomVariationFloat(MIN_TURB_DENS, MAX_TURB_DENS, 0.2f, 5f);
        private static RandomVariationVector _windVariation = new RandomVariationVector(MAX_WIND_MAG, 10f, 50f);

        public static D2DPoint Gravity = new D2DPoint(9.8f, 0);

        public static void UpdateViewport(Size viewPortSize)
        {
            ViewPortBaseSize = new D2DSize(viewPortSize.Width, viewPortSize.Height);
            ViewPortSize = new D2DSize(viewPortSize.Width * ViewPortScaleMulti, viewPortSize.Height * ViewPortScaleMulti);
            ViewPortRect = new D2DRect(0, 0, viewPortSize.Width * ViewPortScaleMulti, viewPortSize.Height * ViewPortScaleMulti);
        }

        public static void UpdateAirDensityAndWind(float dt)
        {
            if (EnableTurbulence)
            {
                _airDensVariation.Update(dt);
                AirDensity = _airDensVariation.Value;
            }
            else
            {
                AirDensity = MAX_TURB_DENS;
            }

            if (EnableWind)
            {
                _windVariation.Update(dt);
                Wind = _windVariation.Value;
            }
            else
            {
                Wind = D2DPoint.Zero;
            }
        }

    }
}
