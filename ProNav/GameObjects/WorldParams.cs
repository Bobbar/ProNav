using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unvell.D2DLib;

namespace ProNav.GameObjects
{
    public static class World
    {
        public static float DT
        {
            get { return _dt; }
            set
            {
                _dt = Math.Clamp(value, 0.0004f, 1f);
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

        private static float _zoomScale = 0.4f;
        private static float _dt = 0.06f;

        public static void UpdateViewport(Size viewPortSize)
        {
            ViewPortSize = new D2DSize(viewPortSize.Width * ViewPortScaleMulti, viewPortSize.Height * ViewPortScaleMulti);
            ViewPortRect = new D2DRect(0, 0, viewPortSize.Width * ViewPortScaleMulti, viewPortSize.Height * ViewPortScaleMulti);
        }
    }
}
