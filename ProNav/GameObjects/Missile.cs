using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unvell.D2DLib;

namespace ProNav.GameObjects
{
    public abstract class Missile : GameObjectPoly
    {
        public Missile() { }

        public Missile(D2DPoint pos) : base(pos) { }

        public Missile(D2DPoint pos, D2DPoint velo) : base(pos, velo) { }

        public Missile(D2DPoint pos, D2DPoint velo, float rotation) : base(pos, velo, rotation) { }
    }


}
