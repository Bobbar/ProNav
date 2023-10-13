using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProNav
{
    public static class Helpers
    {

        public static Random Rnd = new Random();


        public static float Lerp(float value1, float value2, float amount)
        {
            return value1 + (value2 - value1) * amount;
        }

        public static float Factor(float value1, float value2)
        {
            return Math.Clamp(value1 / value2, 0f, 1f);
        }

    }
}
