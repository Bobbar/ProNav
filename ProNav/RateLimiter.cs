using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProNav
{
    /// <summary>
    /// Provides basic rate limiting for a single value.
    /// </summary>
    public class RateLimiter
    {
        public float Target 
        { 
            get { return _target; }
            set { _target = value; }
        }

        public float Value => _current;

        private float _target = 0f;
        private float _current = 0f;
        private float _rate = 0f;

        public RateLimiter(float rate)
        {
            _rate = rate;
        }

        public void Update(float dt)
        {
            if (_current == _target) 
                return;

            var diff = _target - _current;
            var sign = Math.Sign(diff);
            var amt = (_rate * sign) * dt;

            if (Math.Abs(amt) > Math.Abs(diff))
                amt = diff;

            _current += amt;
        }
    }
}
